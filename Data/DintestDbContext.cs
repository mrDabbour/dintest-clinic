using Microsoft.EntityFrameworkCore;
using dintest_clinic_api.Models;

namespace dintest_clinic_api.Data;

public class DintestDbContext : DbContext
{
    public DintestDbContext(DbContextOptions<DintestDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }
}