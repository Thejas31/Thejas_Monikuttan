import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { AlertDTO, DashboardStatsDTO } from '../models/alert.dto';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FraudService {
  constructor(private http: HttpClient) {}

  getDashboardStats(): Observable<DashboardStatsDTO> {
    return of({
      totalDonations: '₹12.8L',
      transactionsToday: 486,
      flaggedDonations: 23,
      confirmedFraud: 7
    });
  }

  getRecentAlerts(): Observable<AlertDTO[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/fraud`).pipe(
      map(flags => {
        // Map backend FraudFlag objects to Frontend AlertDTO
        return flags.map(f => ({
          donationId: f.donationId?.toString() || f.id.toString(),
          donorName: 'Donor ' + f.donationId, // Can be improved if backend returns donor name
          amount: 0, // Fill if available from backend
          riskScore: f.riskScore,
          reason: f.reason,
          status: f.status,
          date: new Date(f.flaggedDate).toLocaleString(),
          paymentMethod: 'Unknown',
          ipAddress: 'Unknown',
          country: 'Unknown'
        }));
      })
    );
  }

  reviewAlert(donationId: string, decision: string, notes: string): Observable<{success: boolean}> {
    const isApproved = decision === 'Approve';
    return this.http.put<any>(`${environment.apiUrl}/fraud/${donationId}/review`, {
      isApproved,
      notes
    }).pipe(
      map(() => ({ success: true }))
    );
  }
}
