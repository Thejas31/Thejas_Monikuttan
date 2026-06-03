using System;

namespace DonationFraud.API.Entities
{
    public class DeviceFingerprint
    {
        public int Id { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public string ScreenResolution { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string CanvasHash { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class IpIntelligence
    {
        public int Id { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Isp { get; set; } = string.Empty;
        public bool IsVpnOrProxy { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentMethod
    {
        public int Id { get; set; }
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string CardBrand { get; set; } = string.Empty;
        public string BankCountryCode { get; set; } = string.Empty;
        public bool ThreeDSecureSuccess { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
    }

    public class MlPrediction
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public Donation Donation { get; set; } = null!;
        public string ModelVersion { get; set; } = string.Empty;
        public double PredictionProbability { get; set; }
        public string TopFeaturesImpact { get; set; } = string.Empty; // JSON dictionary of features and weights
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MlModelMetadata
    {
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public double F1Score { get; set; }
        public double RocAuc { get; set; }
        public bool IsActive { get; set; }
        public DateTime DeployedAt { get; set; } = DateTime.UtcNow;
    }
}
