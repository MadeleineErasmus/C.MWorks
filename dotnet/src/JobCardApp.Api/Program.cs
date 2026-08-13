using JobCardApp.Api.Data;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The mobile app talks to this API directly, but CORS keeps a future web
// dashboard (or MAUI Blazor hybrid) working too.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.EnsureSeeded(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
