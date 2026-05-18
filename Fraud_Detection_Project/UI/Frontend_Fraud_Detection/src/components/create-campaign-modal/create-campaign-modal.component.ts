import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DonationService } from '../../services/donation.service';

@Component({
  selector: 'app-create-campaign-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-campaign-modal.component.html'
})
export class CreateCampaignModalComponent {
  @Output() close = new EventEmitter<void>();
  @Output() created = new EventEmitter<void>();

  campaign = {
    title: '',
    description: '',
    targetAmount: 0
  };

  isSubmitting = false;
  errorMessage = '';

  constructor(private donationService: DonationService) {}

  onClose() {
    this.close.emit();
  }

  onSubmit() {
    this.errorMessage = '';

    if (!this.campaign.title || !this.campaign.description || !this.campaign.targetAmount) {
      this.errorMessage = 'Please fill all required fields.';
      return;
    }

    if (this.campaign.targetAmount <= 0) {
      this.errorMessage = 'Target amount must be greater than zero.';
      return;
    }

    this.isSubmitting = true;

    this.donationService.createCampaign(this.campaign).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.created.emit();
      },
      error: () => {
        this.isSubmitting = false;
        this.errorMessage = 'Failed to create campaign. Please try again.';
      }
    });
  }
}
