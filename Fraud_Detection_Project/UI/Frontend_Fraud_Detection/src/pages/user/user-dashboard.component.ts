import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DonationService } from '../../services/donation.service';
import { AuthService } from '../../services/auth.service';
import { CampaignDTO, DonationDTO } from '../../models/donation.dto';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './user-dashboard.component.html',
})
export class UserDashboardComponent implements OnInit {
  campaigns: CampaignDTO[] = [];
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
    private authService: AuthService
  ) {}

  ngOnInit() {
    const user = this.authService.getCurrentUserValue();
    if (user) {
      this.donation.donorName = `${user.firstName} ${user.lastName}`;
      this.donation.donorEmail = user.email;
    }

    this.donationService.getCampaigns().subscribe(c => {
      this.campaigns = c;
    });
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
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.errorMessage = 'Donation failed. Please try again.';
      }
    });
  }
}
