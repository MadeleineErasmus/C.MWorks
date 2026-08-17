using JobCardApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerItem> CustomerItems => Set<CustomerItem>();
    public DbSet<JobCard> JobCards => Set<JobCard>();
    public DbSet<JobCardLine> JobCardLines => Set<JobCardLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Name);
        });

        b.Entity<CustomerItem>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => new { x.CustomerId, x.Name });

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
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

            e.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
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

            // Optional — a line can reference a piece of customer equipment,
            // but if that item is ever removed the line itself must survive
            // (history is otherwise blocked at the delete endpoint instead).
            e.HasOne(x => x.CustomerItem)
                .WithMany()
                .HasForeignKey(x => x.CustomerItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Invoice>(e =>
        {
            e.Property(x => x.Number).IsRequired().HasMaxLength(40);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.TaxRate).HasPrecision(9, 4);
            e.Ignore(x => x.Subtotal);
            e.Ignore(x => x.TaxAmount);
            e.Ignore(x => x.Total);
            e.Ignore(x => x.AllocatedAmount);
            e.Ignore(x => x.OutstandingAmount);
            e.Ignore(x => x.CanSend);
            e.Ignore(x => x.CanRevise);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.JobCard)
                .WithMany()
                .HasForeignKey(x => x.JobCardId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

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

        b.Entity<User>(e =>
        {
            e.Property(x => x.Username).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.PasswordHash).IsRequired();
        });

        b.Entity<Company>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.TaxRate).HasPrecision(9, 4);
        });

        b.Entity<Quote>(e =>
        {
            e.Property(x => x.Number).IsRequired().HasMaxLength(40);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.TaxRate).HasPrecision(9, 4);
            e.Ignore(x => x.Subtotal);
            e.Ignore(x => x.TaxAmount);
            e.Ignore(x => x.Total);
            e.Ignore(x => x.CanSend);
            e.Ignore(x => x.CanAcceptOrReject);
            e.Ignore(x => x.CanConvertToInvoice);
            e.Ignore(x => x.CanDelete);
            e.Ignore(x => x.CanRevise);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.JobCard)
                .WithMany()
                .HasForeignKey(x => x.JobCardId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<QuoteLine>(e =>
        {
            e.Property(x => x.Description).IsRequired().HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(18, 2);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.LineTotal);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(x => x.Reference).HasMaxLength(100);
            e.Property(x => x.PaymentMethod).HasMaxLength(100);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Ignore(x => x.AllocatedAmount);
            e.Ignore(x => x.UnallocatedAmount);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Allocations)
                .WithOne()
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PaymentAllocation>(e =>
        {
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);

            // Wires up Invoice.Allocations too — configured from this side
            // so it isn't declared twice.
            e.HasOne(x => x.Invoice)
                .WithMany(i => i.Allocations)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
