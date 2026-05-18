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
      this.isLoading = false;
    });

    this.loadCampaigns();
  }

  loadCampaigns() {
    this.donationService.getCampaigns().subscribe(c => {
      if (!c || c.length === 0) {
        this.isCampaignsDummy = true;
        this.campaigns = [
          { id: 'dummy-1', title: 'Save the Amazon Rainforest', description: 'Help us plant trees and protect wildlife.', targetAmount: 50000 },
          { id: 'dummy-2', title: 'Clean Water Initiative', description: 'Providing clean water to remote villages.', targetAmount: 25000 }
        ];
      } else {
        this.isCampaignsDummy = false;
        this.campaigns = c;
      }
    });
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
