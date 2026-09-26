using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dentist_clinic_api.Data;
using dentist_clinic_api.Models;
using dentist_clinic_api.DTOs.DentalServices;

namespace dentist_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DentalServicesController : ControllerBase
{
    private readonly DentistDbContext _context;

    public DentalServicesController(DentistDbContext context)
    {
        _context = context;
    }

    // GET: api/dentalservices
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DentalServiceResponseDto>>> GetDentalServices()
    {
        var services = await _context.DentalServices
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new DentalServiceResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(services);
    }

    // GET: api/dentalservices/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DentalServiceResponseDto>> GetDentalService(int id)
    {
        var service = await _context.DentalServices.FindAsync(id);

        if (service == null)
        {
            return NotFound(new
            {
                message = $"Dental service with ID {id} was not found."
            });
        }

        return Ok(MapToResponseDto(service));
    }

    // POST: api/dentalservices
    [HttpPost]
    public async Task<ActionResult<DentalServiceResponseDto>> CreateDentalService(
        CreateDentalServiceDto dto)
    {
        var service = new DentalService
        {
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            DurationMinutes = dto.DurationMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DentalServices.Add(service);
        await _context.SaveChangesAsync();

        var response = MapToResponseDto(service);

        return CreatedAtAction(
            nameof(GetDentalService),
            new { id = service.Id },
            response);
    }

    // PUT: api/dentalservices/1
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DentalServiceResponseDto>> UpdateDentalService(
        int id,
        UpdateDentalServiceDto dto)
    {
        var service = await _context.DentalServices.FindAsync(id);

        if (service == null)
        {
            return NotFound(new
            {
                message = $"Dental service with ID {id} was not found."
            });
        }

        service.Name = dto.Name.Trim();
        service.Category = dto.Category.Trim();
        service.Description = dto.Description.Trim();
        service.Price = dto.Price;
        service.DurationMinutes = dto.DurationMinutes;
        service.IsActive = dto.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToResponseDto(service));
    }

    // DELETE: api/dentalservices/1
    // Soft delete: keep the service for appointment history.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeactivateDentalService(int id)
    {
        var service = await _context.DentalServices.FindAsync(id);

        if (service == null)
        {
            return NotFound(new
            {
                message = $"Dental service with ID {id} was not found."
            });
        }

        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static DentalServiceResponseDto MapToResponseDto(
        DentalService service)
    {
        return new DentalServiceResponseDto
        {
            Id = service.Id,
            Name = service.Name,
            Category = service.Category,
            Description = service.Description,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        };
    }
}