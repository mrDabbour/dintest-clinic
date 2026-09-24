using Microsoft.EntityFrameworkCore;
using dintest_clinic_api.Data;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Database
// -------------------------

builder.Services.AddDbContext<DintestDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// -------------------------
// API Services
// -------------------------

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// -------------------------
// Development
// -------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// -------------------------
// HTTP Pipeline
// -------------------------

// We'll configure HTTPS properly later.
// app.UseHttpsRedirection();

app.MapControllers();

// Simple test endpoint
app.MapGet("/", () => new
{
    message = "Dintest Clinic API is running",
    status = "OK"
});

app.Run();