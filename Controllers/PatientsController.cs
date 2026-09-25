using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dentist_clinic_api.Data;
using dentist_clinic_api.Models;
using dentist_clinic_api.DTOs.Patients;

namespace dentist_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly DentistDbContext _context;

    public PatientsController(DentistDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // GET: api/patients
    // Get all patients
    // ==========================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetPatients()
    {
        var patients = await _context.Patients
            .AsNoTracking()
            .Select(patient => new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                CreatedAt = patient.CreatedAt
            })
            .ToListAsync();

        return Ok(patients);
    }

    // ==========================================
    // GET: api/patients/1
    // Get one patient by ID
    // ==========================================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientResponseDto>> GetPatient(int id)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Where(patient => patient.Id == id)
            .Select(patient => new PatientResponseDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                CreatedAt = patient.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (patient == null)
        {
            return NotFound(new
            {
                message = $"Patient with ID {id} was not found."
            });
        }

        return Ok(patient);
    }

    // ==========================================
    // POST: api/patients
    // Create a new patient
    // ==========================================
    [HttpPost]
    public async Task<ActionResult<PatientResponseDto>> CreatePatient(
        CreatePatientDto dto)
    {
        var patient = new Patient
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Patients.Add(patient);

        await _context.SaveChangesAsync();

        var response = new PatientResponseDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email,
            Phone = patient.Phone,
            CreatedAt = patient.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetPatient),
            new { id = patient.Id },
            response
        );
    }

// ==========================================
// PUT: api/patients/1
// Update an existing patient
// Returns: 200 OK + updated patient
// ==========================================
[HttpPut("{id:int}")]
public async Task<ActionResult<PatientResponseDto>> UpdatePatient(
    int id,
    UpdatePatientDto dto)
{
    var patient = await _context.Patients.FindAsync(id);

    if (patient == null)
    {
        return NotFound(new
        {
            message = $"Patient with ID {id} was not found."
        });
    }

    patient.FirstName = dto.FirstName.Trim();
    patient.LastName = dto.LastName.Trim();
    patient.Email = dto.Email.Trim().ToLowerInvariant();
    patient.Phone = dto.Phone.Trim();

    await _context.SaveChangesAsync();

    var response = new PatientResponseDto
    {
        Id = patient.Id,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        Email = patient.Email,
        Phone = patient.Phone,
        CreatedAt = patient.CreatedAt
    };

    return Ok(response);
}

    // ==========================================
    // DELETE: api/patients/1
    // Delete a patient
    // ==========================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound(new
            {
                message = $"Patient with ID {id} was not found."
            });
        }

        _context.Patients.Remove(patient);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}