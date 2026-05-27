import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FraudService } from '../../../services/fraud.service';
import { AuthService } from '../../../services/auth.service';
import { AlertDTO, DashboardStatsDTO } from '../../../models/alert.dto';
import { SidebarComponent } from '../../../components/sidebar/sidebar.component';
import { ReviewModalComponent } from '../../../components/review-modal/review-modal.component';
import { CreateCampaignModalComponent } from '../../../components/create-campaign-modal/create-campaign-modal.component';
import { DonationService } from '../../../services/donation.service';
import { CampaignDTO } from '../../../models/donation.dto';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, ReviewModalComponent, CreateCampaignModalComponent],
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  stats: DashboardStatsDTO | null = null;
  alerts: AlertDTO[] = [];
  selectedAlert: AlertDTO | null = null;
  isLoading = true;

  campaigns: CampaignDTO[] = [];
  isCampaignsDummy = false;
  showCreateCampaignModal = false;
  private pollInterval: any;

  activeCampaigns: CampaignDTO[] = [];
  endedCampaigns: CampaignDTO[] = [];
  selectedEndedCampaign: CampaignDTO | null = null;

  constructor(
    private fraudService: FraudService,
    private authService: AuthService,
    private donationService: DonationService
  ) {}

  ngOnInit() {
    this.loadAllData();
    // Background polling every 5 seconds to keep the admin dashboard live and responsive
    this.pollInterval = setInterval(() => {
      this.loadAllDataSilently();
    }, 5000);
  }

  ngOnDestroy() {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
    }
  }

  loadAllData() {
    this.isLoading = true;
    this.loadAllDataSilently(() => {
      this.isLoading = false;
    });
  }

  loadAllDataSilently(callback?: () => void) {
    let completed = 0;
    const checkComplete = () => {
      completed++;
      if (completed === 3 && callback) {
        callback();
      }
    };

    this.fraudService.getDashboardStats().subscribe({
      next: s => {
        this.stats = s;
        checkComplete();
      },
      error: () => checkComplete()
    });

    this.fraudService.getRecentAlerts().subscribe({
      next: alerts => {
        this.alerts = alerts;
        checkComplete();
      },
      error: () => checkComplete()
    });

    this.donationService.getCampaigns().subscribe({
      next: c => {
        this.campaigns = c || [];
        this.activeCampaigns = this.campaigns.filter(camp => camp.isActive !== false);
        this.endedCampaigns = this.campaigns.filter(camp => camp.isActive === false);
        if (this.selectedEndedCampaign) {
          const updated = this.campaigns.find(camp => camp.id === this.selectedEndedCampaign?.id);
          if (updated) {
            this.selectedEndedCampaign = updated;
          }
        }
        checkComplete();
      },
      error: () => checkComplete()
    });
  }

  loadCampaigns() {
    this.donationService.getCampaigns().subscribe(c => {
      this.isCampaignsDummy = false;
      this.campaigns = c || [];
      this.activeCampaigns = this.campaigns.filter(camp => camp.isActive !== false);
      this.endedCampaigns = this.campaigns.filter(camp => camp.isActive === false);
    });
  }

  endCampaign(id: string) {
    if (confirm('Are you sure you want to end this campaign?')) {
      this.donationService.endCampaign(id).subscribe({
        next: () => {
          this.loadAllDataSilently();
          this.selectedEndedCampaign = null;
        },
        error: (err) => {
          console.error('Failed to end campaign:', err);
        }
      });
    }
  }

  reactivateCampaign(campaign: CampaignDTO) {
    if (confirm(`Are you sure you want to reactivate the campaign "${campaign.title}"?`)) {
      this.donationService.reactivateCampaign(campaign.id.toString()).subscribe({
        next: () => {
          this.loadAllDataSilently();
          this.selectedEndedCampaign = null;
        },
        error: (err) => {
          console.error('Failed to reactivate campaign:', err);
          alert(err.error?.message || 'Failed to reactivate campaign.');
        }
      });
    }
  }

  selectEndedCampaign(campaign: CampaignDTO) {
    this.selectedEndedCampaign = campaign;
  }

  openCreateCampaign() {
    this.showCreateCampaignModal = true;
  }

  closeCreateCampaign() {
    this.showCreateCampaignModal = false;
  }

  onCampaignCreated() {
    this.showCreateCampaignModal = false;
    this.loadAllDataSilently();
  }

  openReview(alert: AlertDTO) {
    this.selectedAlert = alert;
  }

  onModalClose() {
    this.selectedAlert = null;
  }

  onReviewed() {
    this.selectedAlert = null;
    // Instantly refresh all data to update stats and campaigns raised amount
    this.loadAllDataSilently();
  }

  getRiskClass(score: number): string {
    if (score >= 80) return 'text-status-danger';
    if (score >= 60) return 'text-status-warning';
    return 'text-status-success';
  }

  getRiskBadgeClass(score: number): string {
    if (score >= 80) return 'bg-status-danger/20 text-status-danger border-status-danger/30';
    if (score >= 60) return 'bg-status-warning/20 text-status-warning border-status-warning/30';
    return 'bg-status-success/20 text-status-success border-status-success/30';
  }

  getStatusClass(status: string): string {
    return status === 'Pending'
      ? 'bg-status-warning/20 text-status-warning border-status-warning/30'
      : 'bg-status-success/20 text-status-success border-status-success/30';
  }
}
