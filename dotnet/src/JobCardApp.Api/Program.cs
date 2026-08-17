using System.Text;
using System.Text.Json.Serialization;
using JobCardApp.Api.Data;
using JobCardApp.Api.Services;
using JobCardApp.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Required by QuestPDF at startup — Community license is free for small
// businesses/individuals, which fits this app (§ decision).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString(provider)
    ?? "Data Source=jobcards.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connectionString);
    else
        options.UseSqlite(connectionString);
});

// PaymentAllocation.Invoice <-> Invoice.Allocations is a genuine
// bidirectional nav (unlike JobCardLine/InvoiceLine, which deliberately
// have no back-reference) — EF Core's relationship fixup wires both sides
// in memory, which System.Text.Json won't serialize without this.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();

// NOTE: Swashbuckle 10.x (net10) reworked its OpenAPI model namespaces; the
// Swagger UI "Authorize" bearer-token button isn't wired up here as a
// result. Auth itself is unaffected — use a login response's token directly
// (e.g. via curl/Postman/the mobile client) when calling protected routes.
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<EmailService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "JobCardApp.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "JobCardApp.Mobile";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// The mobile app talks to this API directly, but CORS keeps a future web
// dashboard (or MAUI Blazor hybrid) working too.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    db.Database.Migrate();
    SeedData.EnsureSeeded(db, hasher);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
