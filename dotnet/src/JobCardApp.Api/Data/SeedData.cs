using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace JobCardApp.Api.Data;

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        EnsureAdminSeeded(db, passwordHasher);
        var defaultCompany = EnsureDefaultCompanySeeded(db);

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
            CompanyId = defaultCompany.Id,
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
            CompanyId = defaultCompany.Id,
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

    /// <summary>
    /// Bootstraps one placeholder VAT-registered company so there's always
    /// something to pick when creating a job card. Address/banking details
    /// are obvious placeholders — replace them via the Companies screen.
    /// </summary>
    private static Company EnsureDefaultCompanySeeded(AppDbContext db)
    {
        var existing = db.Companies.FirstOrDefault();
        if (existing is not null) return existing;

        var company = new Company
        {
            Name = "Your Company (Pty) Ltd — set this in Companies",
            Address = "Set your business address",
            Phone = "Set your phone number",
            Email = "Set your business email",
            IsVatRegistered = true,
            VatNumber = "Set your VAT number",
            TaxRate = 0.15m,
            BankName = "Set your bank",
            AccountHolder = "Set the account holder name",
            AccountNumber = "Set your account number",
            BranchCode = "Set your branch code",
            AccountType = "Set your account type"
        };

        db.Companies.Add(company);
        db.SaveChanges();
        return company;
    }

    /// <summary>
    /// Bootstraps the first Administrator account so there's a way to log in
    /// at all before any users exist. Dev-only credentials — change the
    /// password (or delete this account) before real deployment.
    /// </summary>
    private static void EnsureAdminSeeded(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        if (db.Users.Any()) return;

        var admin = new User
        {
            Username = "admin",
            DisplayName = "Administrator",
            Role = UserRole.Administrator
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "ChangeMe123!");

        db.Users.Add(admin);
        db.SaveChanges();
    }
}
