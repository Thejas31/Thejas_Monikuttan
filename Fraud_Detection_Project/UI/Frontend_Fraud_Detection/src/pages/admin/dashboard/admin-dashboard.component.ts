import { Component, OnInit } from '@angular/core';
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
export class AdminDashboardComponent implements OnInit {
  stats: DashboardStatsDTO | null = null;
  alerts: AlertDTO[] = [];
  selectedAlert: AlertDTO | null = null;
  isLoading = true;

  campaigns: CampaignDTO[] = [];
  isCampaignsDummy = false;
  showCreateCampaignModal = false;

  constructor(
    private fraudService: FraudService,
    private authService: AuthService,
    private donationService: DonationService
  ) {}

  ngOnInit() {
    this.fraudService.getDashboardStats().subscribe(s => {
      this.stats = s;
    });

    this.fraudService.getRecentAlerts().subscribe(alerts => {
      this.alerts = alerts;
      setTimeout(() => {
        this.isLoading = false;
      });
    });

    this.loadCampaigns();
  }

  activeCampaigns: CampaignDTO[] = [];
  endedCampaigns: CampaignDTO[] = [];
  selectedEndedCampaign: CampaignDTO | null = null;

  loadCampaigns() {
    this.donationService.getCampaigns().subscribe(c => {
      if (!c || c.length === 0) {
        this.isCampaignsDummy = true;
        const dummyCampaigns: CampaignDTO[] = [
          { 
            id: 'dummy-1', 
            title: 'Save the Amazon Rainforest', 
            description: 'Help us plant trees and protect wildlife.', 
            targetAmount: 50000, 
            isActive: true, 
            totalAmountRaised: 12000 
          },
          { 
            id: 'dummy-2', 
            title: 'Clean Water Initiative', 
            description: 'Providing clean water to remote villages.', 
            targetAmount: 25000, 
            isActive: true, 
            totalAmountRaised: 5000 
          },
          { 
            id: 'dummy-3', 
            title: 'Ended Scholarship Fund', 
            description: 'Providing tuition fees for underprivileged students.', 
            targetAmount: 10000, 
            isActive: false, 
            totalAmountRaised: 10000,
            donations: [
              { id: 101, amount: 5000, timestamp: '2026-05-10T12:00:00Z', donorName: 'John Doe', donorEmail: 'john@example.com', isApproved: true },
              { id: 102, amount: 5000, timestamp: '2026-05-11T14:30:00Z', donorName: 'Jane Smith', donorEmail: 'jane@example.com', isApproved: true }
            ]
          }
        ];
        this.activeCampaigns = dummyCampaigns.filter(camp => camp.isActive !== false);
        this.endedCampaigns = dummyCampaigns.filter(camp => camp.isActive === false);
      } else {
        this.isCampaignsDummy = false;
        this.campaigns = c;
        this.activeCampaigns = c.filter(camp => camp.isActive !== false);
        this.endedCampaigns = c.filter(camp => camp.isActive === false);
      }
    });
  }

  endCampaign(id: string) {
    if (this.isCampaignsDummy) {
      // Handle dummy ending in frontend-only dev mode
      const allDummies = [
        { id: 'dummy-1', title: 'Save the Amazon Rainforest', description: 'Help us plant trees and protect wildlife.', targetAmount: 50000, isActive: true, totalAmountRaised: 12000 },
        { id: 'dummy-2', title: 'Clean Water Initiative', description: 'Providing clean water to remote villages.', targetAmount: 25000, isActive: true, totalAmountRaised: 5000 },
        { 
          id: 'dummy-3', 
          title: 'Ended Scholarship Fund', 
          description: 'Providing tuition fees for underprivileged students.', 
          targetAmount: 10000, 
          isActive: false, 
          totalAmountRaised: 10000,
          donations: [
            { id: 101, amount: 5000, timestamp: '2026-05-10T12:00:00Z', donorName: 'John Doe', donorEmail: 'john@example.com', isApproved: true },
            { id: 102, amount: 5000, timestamp: '2026-05-11T14:30:00Z', donorName: 'Jane Smith', donorEmail: 'jane@example.com', isApproved: true }
          ]
        }
      ];
      const found = allDummies.find(x => x.id === id);
      if (found) {
        found.isActive = false;
        found.donations = [
          { id: 201, amount: 8000, timestamp: '2026-05-22T09:15:00Z', donorName: 'Alice Johnson', donorEmail: 'alice@example.com', isApproved: true },
          { id: 202, amount: 4000, timestamp: '2026-05-22T10:45:00Z', donorName: 'Bob Smith', donorEmail: 'bob@example.com', isApproved: true }
        ];
      }
      this.activeCampaigns = allDummies.filter(camp => camp.id !== id && camp.isActive !== false);
      this.endedCampaigns = [
        ...allDummies.filter(camp => camp.isActive === false),
        ...(found ? [found] : [])
      ];
      return;
    }

    if (confirm('Are you sure you want to end this campaign?')) {
      this.donationService.endCampaign(id).subscribe({
        next: () => {
          this.loadCampaigns();
          this.selectedEndedCampaign = null;
        },
        error: (err) => {
          console.error('Failed to end campaign:', err);
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
    this.loadCampaigns();
  }

  openReview(alert: AlertDTO) {
    this.selectedAlert = alert;
  }

  onModalClose() {
    this.selectedAlert = null;
  }

  onReviewed() {
    this.selectedAlert = null;
    // Refresh alerts
    this.fraudService.getRecentAlerts().subscribe(a => this.alerts = a);
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
