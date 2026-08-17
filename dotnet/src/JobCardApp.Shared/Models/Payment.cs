namespace JobCardApp.Shared.Models;

/// <summary>
/// A single payment transaction from a customer. May cover more than one
/// invoice, or only part of one — see <see cref="PaymentAllocation"/>. The
/// amount is never reduced when allocating; <see cref="UnallocatedAmount"/>
/// is always computed, giving a proper audit trail (§13).
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PaymentAllocation> Allocations { get; set; } = new();

    public decimal AllocatedAmount => Math.Round(Allocations.Sum(a => a.AllocatedAmount), 2);
    public decimal UnallocatedAmount => Amount - AllocatedAmount;
}

public class PaymentAllocation
{
    public int Id { get; set; }

    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal AllocatedAmount { get; set; }
    public DateTime AllocatedDate { get; set; } = DateTime.UtcNow;
}

public class CreateAllocationRequest
{
    public int InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
}
