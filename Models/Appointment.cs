namespace dentist_clinic_api.Models;

public class Appointment
{
    public int Id { get; set; }

    // Foreign keys
    public int PatientId { get; set; }
    public int DentistId { get; set; }
    public int DentalServiceId { get; set; }

    // Booking information
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string Status { get; set; } = "Pending";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Dentist Dentist { get; set; } = null!;
    public DentalService DentalService { get; set; } = null!;
}