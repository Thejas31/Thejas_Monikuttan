// src/models/alert.dto.ts
export interface AlertDTO {
  donationId: string;
  donorName: string;
  amount: number;
  riskScore: number;
  reason: string;
  status: 'Pending' | 'Resolved';
  date: string;
  paymentMethod: string;
  ipAddress: string;
  country: string;
  notes?: string;
}

export interface DashboardStatsDTO {
  totalDonations: string;
  transactionsToday: number;
  flaggedDonations: number;
  confirmedFraud: number;
}
