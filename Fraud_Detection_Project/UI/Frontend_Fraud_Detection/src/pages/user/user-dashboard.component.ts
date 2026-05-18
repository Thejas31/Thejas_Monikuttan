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
  totalDonated = 0;
  totalCampaignsSupported = 0;
  isLoading = false;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

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
      this.donation.donorName = this.userFullName;
      this.donation.donorEmail = this.userEmail;

      this.fetchMyDonations(user.id);
    }

    this.donationService.getCampaigns().subscribe(c => {
      this.campaigns = c;
    });
  }

  fetchMyDonations(userId: string) {
    this.donationService.getUserDonations(userId).subscribe(donations => {
      this.myDonations = donations || [];
      this.calculateStats();
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
    this.successMessage = '';
    this.errorMessage = '';
  }

  selectCampaign(campaign: CampaignDTO) {
    this.selectedCampaign = campaign;
    this.donation.campaignId = campaign.id;
    this.donation.amount = 0;
  }

  logout() {
    this.authService.logout();
  }

  selectAmount(amount: number) {
    this.donation.amount = amount;
  }

  onSubmit() {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.donation.campaignId || !this.donation.amount || !this.donation.donorName) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    if (this.donation.amount <= 0) {
      this.errorMessage = 'Please enter a valid donation amount.';
      return;
    }

    this.isSubmitting = true;

    this.donationService.submitDonation(this.donation).subscribe({
      next: (result) => {
        this.isSubmitting = false;
        if (result.success) {
          this.successMessage = `✅ ${result.message}! Your donation of ₹${this.donation.amount.toLocaleString()} has been received.`;
          // Reset form
          this.donation.campaignId = '';
          this.donation.amount = 0;
          this.donation.message = '';
          this.selectedCampaign = null;

          // Refresh donations
          const user = this.authService.getCurrentUserValue();
          if (user) {
            this.fetchMyDonations(user.id);
          }
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.errorMessage = 'Donation failed. Please try again.';
      }
    });
  }
}
