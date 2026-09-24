using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dintest_clinic_api.Data;
using dintest_clinic_api.Models;

using dintest_clinic_api.DTOs.Patients;


namespace dintest_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly DintestDbContext _context;

    public PatientsController(DintestDbContext context)
    {
        _context = context;
    }

    // GET: /api/patients
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
    {
        var patients = await _context.Patients.ToListAsync();

        return Ok(patients);
    }

    // GET: /api/patients/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetPatient(int id)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound();
        }

        return Ok(patient);
    }

   // POST: /api/patients
[HttpPost]
public async Task<ActionResult<Patient>> CreatePatient(CreatePatientDto dto)
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

    return CreatedAtAction(
        nameof(GetPatient),
        new { id = patient.Id },
        patient
    );
}

    // PUT: /api/patients/1
[HttpPut("{id}")]
public async Task<IActionResult> UpdatePatient(int id, Patient updatedPatient)
{
    var patient = await _context.Patients.FindAsync(id);

    if (patient == null)
    {
        return NotFound();
    }

    patient.FirstName = updatedPatient.FirstName;
    patient.LastName = updatedPatient.LastName;
    patient.Email = updatedPatient.Email;
    patient.Phone = updatedPatient.Phone;

    await _context.SaveChangesAsync();

    return NoContent();
}

// DELETE: /api/patients/2
[HttpDelete("{id}")]
public async Task<IActionResult> DeletePatient(int id)
{
    var patient = await _context.Patients.FindAsync(id);

    if (patient == null)
    {
        return NotFound();
    }

    _context.Patients.Remove(patient);

    await _context.SaveChangesAsync();

    return NoContent();
}
}