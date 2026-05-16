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
  name: string;
}
