using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DonationFraud.API.Data;

#nullable disable

namespace DonationFraud.API.Migrations
{
    [DbContext(typeof(DonationDbContext))]
    [Migration("20260523000000_AddIsActiveToCampaign")]
    /// <inheritdoc />
    public partial class AddIsActiveToCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Campaigns");
        }
    }
}
