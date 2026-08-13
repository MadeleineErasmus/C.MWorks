using JobCardApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<JobCard> JobCards => Set<JobCard>();
    public DbSet<JobCardLine> JobCardLines => Set<JobCardLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Name);
        });

        b.Entity<JobCard>(e =>
        {
            e.Property(x => x.Reference).IsRequired().HasMaxLength(40);
            e.HasIndex(x => x.Reference).IsUnique();
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Ignore(x => x.Subtotal);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.JobCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<JobCardLine>(e =>
        {
            e.Property(x => x.Description).IsRequired().HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(18, 2);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.LineTotal);
        });

        b.Entity<Invoice>(e =>
        {
            e.Property(x => x.Number).IsRequired().HasMaxLength(40);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.TaxRate).HasPrecision(9, 4);
            e.Ignore(x => x.Subtotal);
            e.Ignore(x => x.TaxAmount);
            e.Ignore(x => x.Total);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.JobCard)
                .WithMany()
                .HasForeignKey(x => x.JobCardId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<InvoiceLine>(e =>
        {
            e.Property(x => x.Description).IsRequired().HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(18, 2);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.LineTotal);
        });
    }
}
