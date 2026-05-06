using System.Linq;
using DonationFraud.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DonationFraud.Tests.Controllers
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Add DonationDbContext using an in-memory database for testing.
                // We don't need to remove the existing one because Program.cs 
                // will skip registering SQL Server in the "Testing" environment.
                services.AddDbContext<DonationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database
                // context (DonationDbContext).
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<DonationDbContext>();

                    // Ensure the database is created.
                    db.Database.EnsureCreated();

                    // Seed data for tests if necessary here
                }
            });
        }
    }
}
