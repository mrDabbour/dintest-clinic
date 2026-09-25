using Microsoft.EntityFrameworkCore;
using dentist_clinic_api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DentistDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapGet("/", () => new
{
    message = "Dentist Clinic API is running",
    status = "OK"
});

app.Run();