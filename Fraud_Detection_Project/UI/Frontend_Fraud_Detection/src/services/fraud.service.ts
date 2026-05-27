import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map, catchError } from 'rxjs/operators';
import { AlertDTO, DashboardStatsDTO } from '../models/alert.dto';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FraudService {
  constructor(private http: HttpClient) {}

  getDashboardStats(): Observable<DashboardStatsDTO> {
    return this.http.get<DashboardStatsDTO>(`${environment.apiUrl}/fraud/stats`).pipe(
      catchError(err => {
        console.error('Failed to load dashboard stats:', err.message || err);
        return of({
          totalDonations: '₹0',
          transactionsToday: 0,
          flaggedDonations: 0,
          confirmedFraud: 0
        });
      })
    );
  }

  getRecentAlerts(): Observable<AlertDTO[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/fraud`).pipe(
      map(flags => {
        // Map backend FraudFlag objects to Frontend AlertDTO
        return flags.map((f): AlertDTO => ({
          id: f.id.toString(),
          donationId: f.donationId?.toString() || f.id.toString(),
          donorName: f.donorUsername || 'Donor ' + f.donationId,
          amount: f.donationAmount || 0,
          riskScore: f.riskScore,
          reason: f.reason,
          status: f.isApproved != null ? 'Resolved' : 'Pending',
          date: f.donationTimestamp ? new Date(f.donationTimestamp).toLocaleString() : new Date(f.createdAt).toLocaleString(),
          paymentMethod: f.paymentMethod || 'Unknown',
          ipAddress: f.ipAddress || 'Unknown',
          country: f.country || 'Unknown'
        }));
      }),
      catchError(err => {
        console.error('Failed to load recent alerts from server:', err.message || err);
        return of([]);
      })
    );
  }

  reviewAlert(flagId: string, decision: string, notes: string): Observable<{success: boolean}> {
    const isApproved = decision === 'Approve';
    return this.http.put<any>(`${environment.apiUrl}/fraud/${flagId}/review`, {
      isApproved,
      notes
    }).pipe(
      map(() => ({ success: true }))
    );
  }
}
