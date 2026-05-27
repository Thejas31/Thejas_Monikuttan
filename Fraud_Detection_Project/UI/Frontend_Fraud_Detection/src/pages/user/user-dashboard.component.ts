import { Component, OnInit, OnDestroy } from '@angular/core';
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
export class UserDashboardComponent implements OnInit, OnDestroy {
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
  private pollInterval: any;

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
      this.loadAllData();
    }

    // Live background polling every 5 seconds to keep dashboard and campaigns updated in real-time
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
      if (completed === 2 && callback) {
        callback();
      }
    };

    if (this.userId) {
      this.donationService.getUserDonations(this.userId).subscribe({
        next: donations => {
          this.myDonations = donations || [];
          this.calculateStats();
          checkComplete();
        },
        error: () => {
          this.myDonations = [];
          this.calculateStats();
          checkComplete();
        }
      });
    } else {
      checkComplete();
    }

    this.donationService.getCampaigns().subscribe({
      next: c => {
        this.campaigns = c ? c.filter(camp => camp.isActive !== false) : [];
        if (this.selectedCampaign) {
          const updated = this.campaigns.find(camp => camp.id === this.selectedCampaign?.id);
          if (updated) {
            this.selectedCampaign = updated;
          }
        }
        checkComplete();
      },
      error: () => checkComplete()
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
    const validDonations = this.myDonations.filter(d => d.isApproved !== false);
    this.totalDonated = validDonations.reduce((sum, d) => sum + d.amount, 0);
    const uniqueCampaigns = new Set(validDonations.map(d => d.campaignId));
    this.totalCampaignsSupported = uniqueCampaigns.size;
  }

  setActiveTab(tab: 'dashboard' | 'make-donations' | 'my-donations') {
    this.activeTab = tab;
    this.selectedCampaign = null;
    this.showConfirmModal = false;
    if (tab === 'dashboard' || tab === 'my-donations') {
      this.loadAllDataSilently();
    }
  }

  selectCampaign(campaign: CampaignDTO) {
    this.selectedCampaign = campaign;
    this.donation.campaignId = campaign.id;
    const remaining = (campaign.targetAmount ?? 0) - (campaign.totalAmountRaised ?? 0);
    this.donation.amount = Math.min(1000, remaining);
  }

  onAmountChange() {
    if (!this.selectedCampaign) return;
    const remaining = (this.selectedCampaign.targetAmount ?? 0) - (this.selectedCampaign.totalAmountRaised ?? 0);
    if (this.donation.amount > remaining) {
      this.donation.amount = remaining;
    }
    if (this.donation.amount < 1) {
      this.donation.amount = 1;
    }
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  onSubmit() {
    if (!this.donation.campaignId || !this.donation.amount || !this.donation.donorName) {
      this.showToast('Please fill in all required fields.', 'error');
      return;
    }
    if (this.donation.amount <= 0) {
      this.showToast('Please enter a valid donation amount.', 'error');
      return;
    }
    const remaining = (this.selectedCampaign?.targetAmount ?? 0) - (this.selectedCampaign?.totalAmountRaised ?? 0);
    if (this.donation.amount > remaining) {
      this.showToast(`Maximum allowed donation for this campaign is ₹${remaining.toLocaleString()}.`, 'error');
      return;
    }
    this.showConfirmModal = true;
  }

  confirmDonation() {
    this.isSubmitting = true;

    this.donationService.submitDonation(this.donation).subscribe({
      next: (result) => {
        this.isSubmitting = false;
        this.showConfirmModal = false;
        if (result.success) {
          const donatedAmount = this.donation.amount;
          const campaignId = parseInt(this.donation.campaignId, 10);

          this.totalDonated += donatedAmount;
          const existingCampaign = this.myDonations.find(d => d.campaignId === campaignId);
          if (!existingCampaign) {
            this.totalCampaignsSupported++;
          }

          this.donation.campaignId = '';
          this.donation.amount = 0;
          this.donation.message = '';
          this.selectedCampaign = null;

          this.showToast('Donation made successfully!', 'success');

          // Instantly reload all data to update campaign progress and history
          this.loadAllDataSilently();
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.showConfirmModal = false;
        this.showToast('Donation failed. Please try again.', 'error');
      }
    });
  }

  cancelDonation() {
    this.showConfirmModal = false;
    this.showToast('Donation Withheld!', 'warning');
  }

  showToast(message: string, type: 'success' | 'warning' | 'error') {
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }
    this.toastMessage = message;
    this.toastType = type;
    this.toastTimer = setTimeout(() => {
      this.toastMessage = null;
    }, 3500);
  }

  dismissToast() {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastMessage = null;
  }
}
