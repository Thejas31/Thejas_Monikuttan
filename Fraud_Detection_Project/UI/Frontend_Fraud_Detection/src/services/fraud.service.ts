import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { AlertDTO, DashboardStatsDTO } from '../models/alert.dto';

@Injectable({
  providedIn: 'root'
})
export class FraudService {

  constructor() {}

  getDashboardStats(): Observable<DashboardStatsDTO> {
    return of({
      totalDonations: '₹12.8L',
      transactionsToday: 486,
      flaggedDonations: 23,
      confirmedFraud: 7
    });
  }

  getRecentAlerts(): Observable<AlertDTO[]> {
    return of([
      {
        donationId: 'DN-10421',
        donorName: 'John Smith',
        amount: 50000,
        riskScore: 92,
        reason: 'Multiple cards used from the same IP address within 15 minutes.',
        status: 'Pending',
        date: '10-May-2026 10:45 AM',
        paymentMethod: 'Credit Card',
        ipAddress: '192.168.10.25',
        country: 'India'
      },
      {
        donationId: 'DN-10418',
        donorName: 'Priya Nair',
        amount: 75000,
        riskScore: 88,
        reason: 'High-value new donor',
        status: 'Resolved',
        date: '10-May-2026 09:30 AM',
        paymentMethod: 'Bank Transfer',
        ipAddress: '117.200.10.5',
        country: 'India'
      },
      {
        donationId: 'DN-10412',
        donorName: 'Anonymous',
        amount: 15000,
        riskScore: 71,
        reason: 'Suspicious IP address',
        status: 'Pending',
        date: '09-May-2026 08:15 PM',
        paymentMethod: 'Debit Card',
        ipAddress: '45.22.100.1',
        country: 'Russia'
      }
    ]);
  }

  reviewAlert(donationId: string, decision: string, notes: string): Observable<{success: boolean}> {
    console.log(`Alert ${donationId} reviewed. Decision: ${decision}, Notes: ${notes}`);
    return of({ success: true });
  }
}
