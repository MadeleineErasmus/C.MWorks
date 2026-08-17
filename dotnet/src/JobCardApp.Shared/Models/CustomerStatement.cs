namespace JobCardApp.Shared.Models;

/// <summary>One line on a customer statement — an issued invoice (debit) or a received payment (credit). See §16.</summary>
public class StatementEntry
{
    public DateTime Date { get; set; }
    public string Document { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    /// <summary>Running balance after this entry.</summary>
    public decimal Balance { get; set; }
}

public class CustomerStatement
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<StatementEntry> Entries { get; set; } = new();
    public decimal ClosingBalance { get; set; }
}
