using JobCardApp.Api.Data;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll()
        => await _db.Customers.OrderBy(c => c.Name).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> Get(int id)
        => await _db.Customers.FindAsync(id) is { } c ? c : NotFound();

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(Customer customer)
    {
        customer.Id = 0;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Customer customer)
    {
        var existing = await _db.Customers.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Name = customer.Name;
        existing.ContactPerson = customer.ContactPerson;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;
        existing.VatNumber = customer.VatNumber;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Customers.FindAsync(id);
        if (existing is null) return NotFound();

        _db.Customers.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
