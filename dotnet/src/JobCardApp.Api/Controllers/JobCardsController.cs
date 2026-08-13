using JobCardApp.Api.Data;
using JobCardApp.Shared;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobCardsController : ControllerBase
{
    private readonly AppDbContext _db;
    public JobCardsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<JobCard>>> GetAll([FromQuery] JobCardStatus? status)
    {
        var query = _db.JobCards
            .Include(j => j.Customer)
            .Include(j => j.Lines)
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
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id);

        return jobCard is null ? NotFound() : jobCard;
    }

    [HttpPost]
    public async Task<ActionResult<JobCard>> Create(JobCard jobCard)
    {
        jobCard.Id = 0;
        jobCard.Customer = null;
        jobCard.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(jobCard.Reference))
            jobCard.Reference = InvoiceFactory.NextReference("JC", await NextSequenceAsync());

        foreach (var line in jobCard.Lines) line.Id = 0;

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
        existing.Title = jobCard.Title;
        existing.Description = jobCard.Description;
        existing.SiteAddress = jobCard.SiteAddress;
        existing.Technician = jobCard.Technician;
        existing.ScheduledFor = jobCard.ScheduledFor;
        existing.Status = jobCard.Status;
        existing.CompletedAt = jobCard.Status == JobCardStatus.Completed
            ? existing.CompletedAt ?? DateTime.UtcNow
            : jobCard.CompletedAt;

        // Simple replace-all strategy — fine for a base, swap for a diff later.
        _db.JobCardLines.RemoveRange(existing.Lines);
        existing.Lines = jobCard.Lines.Select(l => new JobCardLine
        {
            Kind = l.Kind,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
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
