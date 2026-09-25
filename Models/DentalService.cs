namespace dentist_clinic_api.Models;

public class DentalService
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}