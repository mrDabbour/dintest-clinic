using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using dentist_clinic_api.Data;
using dentist_clinic_api.DTOs.Appointments;
using dentist_clinic_api.DTOs.Auth;
using dentist_clinic_api.Models;
using dentist_clinic_api.Models.Auth;

namespace dentist_clinic_api.Tests;

public class AppointmentBusinessLogicTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AppointmentBusinessLogicTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AppointmentBusinessRules_WorkCorrectly()
    {
        const string adminEmail =
            "appointment.test.admin@dentistclinic.test";

        const string password =
            "IntegrationTest123!";

        using var scope =
            _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<DentistDbContext>();

        // -------------------------------------------------
        // Clean leftovers from an interrupted previous test
        // -------------------------------------------------

        var oldAppointments = await db.Appointments
            .Where(a =>
                a.Patient.Email.StartsWith("appointment.test.") ||
                a.Dentist.Email.StartsWith("appointment.test."))
            .ToListAsync();

        db.Appointments.RemoveRange(oldAppointments);

        var oldPatients = await db.Patients
            .Where(p => p.Email.StartsWith("appointment.test."))
            .ToListAsync();

        db.Patients.RemoveRange(oldPatients);

        var oldDentists = await db.Dentists
            .Where(d => d.Email.StartsWith("appointment.test."))
            .ToListAsync();

        db.Dentists.RemoveRange(oldDentists);

        var oldServices = await db.DentalServices
            .Where(s => s.Name.StartsWith("Appointment Test"))
            .ToListAsync();

        db.DentalServices.RemoveRange(oldServices);

        var oldUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (oldUser != null)
        {
            db.Users.Remove(oldUser);
        }

        await db.SaveChangesAsync();

        // -------------------------------------------------
        // Create temporary Admin
        // -------------------------------------------------

        var admin = new User
        {
            FirstName = "Appointment",
            LastName = "TestAdmin",
            Email = adminEmail,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var passwordHasher =
            new PasswordHasher<User>();

        admin.PasswordHash =
            passwordHasher.HashPassword(
                admin,
                password);

        db.Users.Add(admin);

        // -------------------------------------------------
        // Create temporary patients
        // -------------------------------------------------

        var patientOne = new Patient
        {
            FirstName = "Appointment",
            LastName = "PatientOne",
            Email = "appointment.test.patient1@example.test",
            Phone = "0200000001"
        };

        var patientTwo = new Patient
        {
            FirstName = "Appointment",
            LastName = "PatientTwo",
            Email = "appointment.test.patient2@example.test",
            Phone = "0200000002"
        };

        db.Patients.AddRange(
            patientOne,
            patientTwo);

        // -------------------------------------------------
        // Create temporary dentists
        // -------------------------------------------------

        var activeDentist = new Dentist
        {
            FirstName = "Appointment",
            LastName = "ActiveDentist",
            Email = "appointment.test.active.dentist@example.test",
            Phone = "0200000010",
            RegistrationNumber = "TEST-DENTIST-ACTIVE",
            Specialty = "General Dentistry",
            Biography = "Integration test dentist.",
            IsActive = true
        };

        var secondDentist = new Dentist
        {
            FirstName = "Appointment",
            LastName = "SecondDentist",
            Email = "appointment.test.second.dentist@example.test",
            Phone = "0200000011",
            RegistrationNumber = "TEST-DENTIST-SECOND",
            Specialty = "General Dentistry",
            Biography = "Integration test dentist.",
            IsActive = true
        };

        var inactiveDentist = new Dentist
        {
            FirstName = "Appointment",
            LastName = "InactiveDentist",
            Email = "appointment.test.inactive.dentist@example.test",
            Phone = "0200000012",
            RegistrationNumber = "TEST-DENTIST-INACTIVE",
            Specialty = "General Dentistry",
            Biography = "Integration test dentist.",
            IsActive = false
        };

        db.Dentists.AddRange(
            activeDentist,
            secondDentist,
            inactiveDentist);

        // -------------------------------------------------
        // Create temporary services
        // -------------------------------------------------

        var activeService = new DentalService
        {
            Name = "Appointment Test Active Service",
            Category = "Testing",
            Description = "Integration test service.",
            Price = 100,
            DurationMinutes = 60,
            IsActive = true
        };

        var inactiveService = new DentalService
        {
            Name = "Appointment Test Inactive Service",
            Category = "Testing",
            Description = "Integration test service.",
            Price = 100,
            DurationMinutes = 60,
            IsActive = false
        };

        db.DentalServices.AddRange(
            activeService,
            inactiveService);

        await db.SaveChangesAsync();

        try
        {
            // -------------------------------------------------
            // Login and obtain JWT
            // -------------------------------------------------

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        email = adminEmail,
                        password
                    });

            Assert.Equal(
                HttpStatusCode.OK,
                loginResponse.StatusCode);

            var auth =
                await loginResponse.Content
                    .ReadFromJsonAsync<AuthResponseDto>();

            Assert.NotNull(auth);
            Assert.False(
                string.IsNullOrWhiteSpace(auth.Token));

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    auth.Token);

            // Use a stable future date.
            var startTime =
                DateTime.UtcNow
                    .AddDays(30)
                    .Date
                    .AddHours(10);

            // =================================================
            // 1. VALID BOOKING → 201
            // =================================================

            var validResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = activeDentist.Id,
                        DentalServiceId = activeService.Id,
                        StartTime = startTime,
                        Notes = "Integration test appointment."
                    });

            Assert.Equal(
                HttpStatusCode.Created,
                validResponse.StatusCode);

            var createdAppointment =
                await validResponse.Content
                    .ReadFromJsonAsync<AppointmentResponseDto>();

            Assert.NotNull(createdAppointment);

            Assert.True(
                createdAppointment.Id > 0);

            Assert.Equal(
                "Pending",
                createdAppointment.Status);

            Assert.Equal(
                startTime,
                createdAppointment.StartTime);

            Assert.Equal(
                startTime.AddMinutes(
                    activeService.DurationMinutes),
                createdAppointment.EndTime);

            // =================================================
            // 2. DENTIST OVERLAP → 409
            //
            // Different patient, same dentist, overlapping time.
            // =================================================

            var dentistConflictResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientTwo.Id,
                        DentistId = activeDentist.Id,
                        DentalServiceId = activeService.Id,
                        StartTime = startTime.AddMinutes(30),
                        Notes = "Dentist conflict test."
                    });

            Assert.Equal(
                HttpStatusCode.Conflict,
                dentistConflictResponse.StatusCode);

            // =================================================
            // 3. PATIENT OVERLAP → 409
            //
            // Same patient, different dentist, overlapping time.
            // =================================================

            var patientConflictResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = secondDentist.Id,
                        DentalServiceId = activeService.Id,
                        StartTime = startTime.AddMinutes(30),
                        Notes = "Patient conflict test."
                    });

            Assert.Equal(
                HttpStatusCode.Conflict,
                patientConflictResponse.StatusCode);

            // =================================================
            // 4. INVALID PATIENT → 400
            // =================================================

            var invalidPatientResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = int.MaxValue,
                        DentistId = activeDentist.Id,
                        DentalServiceId = activeService.Id,
                        StartTime = startTime.AddDays(1)
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                invalidPatientResponse.StatusCode);

            // =================================================
            // 5. INVALID DENTIST → 400
            // =================================================

            var invalidDentistResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = int.MaxValue,
                        DentalServiceId = activeService.Id,
                        StartTime = startTime.AddDays(2)
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                invalidDentistResponse.StatusCode);

            // =================================================
            // 6. INVALID SERVICE → 400
            // =================================================

            var invalidServiceResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = activeDentist.Id,
                        DentalServiceId = int.MaxValue,
                        StartTime = startTime.AddDays(3)
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                invalidServiceResponse.StatusCode);

            // =================================================
            // 7. INACTIVE DENTIST → 400
            // =================================================

            var inactiveDentistResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = inactiveDentist.Id,
                        DentalServiceId = activeService.Id,
                        StartTime = startTime.AddDays(4)
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                inactiveDentistResponse.StatusCode);

            // =================================================
            // 8. INACTIVE SERVICE → 400
            // =================================================

            var inactiveServiceResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = activeDentist.Id,
                        DentalServiceId = inactiveService.Id,
                        StartTime = startTime.AddDays(5)
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                inactiveServiceResponse.StatusCode);

            // =================================================
            // 9. PAST APPOINTMENT → 400
            // =================================================

            var pastAppointmentResponse =
                await _client.PostAsJsonAsync(
                    "/api/appointments",
                    new CreateAppointmentDto
                    {
                        PatientId = patientOne.Id,
                        DentistId = activeDentist.Id,
                        DentalServiceId = activeService.Id,
                        StartTime = DateTime.UtcNow.AddDays(-1)
                    });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                pastAppointmentResponse.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;

            // -------------------------------------------------
            // Clean appointments first because they reference
            // patient, dentist and service foreign keys.
            // -------------------------------------------------

            var testAppointments =
                await db.Appointments
                    .Where(a =>
                        a.PatientId == patientOne.Id ||
                        a.PatientId == patientTwo.Id ||
                        a.DentistId == activeDentist.Id ||
                        a.DentistId == secondDentist.Id ||
                        a.DentistId == inactiveDentist.Id)
                    .ToListAsync();

            db.Appointments.RemoveRange(
                testAppointments);

            db.Patients.RemoveRange(
                patientOne,
                patientTwo);

            db.Dentists.RemoveRange(
                activeDentist,
                secondDentist,
                inactiveDentist);

            db.DentalServices.RemoveRange(
                activeService,
                inactiveService);

            var testAdmin =
                await db.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == adminEmail);

            if (testAdmin != null)
            {
                db.Users.Remove(testAdmin);
            }

            await db.SaveChangesAsync();
        }
    }
}