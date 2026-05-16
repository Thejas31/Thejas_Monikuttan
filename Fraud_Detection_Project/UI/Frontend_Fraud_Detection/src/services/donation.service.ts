import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { DonationDTO, CampaignDTO } from '../models/donation.dto';
import { environment } from '../environments/environment';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class DonationService {
  constructor(private http: HttpClient) {}

  getCampaigns(): Observable<CampaignDTO[]> {
    // Assuming backend has a campaigns endpoint. Adjust if different.
    return this.http.get<CampaignDTO[]>(`${environment.apiUrl}/campaigns`);
  }

  submitDonation(donation: DonationDTO): Observable<{success: boolean, message: string}> {
    // Map Frontend DTO to Backend CreateDonationDto
    const backendDto = {
      campaignId: parseInt(donation.campaignId, 10),
      amount: donation.amount
    };

    return this.http.post<any>(`${environment.apiUrl}/donations`, backendDto).pipe(
      map(response => ({
        success: true,
        message: response.message || 'Donation processed securely'
      }))
    );
  }
}
