using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobCardApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IConfiguration _config;
    public SettingsController(IConfiguration config) => _config = config;

    [HttpGet("billing")]
    public ActionResult<BillingSettings> GetBilling() => new BillingSettings
    {
        TaxRate = _config.GetValue("Billing:TaxRate", 0.15m),
        PaymentTermDays = _config.GetValue("Billing:PaymentTermDays", 30),
        DefaultCallOutFee = _config.GetValue("Billing:DefaultCallOutFee", 500.00m)
    };
}
