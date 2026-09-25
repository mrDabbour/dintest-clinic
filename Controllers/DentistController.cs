using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dentist_clinic_api.Data;
using dentist_clinic_api.Models;
using dentist_clinic_api.DTOs.Dentists;

namespace dentist_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DentistsController : ControllerBase
{
    private readonly DentistDbContext _context;

    public DentistsController(DentistDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // GET: api/dentists
    // Get all dentists
    // ==========================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DentistResponseDto>>> GetDentists()
    {
        var dentists = await _context.Dentists
            .AsNoTracking()
            .OrderBy(d => d.LastName)
            .Select(d => new DentistResponseDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                Phone = d.Phone,
                RegistrationNumber = d.RegistrationNumber,
                Specialty = d.Specialty,
                Biography = d.Biography,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        return Ok(dentists);
    }

    // ==========================================
    // GET: api/dentists/1
    // Get one dentist by ID
    // ==========================================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DentistResponseDto>> GetDentist(int id)
    {
        var dentist = await _context.Dentists
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DentistResponseDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                Phone = d.Phone,
                RegistrationNumber = d.RegistrationNumber,
                Specialty = d.Specialty,
                Biography = d.Biography,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (dentist == null)
        {
            return NotFound(new
            {
                message = $"Dentist with ID {id} was not found."
            });
        }

        return Ok(dentist);
    }

    // ==========================================
    // POST: api/dentists
    // Create a new dentist
    // ==========================================
    [HttpPost]
    public async Task<ActionResult<DentistResponseDto>> CreateDentist(
        CreateDentistDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var registrationNumber = dto.RegistrationNumber.Trim();

        var duplicateExists = await _context.Dentists
            .AnyAsync(d =>
                d.Email == email ||
                d.RegistrationNumber == registrationNumber);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "A dentist with this email or registration number already exists."
            });
        }

        var dentist = new Dentist
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            RegistrationNumber = registrationNumber,
            Specialty = dto.Specialty.Trim(),
            Biography = dto.Biography?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Dentists.Add(dentist);

        await _context.SaveChangesAsync();

        var response = new DentistResponseDto
        {
            Id = dentist.Id,
            FirstName = dentist.FirstName,
            LastName = dentist.LastName,
            Email = dentist.Email,
            Phone = dentist.Phone,
            RegistrationNumber = dentist.RegistrationNumber,
            Specialty = dentist.Specialty,
            Biography = dentist.Biography,
            IsActive = dentist.IsActive,
            CreatedAt = dentist.CreatedAt,
            UpdatedAt = dentist.UpdatedAt
        };

        return CreatedAtAction(
            nameof(GetDentist),
            new { id = dentist.Id },
            response
        );
    }

    // ==========================================
    // PUT: api/dentists/1
    // Update an existing dentist
    // Returns: 200 OK + updated dentist
    // ==========================================
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DentistResponseDto>> UpdateDentist(
        int id,
        UpdateDentistDto dto)
    {
        var dentist = await _context.Dentists.FindAsync(id);

        if (dentist == null)
        {
            return NotFound(new
            {
                message = $"Dentist with ID {id} was not found."
            });
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var registrationNumber = dto.RegistrationNumber.Trim();

        var duplicateExists = await _context.Dentists
            .AnyAsync(d =>
                d.Id != id &&
                (d.Email == email ||
                 d.RegistrationNumber == registrationNumber));

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Another dentist already uses this email or registration number."
            });
        }

        dentist.FirstName = dto.FirstName.Trim();
        dentist.LastName = dto.LastName.Trim();
        dentist.Email = email;
        dentist.Phone = dto.Phone.Trim();
        dentist.RegistrationNumber = registrationNumber;
        dentist.Specialty = dto.Specialty.Trim();
        dentist.Biography = dto.Biography?.Trim();
        dentist.IsActive = dto.IsActive;
        dentist.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = new DentistResponseDto
        {
            Id = dentist.Id,
            FirstName = dentist.FirstName,
            LastName = dentist.LastName,
            Email = dentist.Email,
            Phone = dentist.Phone,
            RegistrationNumber = dentist.RegistrationNumber,
            Specialty = dentist.Specialty,
            Biography = dentist.Biography,
            IsActive = dentist.IsActive,
            CreatedAt = dentist.CreatedAt,
            UpdatedAt = dentist.UpdatedAt
        };

        return Ok(response);
    }

    // ==========================================
    // DELETE: api/dentists/1
    // Delete a dentist
    // ==========================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDentist(int id)
    {
        var dentist = await _context.Dentists.FindAsync(id);

        if (dentist == null)
        {
            return NotFound(new
            {
                message = $"Dentist with ID {id} was not found."
            });
        }

        _context.Dentists.Remove(dentist);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}