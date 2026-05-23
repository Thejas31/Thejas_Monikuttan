import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DonationService } from '../../services/donation.service';
import { AuthService } from '../../services/auth.service';
import { CampaignDTO, DonationDTO, MyDonationDTO } from '../../models/donation.dto';
import { Router } from '@angular/router';

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-dashboard.component.html',
})
export class UserDashboardComponent implements OnInit {
  Math = Math;
  activeTab: 'dashboard' | 'make-donations' | 'my-donations' = 'dashboard';
  myDonations: MyDonationDTO[] = [];
  campaigns: CampaignDTO[] = [];
  selectedCampaign: CampaignDTO | null = null;
  
  userFullName = '';
  userEmail = '';
  userId = '';
  totalDonated = 0;
  totalCampaignsSupported = 0;
  isLoading = false;
  isSubmitting = false;

  // Confirmation modal
  showConfirmModal = false;

  // Toast
  toastMessage: string | null = null;
  toastType: 'success' | 'warning' | 'error' = 'success';
  private toastTimer: any;

  donation: DonationDTO = {
    campaignId: '',
    donorName: '',
    amount: 0,
    paymentMethod: 'Credit Card',
    donorEmail: '',
    anonymous: false,
    message: ''
  };

  paymentMethods: string[] = [
    'Credit Card', 'Debit Card', 'Bank Transfer', 'UPI'
  ];

  presetAmounts = [500, 1000, 2500, 5000, 10000];

  constructor(
    private donationService: DonationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    const user = this.authService.getCurrentUserValue();
    if (user) {
      this.userFullName = `${user.firstName} ${user.lastName}`;
      this.userEmail = user.email;
      this.userId = user.id;
      this.donation.donorName = this.userFullName;
      this.donation.donorEmail = this.userEmail;
      this.fetchMyDonations();
    }

    this.donationService.getCampaigns().subscribe(c => {
      this.campaigns = c;
    });
  }

  fetchMyDonations() {
    if (!this.userId) return;
    this.donationService.getUserDonations(this.userId).subscribe({
      next: donations => {
        this.myDonations = donations || [];
        this.calculateStats();
      },
      error: () => {
        this.myDonations = [];
        this.calculateStats();
      }
    });
  }

  calculateStats() {
    this.totalDonated = this.myDonations.reduce((sum, d) => sum + d.amount, 0);
    const uniqueCampaigns = new Set(this.myDonations.map(d => d.campaignId));
    this.totalCampaignsSupported = uniqueCampaigns.size;
  }

  setActiveTab(tab: 'dashboard' | 'make-donations' | 'my-donations') {
    this.activeTab = tab;
    this.selectedCampaign = null;
    this.showConfirmModal = false;
    // Refresh stats when switching to dashboard
    if (tab === 'dashboard' || tab === 'my-donations') {
      this.fetchMyDonations();
    }
  }

  selectCampaign(campaign: CampaignDTO) {
    this.selectedCampaign = campaign;
    this.donation.campaignId = campaign.id;
    this.donation.amount = 0;
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // Called when user clicks "Complete Donation" — validates and opens confirm modal
  onSubmit() {
    if (!this.donation.campaignId || !this.donation.amount || !this.donation.donorName) {
      this.showToast('Please fill in all required fields.', 'error');
      return;
    }
    if (this.donation.amount <= 0) {
      this.showToast('Please enter a valid donation amount.', 'error');
      return;
    }
    this.showConfirmModal = true;
  }

  // User confirmed — execute the actual donation
  confirmDonation() {
    this.showConfirmModal = false;
    this.isSubmitting = true;

    this.donationService.submitDonation(this.donation).subscribe({
      next: (result) => {
        this.isSubmitting = false;
        if (result.success) {
          const donatedAmount = this.donation.amount;
          const campaignId = parseInt(this.donation.campaignId, 10);

          // Optimistically update stats immediately (before API refresh completes)
          this.totalDonated += donatedAmount;
          const existingCampaign = this.myDonations.find(d => d.campaignId === campaignId);
          if (!existingCampaign) {
            this.totalCampaignsSupported++;
          }

          // Reset form and navigate back
          this.donation.campaignId = '';
          this.donation.amount = 0;
          this.donation.message = '';
          this.selectedCampaign = null;

          // Show success toast
          this.showToast('Donation made successfully!', 'success');

          // Refresh from server to get accurate data
          this.fetchMyDonations();
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.showToast('Donation failed. Please try again.', 'error');
      }
    });
  }

  // User cancelled — close modal and show withheld toast
  cancelDonation() {
    this.showConfirmModal = false;
    this.showToast('Donation Withheld!', 'warning');
  }

  showToast(message: string, type: 'success' | 'warning' | 'error') {
    // Cancel any pending timer
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }
    this.toastMessage = message;
    this.toastType = type;
    // Auto-hide after 3.5 seconds
    this.toastTimer = setTimeout(() => {
      this.toastMessage = null;
    }, 3500);
  }

  dismissToast() {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastMessage = null;
  }
}
