using JobCardApp.Shared.Models;

namespace JobCardApp.Api.Data;

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Customers.Any()) return;

        var acme = new Customer
        {
            Name = "Acme Manufacturing",
            ContactPerson = "Johan Botha",
            Email = "accounts@acme.co.za",
            Phone = "011 555 0134",
            Address = "12 Industrial Rd, Germiston"
        };

        var harbour = new Customer
        {
            Name = "Harbour Cold Storage",
            ContactPerson = "Nadia Petersen",
            Email = "nadia@harbourcold.co.za",
            Phone = "021 555 0198",
            Address = "4 Dock Rd, Cape Town"
        };

        db.Customers.AddRange(acme, harbour);
        db.SaveChanges();

        db.JobCards.Add(new JobCard
        {
            Reference = "JC-2026-0001",
            CustomerId = acme.Id,
            Title = "Compressor service",
            Description = "Annual service on line 2 compressor, replace filters.",
            SiteAddress = acme.Address,
            Technician = "Pieter",
            Status = JobCardStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            Lines =
            {
                new JobCardLine { Kind = LineKind.Labour, Description = "On-site labour", Quantity = 4, UnitPrice = 650 },
                new JobCardLine { Kind = LineKind.Part, Description = "Air filter element", Quantity = 2, UnitPrice = 480 },
                new JobCardLine { Kind = LineKind.Travel, Description = "Travel (km)", Quantity = 62, UnitPrice = 8.5m }
            }
        });

        db.JobCards.Add(new JobCard
        {
            Reference = "JC-2026-0002",
            CustomerId = harbour.Id,
            Title = "Chiller fault call-out",
            Description = "Chiller 3 tripping on high pressure.",
            SiteAddress = harbour.Address,
            Technician = "Sipho",
            Status = JobCardStatus.Open,
            Lines =
            {
                new JobCardLine { Kind = LineKind.Labour, Description = "Diagnostics", Quantity = 2, UnitPrice = 750 }
            }
        });

        db.SaveChanges();
    }
}
