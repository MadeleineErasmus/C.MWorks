using JobCardApp.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobCardApp.Api.Controllers;

// Same reasoning as CustomerEmailsController owning its own site-level
// actions rather than nesting under CustomersController.
[ApiController]
[Route("api/customer-sites")]
[Authorize]
public class CustomerSitesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomerSitesController(AppDbContext db) => _db = db;

    /// <summary>
    /// Removes a saved site — no history depends on it (unlike CustomerItem),
    /// so it's a straightforward delete.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.CustomerSites.FindAsync(id);
        if (existing is null) return NotFound();

        _db.CustomerSites.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
