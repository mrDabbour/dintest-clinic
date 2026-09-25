namespace dentist_clinic_api.DTOs.Dentists;

public class DentistResponseDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public string Specialty { get; set; } = string.Empty;

    public string? Biography { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}