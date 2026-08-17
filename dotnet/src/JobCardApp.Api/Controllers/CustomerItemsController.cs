using JobCardApp.Api.Data;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

// Item-level actions (history) don't belong under a customer route prefix —
// same reasoning as QuotesController/InvoicesController owning their own
// item-level actions rather than nesting under another controller.
[ApiController]
[Route("api/customer-items")]
[Authorize]
public class CustomerItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomerItemsController(AppDbContext db) => _db = db;

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerItem>> Get(int id)
        => await _db.CustomerItems.FindAsync(id) is { } item ? item : NotFound();

    /// <summary>
    /// Every job card line across all of this customer's job cards that
    /// references this item, newest first — "all replacements/changes on
    /// that item" from the customer's equipment view.
    /// </summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<CustomerItemHistoryEntry>>> GetHistory(int id)
    {
        if (!await _db.CustomerItems.AnyAsync(i => i.Id == id))
            return NotFound();

        var query =
            from line in _db.JobCardLines
            join jobCard in _db.JobCards on line.JobCardId equals jobCard.Id
            where line.CustomerItemId == id
            select new { line, jobCard };

        return await query
            .OrderByDescending(x => x.jobCard.CompletedAt ?? x.jobCard.CreatedAt)
            .Select(x => new CustomerItemHistoryEntry
            {
                JobCardReference = x.jobCard.Reference,
                JobCardTitle = x.jobCard.Title,
                Description = x.line.Description,
                Kind = x.line.Kind,
                Quantity = x.line.Quantity,
                UnitPrice = x.line.UnitPrice,
                LineTotal = Math.Round(x.line.Quantity * x.line.UnitPrice, 2),
                Date = x.jobCard.CompletedAt ?? x.jobCard.CreatedAt
            })
            .ToListAsync();
    }
}
