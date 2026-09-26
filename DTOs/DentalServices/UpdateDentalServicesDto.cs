using System.ComponentModel.DataAnnotations;

namespace dentist_clinic_api.DTOs.DentalServices;

public class UpdateDentalServiceDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; }
}