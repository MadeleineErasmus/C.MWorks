using JobCardApp.Api.Data;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public CompaniesController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<List<Company>>> GetAll()
        => await _db.Companies.OrderBy(c => c.Name).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Company>> Get(int id)
        => await _db.Companies.FindAsync(id) is { } c ? c : NotFound();

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<ActionResult<Company>> Create(Company company)
    {
        company.Id = 0;
        company.CreatedAt = DateTime.UtcNow;
        NormalizeTaxRate(company);

        _db.Companies.Add(company);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = company.Id }, company);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> Update(int id, Company company)
    {
        var existing = await _db.Companies.FindAsync(id);
        if (existing is null) return NotFound();

        NormalizeTaxRate(company);

        existing.Name = company.Name;
        existing.Address = company.Address;
        existing.Phone = company.Phone;
        existing.Email = company.Email;
        existing.IsVatRegistered = company.IsVatRegistered;
        existing.VatNumber = company.VatNumber;
        existing.TaxRate = company.TaxRate;
        existing.BankName = company.BankName;
        existing.AccountHolder = company.AccountHolder;
        existing.AccountNumber = company.AccountNumber;
        existing.BranchCode = company.BranchCode;
        existing.AccountType = company.AccountType;
        existing.IsActive = company.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Office)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Companies.FindAsync(id);
        if (existing is null) return NotFound();

        if (await _db.JobCards.AnyAsync(j => j.CompanyId == id) || await _db.Invoices.AnyAsync(i => i.CompanyId == id))
            return Conflict("This company has job cards or invoices against it — deactivate it instead of deleting.");

        _db.Companies.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// VAT status is authoritative for tax rate — a non-VAT company is
    /// always 0%, and a VAT company with no rate specified falls back to the
    /// configured business default rather than silently invoicing at 0%.
    /// </summary>
    private void NormalizeTaxRate(Company company)
    {
        if (!company.IsVatRegistered)
        {
            company.TaxRate = 0m;
            return;
        }

        if (company.TaxRate <= 0)
            company.TaxRate = _config.GetValue("Billing:TaxRate", 0.15m);
    }
}
