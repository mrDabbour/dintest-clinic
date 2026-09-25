using System.ComponentModel.DataAnnotations;

namespace dentist_clinic_api.DTOs.Patients;

public class UpdatePatientDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;
}