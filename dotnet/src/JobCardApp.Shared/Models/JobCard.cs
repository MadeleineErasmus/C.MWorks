namespace JobCardApp.Shared.Models;

public enum JobCardStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Invoiced = 3,
    Cancelled = 4
}

public class JobCard
{
    public int Id { get; set; }

    /// <summary>Human friendly reference, e.g. JC-2026-0001.</summary>
    public string Reference { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SiteAddress { get; set; }
    public string? Technician { get; set; }

    public JobCardStatus Status { get; set; } = JobCardStatus.Open;

    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<JobCardLine> Lines { get; set; } = new();

    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
}

public enum LineKind
{
    Labour = 0,
    Part = 1,
    Travel = 2,
    Other = 3
}

public class JobCardLine
{
    public int Id { get; set; }
    public int JobCardId { get; set; }

    public LineKind Kind { get; set; } = LineKind.Labour;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
}
