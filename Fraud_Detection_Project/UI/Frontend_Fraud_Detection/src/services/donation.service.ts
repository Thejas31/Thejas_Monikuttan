import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { DonationDTO, CampaignDTO, MyDonationDTO } from '../models/donation.dto';
import { environment } from '../environments/environment';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class DonationService {
  constructor(private http: HttpClient) {}

  getCampaigns(): Observable<CampaignDTO[]> {
    return this.http.get<CampaignDTO[]>(`${environment.apiUrl}/campaigns`);
  }

  getUserDonations(userId: string): Observable<MyDonationDTO[]> {
    return this.http.get<MyDonationDTO[]>(`${environment.apiUrl}/donations/user/${userId}`);
  }

  createCampaign(campaign: { title: string, description: string, targetAmount: number }): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/campaigns`, campaign);
  }

  submitDonation(donation: DonationDTO): Observable<{success: boolean, message: string}> {
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
