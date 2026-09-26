using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using dentist_clinic_api.Data;
using dentist_clinic_api.DTOs.Auth;
using dentist_clinic_api.Models;
using dentist_clinic_api.Models.Auth;

namespace dentist_clinic_api.Tests;

public class AppointmentStatusWorkflowTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AppointmentStatusWorkflowTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AppointmentStatusWorkflow_WorksCorrectly()
    {
        const string email =
            "workflow.admin@dentistclinic.test";

        const string password =
            "IntegrationTest123!";

        using var scope =
            _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<DentistDbContext>();

        // =====================================================
        // CLEAN LEFTOVERS
        // =====================================================

        var oldAppointments = await db.Appointments
            .Where(a =>
                a.Patient.Email.StartsWith("workflow.test.") ||
                a.Dentist.Email.StartsWith("workflow.test."))
            .ToListAsync();

        db.Appointments.RemoveRange(oldAppointments);

        var oldPatients = await db.Patients
            .Where(p =>
                p.Email.StartsWith("workflow.test."))
            .ToListAsync();

        db.Patients.RemoveRange(oldPatients);

        var oldDentists = await db.Dentists
            .Where(d =>
                d.Email.StartsWith("workflow.test."))
            .ToListAsync();

        db.Dentists.RemoveRange(oldDentists);

        var oldServices = await db.DentalServices
            .Where(s =>
                s.Name.StartsWith("Workflow Test"))
            .ToListAsync();

        db.DentalServices.RemoveRange(oldServices);

        var oldUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (oldUser != null)
        {
            db.Users.Remove(oldUser);
        }

        await db.SaveChangesAsync();

        // =====================================================
        // CREATE TEMPORARY ADMIN
        // =====================================================

        var admin = new User
        {
            FirstName = "Workflow",
            LastName = "Admin",
            Email = email,
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

        // =====================================================
        // CREATE TEMPORARY PATIENT
        // =====================================================

        var patient = new Patient
        {
            FirstName = "Workflow",
            LastName = "Patient",
            Email = "workflow.test.patient@example.test",
            Phone = "0200000100"
        };

        db.Patients.Add(patient);

        // =====================================================
        // CREATE TEMPORARY DENTIST
        // =====================================================

        var dentist = new Dentist
        {
            FirstName = "Workflow",
            LastName = "Dentist",
            Email = "workflow.test.dentist@example.test",
            Phone = "0200000101",
            RegistrationNumber = "WORKFLOW-DENTIST-001",
            Specialty = "General Dentistry",
            Biography = "Workflow integration test dentist.",
            IsActive = true
        };

        db.Dentists.Add(dentist);

        // =====================================================
        // CREATE TEMPORARY SERVICE
        // =====================================================

        var service = new DentalService
        {
            Name = "Workflow Test Service",
            Category = "Testing",
            Description = "Workflow integration test service.",
            Price = 100,
            DurationMinutes = 30,
            IsActive = true
        };

        db.DentalServices.Add(service);

        await db.SaveChangesAsync();

        // =====================================================
        // CREATE SEPARATE APPOINTMENTS FOR EACH WORKFLOW
        // =====================================================

        var baseTime =
            DateTime.UtcNow
                .AddDays(40)
                .Date
                .AddHours(9);

        var confirmThenComplete = new Appointment
        {
            PatientId = patient.Id,
            DentistId = dentist.Id,
            DentalServiceId = service.Id,
            StartTime = baseTime,
            EndTime = baseTime.AddMinutes(30),
            Status = "Pending"
        };

        var confirmThenNoShow = new Appointment
        {
            PatientId = patient.Id,
            DentistId = dentist.Id,
            DentalServiceId = service.Id,
            StartTime = baseTime.AddHours(2),
            EndTime = baseTime.AddHours(2).AddMinutes(30),
            Status = "Pending"
        };

        var cancelAppointment = new Appointment
        {
            PatientId = patient.Id,
            DentistId = dentist.Id,
            DentalServiceId = service.Id,
            StartTime = baseTime.AddHours(4),
            EndTime = baseTime.AddHours(4).AddMinutes(30),
            Status = "Pending"
        };

        var invalidCompleteAppointment = new Appointment
        {
            PatientId = patient.Id,
            DentistId = dentist.Id,
            DentalServiceId = service.Id,
            StartTime = baseTime.AddHours(6),
            EndTime = baseTime.AddHours(6).AddMinutes(30),
            Status = "Pending"
        };

        db.Appointments.AddRange(
            confirmThenComplete,
            confirmThenNoShow,
            cancelAppointment,
            invalidCompleteAppointment);

        await db.SaveChangesAsync();

        try
        {
            // =================================================
            // LOGIN
            // =================================================

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        email,
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

            // =================================================
            // 1. PENDING → CONFIRMED
            // =================================================

            var confirmResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{confirmThenComplete.Id}/confirm",
                    null);

            Assert.Equal(
                HttpStatusCode.OK,
                confirmResponse.StatusCode);

            await db.Entry(confirmThenComplete).ReloadAsync();

            Assert.Equal(
                "Confirmed",
                confirmThenComplete.Status);

            // =================================================
            // 2. CONFIRMED → COMPLETED
            // =================================================

            var completeResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{confirmThenComplete.Id}/complete",
                    null);

            Assert.Equal(
                HttpStatusCode.OK,
                completeResponse.StatusCode);

            await db.Entry(confirmThenComplete).ReloadAsync();

            Assert.Equal(
                "Completed",
                confirmThenComplete.Status);

            // =================================================
            // 3. COMPLETED → CONFIRMED IS INVALID
            // =================================================

            var confirmCompletedResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{confirmThenComplete.Id}/confirm",
                    null);

            Assert.Equal(
                HttpStatusCode.Conflict,
                confirmCompletedResponse.StatusCode);

            // =================================================
            // 4. PENDING → CONFIRMED → NOSHOW
            // =================================================

            var secondConfirmResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{confirmThenNoShow.Id}/confirm",
                    null);

            Assert.Equal(
                HttpStatusCode.OK,
                secondConfirmResponse.StatusCode);

            var noShowResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{confirmThenNoShow.Id}/no-show",
                    null);

            Assert.Equal(
                HttpStatusCode.OK,
                noShowResponse.StatusCode);

            await db.Entry(confirmThenNoShow).ReloadAsync();

            Assert.Equal(
                "NoShow",
                confirmThenNoShow.Status);

            // =================================================
            // 5. PENDING → CANCELLED
            // =================================================

            var cancelResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{cancelAppointment.Id}/cancel",
                    null);

            Assert.Equal(
                HttpStatusCode.OK,
                cancelResponse.StatusCode);

            await db.Entry(cancelAppointment).ReloadAsync();

            Assert.Equal(
                "Cancelled",
                cancelAppointment.Status);

            // =================================================
            // 6. CANCELLED → CANCELLED AGAIN IS INVALID
            // =================================================

            var cancelAgainResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{cancelAppointment.Id}/cancel",
                    null);

            Assert.Equal(
                HttpStatusCode.Conflict,
                cancelAgainResponse.StatusCode);

            // =================================================
            // 7. PENDING → COMPLETED IS INVALID
            // =================================================

            var invalidCompleteResponse =
                await _client.PatchAsync(
                    $"/api/appointments/{invalidCompleteAppointment.Id}/complete",
                    null);

            Assert.Equal(
                HttpStatusCode.Conflict,
                invalidCompleteResponse.StatusCode);

            await db.Entry(invalidCompleteAppointment)
                .ReloadAsync();

            Assert.Equal(
                "Pending",
                invalidCompleteAppointment.Status);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;

            // =================================================
            // CLEANUP
            // Appointments first because of foreign keys.
            // =================================================

            var appointments = await db.Appointments
                .Where(a =>
                    a.PatientId == patient.Id ||
                    a.DentistId == dentist.Id)
                .ToListAsync();

            db.Appointments.RemoveRange(appointments);

            db.Patients.Remove(patient);
            db.Dentists.Remove(dentist);
            db.DentalServices.Remove(service);

            var testAdmin = await db.Users
                .FirstOrDefaultAsync(
                    u => u.Email == email);

            if (testAdmin != null)
            {
                db.Users.Remove(testAdmin);
            }

            await db.SaveChangesAsync();
        }
    }
}