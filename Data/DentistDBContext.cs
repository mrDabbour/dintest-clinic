using Microsoft.EntityFrameworkCore;
using dentist_clinic_api.Models;

namespace dentist_clinic_api.Data;

public class DentistDbContext : DbContext
{
    public DentistDbContext(DbContextOptions<DentistDbContext> options)
        : base(options)
    {
    }

    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Dentist> Dentists { get; set; }
    public DbSet<DentalService> DentalServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // Dentist
        // =========================
        modelBuilder.Entity<Dentist>(entity =>
        {
            entity.Property(d => d.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(d => d.Phone)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(d => d.RegistrationNumber)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.Specialty)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(d => d.Biography)
                .HasMaxLength(1000);

            entity.HasIndex(d => d.Email)
                .IsUnique();

            entity.HasIndex(d => d.RegistrationNumber)
                .IsUnique();

            entity.HasIndex(d => d.IsActive);
        });

        // =========================
        // Dental Service
        // =========================
        modelBuilder.Entity<DentalService>(entity =>
        {
            entity.Property(s => s.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(s => s.Category)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(s => s.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(s => s.Price)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(s => s.DurationMinutes)
                .IsRequired();

            entity.HasIndex(s => s.Name);
            entity.HasIndex(s => s.Category);
            entity.HasIndex(s => s.IsActive);
        });

        // =========================
        // Appointment
        // =========================
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.Property(a => a.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(a => a.Notes)
                .HasMaxLength(1000);

            // Appointment -> Patient
            entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment -> Dentist
            entity.HasOne(a => a.Dentist)
                .WithMany()
                .HasForeignKey(a => a.DentistId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment -> DentalService
            entity.HasOne(a => a.DentalService)
                .WithMany()
                .HasForeignKey(a => a.DentalServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.PatientId);
            entity.HasIndex(a => a.DentistId);
            entity.HasIndex(a => a.DentalServiceId);
            entity.HasIndex(a => a.StartTime);
            entity.HasIndex(a => a.Status);

            // Useful when checking a dentist's schedule
            entity.HasIndex(a => new
            {
                a.DentistId,
                a.StartTime
            });
        });
    }
}