using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dentist_clinic_api.Data;
using dentist_clinic_api.DTOs.Appointments;
using dentist_clinic_api.Models;

namespace dentist_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly DentistDbContext _context;

    private static readonly string[] AllowedStatuses =
    {
        "Pending",
        "Confirmed",
        "Completed",
        "Cancelled",
        "NoShow"
    };

    public AppointmentsController(DentistDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: api/appointments
    // =========================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetAppointments()
    {
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .Include(a => a.DentalService)
            .OrderBy(a => a.StartTime)
            .Select(a => new AppointmentResponseDto
            {
                Id = a.Id,

                PatientId = a.PatientId,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,

                DentistId = a.DentistId,
                DentistName = a.Dentist.FirstName + " " + a.Dentist.LastName,

                DentalServiceId = a.DentalServiceId,
                ServiceName = a.DentalService.Name,

                Price = a.DentalService.Price,

                StartTime = a.StartTime,
                EndTime = a.EndTime,

                Status = a.Status,
                Notes = a.Notes,

                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // =========================================================
    // GET: api/appointments/5
    // =========================================================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentResponseDto>> GetAppointment(int id)
    {
        var appointment = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AppointmentResponseDto
            {
                Id = a.Id,

                PatientId = a.PatientId,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,

                DentistId = a.DentistId,
                DentistName = a.Dentist.FirstName + " " + a.Dentist.LastName,

                DentalServiceId = a.DentalServiceId,
                ServiceName = a.DentalService.Name,

                Price = a.DentalService.Price,

                StartTime = a.StartTime,
                EndTime = a.EndTime,

                Status = a.Status,
                Notes = a.Notes,

                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (appointment == null)
        {
            return NotFound(new
            {
                message = $"Appointment with ID {id} was not found."
            });
        }

        return Ok(appointment);
    }

    // =========================================================
    // POST: api/appointments
    // =========================================================
    [HttpPost]
    public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment(
        CreateAppointmentDto dto)
    {
        // 1. Validate patient
        var patient = await _context.Patients
            .FindAsync(dto.PatientId);

        if (patient == null)
        {
            return BadRequest(new
            {
                message = "The selected patient does not exist."
            });
        }

        // 2. Validate dentist
        var dentist = await _context.Dentists
            .FindAsync(dto.DentistId);

        if (dentist == null)
        {
            return BadRequest(new
            {
                message = "The selected dentist does not exist."
            });
        }

        if (!dentist.IsActive)
        {
            return BadRequest(new
            {
                message = "The selected dentist is not currently active."
            });
        }

        // 3. Validate service
        var service = await _context.DentalServices
            .FindAsync(dto.DentalServiceId);

        if (service == null)
        {
            return BadRequest(new
            {
                message = "The selected dental service does not exist."
            });
        }

        if (!service.IsActive)
        {
            return BadRequest(new
            {
                message = "The selected dental service is not currently active."
            });
        }

        // 4. Validate appointment time
        if (dto.StartTime <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Appointment time must be in the future."
            });
        }

        // 5. Backend determines duration
        var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

        // 6. Check dentist availability
        //
        // Existing:       |-----------|
        // Requested:          |-----------|
        //
        // Overlap when:
        // existing.Start < requested.End
        // AND
        // existing.End > requested.Start

        var dentistHasConflict = await _context.Appointments
            .AnyAsync(a =>
                a.DentistId == dto.DentistId &&
                a.Status != "Cancelled" &&
                a.StartTime < endTime &&
                a.EndTime > dto.StartTime);

        if (dentistHasConflict)
        {
            return Conflict(new
            {
                message =
                    "The dentist already has another appointment during this time."
            });
        }

        // 7. Optional: prevent the patient being booked twice
        var patientHasConflict = await _context.Appointments
            .AnyAsync(a =>
                a.PatientId == dto.PatientId &&
                a.Status != "Cancelled" &&
                a.StartTime < endTime &&
                a.EndTime > dto.StartTime);

        if (patientHasConflict)
        {
            return Conflict(new
            {
                message =
                    "The patient already has another appointment during this time."
            });
        }

        // 8. Create appointment
        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DentistId = dentist.Id,
            DentalServiceId = service.Id,

            StartTime = dto.StartTime,
            EndTime = endTime,

            Status = "Pending",

            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim(),

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);

        await _context.SaveChangesAsync();

        var response = MapAppointment(
            appointment,
            patient,
            dentist,
            service);

        return CreatedAtAction(
            nameof(GetAppointment),
            new { id = appointment.Id },
            response);
    }

    // =========================================================
    // PUT: api/appointments/5
    // =========================================================
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointment(
        int id,
        UpdateAppointmentDto dto)
    {
        var appointment = await _context.Appointments
            .FindAsync(id);

        if (appointment == null)
        {
            return NotFound(new
            {
                message = $"Appointment with ID {id} was not found."
            });
        }

        // Validate status
        var normalizedStatus = AllowedStatuses.FirstOrDefault(
            status =>
                status.Equals(
                    dto.Status.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (normalizedStatus == null)
        {
            return BadRequest(new
            {
                message =
                    "Status must be Pending, Confirmed, Completed, Cancelled or NoShow."
            });
        }

        // Validate patient
        var patient = await _context.Patients
            .FindAsync(dto.PatientId);

        if (patient == null)
        {
            return BadRequest(new
            {
                message = "The selected patient does not exist."
            });
        }

        // Validate dentist
        var dentist = await _context.Dentists
            .FindAsync(dto.DentistId);

        if (dentist == null)
        {
            return BadRequest(new
            {
                message = "The selected dentist does not exist."
            });
        }

        if (!dentist.IsActive)
        {
            return BadRequest(new
            {
                message = "The selected dentist is not currently active."
            });
        }

        // Validate service
        var service = await _context.DentalServices
            .FindAsync(dto.DentalServiceId);

        if (service == null)
        {
            return BadRequest(new
            {
                message = "The selected dental service does not exist."
            });
        }

        if (!service.IsActive)
        {
            return BadRequest(new
            {
                message = "The selected dental service is not currently active."
            });
        }

        var endTime =
            dto.StartTime.AddMinutes(service.DurationMinutes);

        // Cancelled appointments don't need availability checks.
        if (normalizedStatus != "Cancelled")
        {
            var dentistHasConflict = await _context.Appointments
                .AnyAsync(a =>
                    a.Id != id &&
                    a.DentistId == dto.DentistId &&
                    a.Status != "Cancelled" &&
                    a.StartTime < endTime &&
                    a.EndTime > dto.StartTime);

            if (dentistHasConflict)
            {
                return Conflict(new
                {
                    message =
                        "The dentist already has another appointment during this time."
                });
            }

            var patientHasConflict = await _context.Appointments
                .AnyAsync(a =>
                    a.Id != id &&
                    a.PatientId == dto.PatientId &&
                    a.Status != "Cancelled" &&
                    a.StartTime < endTime &&
                    a.EndTime > dto.StartTime);

            if (patientHasConflict)
            {
                return Conflict(new
                {
                    message =
                        "The patient already has another appointment during this time."
                });
            }
        }

        appointment.PatientId = patient.Id;
        appointment.DentistId = dentist.Id;
        appointment.DentalServiceId = service.Id;

        appointment.StartTime = dto.StartTime;
        appointment.EndTime = endTime;

        appointment.Status = normalizedStatus;

        appointment.Notes =
            string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim();

        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = MapAppointment(
            appointment,
            patient,
            dentist,
            service);

        return Ok(response);
    }

    // =========================================================
    // DELETE: api/appointments/5
    // =========================================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointment = await _context.Appointments
            .FindAsync(id);

        if (appointment == null)
        {
            return NotFound(new
            {
                message = $"Appointment with ID {id} was not found."
            });
        }

        _context.Appointments.Remove(appointment);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================
    // Mapping helper
    // =========================================================
    private static AppointmentResponseDto MapAppointment(
        Appointment appointment,
        Patient patient,
        Dentist dentist,
        DentalService service)
    {
        return new AppointmentResponseDto
        {
            Id = appointment.Id,

            PatientId = patient.Id,
            PatientName =
                $"{patient.FirstName} {patient.LastName}",

            DentistId = dentist.Id,
            DentistName =
                $"{dentist.FirstName} {dentist.LastName}",

            DentalServiceId = service.Id,
            ServiceName = service.Name,

            Price = service.Price,

            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,

            Status = appointment.Status,
            Notes = appointment.Notes,

            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }
}