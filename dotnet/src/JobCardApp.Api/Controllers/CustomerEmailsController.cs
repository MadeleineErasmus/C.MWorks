using JobCardApp.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobCardApp.Api.Controllers;

// Same reasoning as CustomerItemsController owning its own item-level
// actions rather than nesting under CustomersController.
[ApiController]
[Route("api/customer-emails")]
[Authorize]
public class CustomerEmailsController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomerEmailsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Removes an additional recipient address — typos happen, and there's
    /// no reason to keep a bad address around forever.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.CustomerEmails.FindAsync(id);
        if (existing is null) return NotFound();

        _db.CustomerEmails.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
