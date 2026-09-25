namespace dentist_clinic_api.DTOs.Appointments;

public class AppointmentResponseDto
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public int DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;

    public int DentalServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}