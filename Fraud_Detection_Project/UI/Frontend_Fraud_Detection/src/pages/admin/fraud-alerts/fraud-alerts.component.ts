import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FraudService } from '../../../services/fraud.service';
import { AlertDTO } from '../../../models/alert.dto';
import { SidebarComponent } from '../../../components/sidebar/sidebar.component';
import { ReviewModalComponent } from '../../../components/review-modal/review-modal.component';

@Component({
  selector: 'app-fraud-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent, ReviewModalComponent],
  templateUrl: './fraud-alerts.component.html',
})
export class FraudAlertsComponent implements OnInit {
  allAlerts: AlertDTO[] = [];
  filteredAlerts: AlertDTO[] = [];
  selectedAlert: AlertDTO | null = null;
  isLoading = true;

  statusFilter: 'All' | 'Pending' | 'Resolved' = 'All';
  riskFilter: 'All' | 'High' | 'Medium' | 'Low' = 'All';
  searchTerm = '';

  statusFilters = ['All', 'Pending', 'Resolved'];
  riskFilters = ['All', 'High', 'Medium', 'Low'];

  constructor(private fraudService: FraudService) {}

  ngOnInit() {
    this.fraudService.getRecentAlerts().subscribe(alerts => {
      this.allAlerts = alerts;
      this.applyFilters();
      this.isLoading = false;
    });
  }

  applyFilters() {
    this.filteredAlerts = this.allAlerts.filter(a => {
      const matchStatus = this.statusFilter === 'All' || a.status === this.statusFilter;
      const matchRisk =
        this.riskFilter === 'All' ||
        (this.riskFilter === 'High' && a.riskScore >= 80) ||
        (this.riskFilter === 'Medium' && a.riskScore >= 60 && a.riskScore < 80) ||
        (this.riskFilter === 'Low' && a.riskScore < 60);
      const matchSearch =
        !this.searchTerm ||
        a.donorName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        a.donationId.toLowerCase().includes(this.searchTerm.toLowerCase());
      return matchStatus && matchRisk && matchSearch;
    });
  }

  setStatusFilter(filter: string) {
    this.statusFilter = filter as 'All' | 'Pending' | 'Resolved';
    this.applyFilters();
  }

  setRiskFilter(filter: string) {
    this.riskFilter = filter as 'All' | 'High' | 'Medium' | 'Low';
    this.applyFilters();
  }

  openReview(alert: AlertDTO) {
    this.selectedAlert = alert;
  }

  onModalClose() {
    this.selectedAlert = null;
  }

  onReviewed() {
    this.selectedAlert = null;
    this.fraudService.getRecentAlerts().subscribe(a => {
      this.allAlerts = a;
      this.applyFilters();
    });
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
