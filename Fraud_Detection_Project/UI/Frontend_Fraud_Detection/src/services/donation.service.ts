import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { DonationDTO, CampaignDTO } from '../models/donation.dto';

@Injectable({
  providedIn: 'root'
})
export class DonationService {
  constructor() {}

  getCampaigns(): Observable<CampaignDTO[]> {
    return of([
      { id: '1', name: 'Children Education Fund' },
      { id: '2', name: 'Disaster Relief Fund' },
      { id: '3', name: 'Medical Emergency Fund' }
    ]);
  }

  submitDonation(donation: DonationDTO): Observable<{success: boolean, message: string}> {
    // Mock API call
    console.log('Donation submitted:', donation);
    return of({ success: true, message: 'Donation processed securely' });
  }
}
