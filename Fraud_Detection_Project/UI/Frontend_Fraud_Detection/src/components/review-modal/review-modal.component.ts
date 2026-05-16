import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AlertDTO } from '../../models/alert.dto';
import { FraudService } from '../../services/fraud.service';

@Component({
  selector: 'app-review-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './review-modal.component.html',
})
export class ReviewModalComponent {
  @Input() alert!: AlertDTO | null;
  @Output() close = new EventEmitter<void>();
  @Output() reviewed = new EventEmitter<void>();

  decision = '';
  notes = '';
  isSaving = false;

  constructor(private fraudService: FraudService) {}

  onClose() {
    this.close.emit();
  }

  onSave() {
    if (!this.alert || !this.decision) return;

    this.isSaving = true;
    this.fraudService.reviewAlert(this.alert.donationId, this.decision, this.notes).subscribe({
      next: () => {
        this.isSaving = false;
        this.reviewed.emit();
      },
      error: () => {
        this.isSaving = false;
        // Handle error
      }
    });
  }
}
