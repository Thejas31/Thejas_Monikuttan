// src/models/donation.dto.ts
export interface DonationDTO {
  id?: string;
  campaignId: string;
  campaignName?: string;
  amount: number;
  donorName: string;
  donorEmail?: string;
  paymentMethod: string;
  anonymous?: boolean;
  status?: string;
  date?: string;
  message?: string;
}

export interface CampaignDTO {
  id: string;
  title: string;
  description?: string;
  targetAmount?: number;
  totalAmountRaised?: number;
  isActive?: boolean;
  totalDonations?: number;
  donations?: CampaignDonationDTO[];
}

export interface CampaignDonationDTO {
  id: number;
  amount: number;
  timestamp: string;
  donorName: string;
  donorEmail: string;
  isApproved?: boolean | null;
}

export interface MyDonationDTO {
  id: number;
  amount: number;
  timestamp: string;
  campaignId: number;
  campaignTitle: string;
  isFlagged: boolean;
  fraudReason?: string;
  riskLevel?: string;
  isApproved?: boolean | null;
  adminNotes?: string;
}
