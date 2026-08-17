using JobCardApp.Api.Data;
using JobCardApp.Shared;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobCardsController : ControllerBase
{
    private readonly AppDbContext _db;
    public JobCardsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<JobCard>>> GetAll([FromQuery] JobCardStatus? status)
    {
        var query = _db.JobCards
            .Include(j => j.Customer)
            .Include(j => j.Company)
            .Include(j => j.Lines).ThenInclude(l => l.CustomerItem)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        return await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobCard>> Get(int id)
    {
        var jobCard = await _db.JobCards
            .Include(j => j.Customer)
            .Include(j => j.Company)
            .Include(j => j.Lines).ThenInclude(l => l.CustomerItem)
            .FirstOrDefaultAsync(j => j.Id == id);

        return jobCard is null ? NotFound() : jobCard;
    }

    [HttpPost]
    public async Task<ActionResult<JobCard>> Create(JobCard jobCard)
    {
        jobCard.Id = 0;
        jobCard.Customer = null;
        jobCard.Company = null;
        jobCard.CreatedAt = DateTime.UtcNow;

        // Status is server-owned from creation onward — see /complete, /cancel,
        // and POST /api/Invoices/from-jobcard/{id} for the only valid transitions.
        jobCard.Status = JobCardStatus.Open;
        jobCard.CompletedAt = null;

        if (string.IsNullOrWhiteSpace(jobCard.Reference))
            jobCard.Reference = InvoiceFactory.NextReference("JC", await NextSequenceAsync());

        foreach (var line in jobCard.Lines)
        {
            line.Id = 0;
            // Only the FK should travel with the line — a populated nav object
            // (set client-side so a brand-new item's name shows immediately)
            // would otherwise make EF try to re-insert an already-existing item.
            line.CustomerItem = null;
        }

        _db.JobCards.Add(jobCard);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = jobCard.Id }, jobCard);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, JobCard jobCard)
    {
        var existing = await _db.JobCards
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (existing is null) return NotFound();

        existing.CustomerId = jobCard.CustomerId;
        existing.CompanyId = jobCard.CompanyId;
        existing.Title = jobCard.Title;
        existing.Description = jobCard.Description;
        existing.SiteAddress = jobCard.SiteAddress;
        existing.Technician = jobCard.Technician;
        existing.ScheduledFor = jobCard.ScheduledFor;

        // Status/CompletedAt are intentionally NOT settable here — the client
        // cannot move a job card into an arbitrary status via a plain edit.
        // Use POST {id}/complete, POST {id}/cancel, or the invoice-creation
        // endpoint, which validate the transition server-side.

        // Simple replace-all strategy — fine for a base, swap for a diff later.
        _db.JobCardLines.RemoveRange(existing.Lines);
        existing.Lines = jobCard.Lines.Select(l => new JobCardLine
        {
            Kind = l.Kind,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            CustomerItemId = l.CustomerItemId
        }).ToList();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = $"{nameof(UserRole.Technician)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)},{nameof(UserRole.Administrator)}")]
    public async Task<ActionResult<JobCard>> Complete(int id)
    {
        var jobCard = await _db.JobCards.FindAsync(id);
        if (jobCard is null) return NotFound();

        if (jobCard.Status is JobCardStatus.Invoiced or JobCardStatus.Cancelled)
            return Conflict($"Job card is {jobCard.Status} and can no longer be completed.");

        jobCard.Status = JobCardStatus.Completed;
        jobCard.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return jobCard;
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = $"{nameof(UserRole.Office)},{nameof(UserRole.Manager)},{nameof(UserRole.Administrator)}")]
    public async Task<ActionResult<JobCard>> Cancel(int id)
    {
        var jobCard = await _db.JobCards.FindAsync(id);
        if (jobCard is null) return NotFound();

        if (jobCard.Status == JobCardStatus.Invoiced)
            return Conflict("An invoiced job card cannot be cancelled — reverse the invoice instead.");
        if (jobCard.Status == JobCardStatus.Cancelled)
            return Conflict("Job card is already cancelled.");

        jobCard.Status = JobCardStatus.Cancelled;
        await _db.SaveChangesAsync();
        return jobCard;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.JobCards.FindAsync(id);
        if (existing is null) return NotFound();

        _db.JobCards.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<int> NextSequenceAsync()
        => await _db.JobCards.CountAsync(j => j.CreatedAt.Year == DateTime.UtcNow.Year) + 1;
}
