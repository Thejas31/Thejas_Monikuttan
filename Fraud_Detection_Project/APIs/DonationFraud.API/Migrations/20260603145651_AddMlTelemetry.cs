using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationFraud.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMlTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiRiskScore",
                table: "FraudFlags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RuleRiskScore",
                table: "FraudFlags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeviceFingerprintId",
                table: "Donations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IpIntelligenceId",
                table: "Donations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodId",
                table: "Donations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceFingerprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenResolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CanvasHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Os = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceFingerprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpIntelligences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Isp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVpnOrProxy = table.Column<bool>(type: "bit", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpIntelligences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlModelMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    F1Score = table.Column<double>(type: "float", nullable: false),
                    RocAuc = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlModelMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlPredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredictionProbability = table.Column<double>(type: "float", nullable: false),
                    TopFeaturesImpact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MlPredictions_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaskedCardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardBrand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankCountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThreeDSecureSuccess = table.Column<bool>(type: "bit", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_DeviceFingerprintId",
                table: "Donations",
                column: "DeviceFingerprintId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_IpIntelligenceId",
                table: "Donations",
                column: "IpIntelligenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_PaymentMethodId",
                table: "Donations",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MlPredictions_DonationId",
                table: "MlPredictions",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Fingerprint",
                table: "PaymentMethods",
                column: "Fingerprint");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_DeviceFingerprints_DeviceFingerprintId",
                table: "Donations",
                column: "DeviceFingerprintId",
                principalTable: "DeviceFingerprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_IpIntelligences_IpIntelligenceId",
                table: "Donations",
                column: "IpIntelligenceId",
                principalTable: "IpIntelligences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_PaymentMethods_PaymentMethodId",
                table: "Donations",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_DeviceFingerprints_DeviceFingerprintId",
                table: "Donations");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_IpIntelligences_IpIntelligenceId",
                table: "Donations");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_PaymentMethods_PaymentMethodId",
                table: "Donations");

            migrationBuilder.DropTable(
                name: "DeviceFingerprints");

            migrationBuilder.DropTable(
                name: "IpIntelligences");

            migrationBuilder.DropTable(
                name: "MlModelMetadata");

            migrationBuilder.DropTable(
                name: "MlPredictions");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_Donations_DeviceFingerprintId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_IpIntelligenceId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_PaymentMethodId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "AiRiskScore",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "RuleRiskScore",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "DeviceFingerprintId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "IpIntelligenceId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Donations");
        }
    }
}
