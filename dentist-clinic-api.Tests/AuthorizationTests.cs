using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using dentist_clinic_api.Data;
using dentist_clinic_api.DTOs.Auth;
using dentist_clinic_api.Models.Auth;

namespace dentist_clinic_api.Tests;

public class AuthorizationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =========================================================
    // TEST 1:
    // Receptionist can read services,
    // but cannot create a service.
    // =========================================================

    [Fact]
    public async Task Receptionist_CanReadServices_ButCannotCreateService()
    {
        const string email =
            "integration.receptionist@dentistclinic.test";

        const string password =
            "IntegrationTest123!";

        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<DentistDbContext>();

        // Remove leftover test user if a previous test
        // was interrupted.
        var existingUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)
        {
            dbContext.Users.Remove(existingUser);
            await dbContext.SaveChangesAsync();
        }

        // Create temporary Receptionist.
        var user = new User
        {
            FirstName = "Integration",
            LastName = "Receptionist",
            Email = email,
            Role = "Receptionist",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var passwordHasher =
            new PasswordHasher<User>();

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        try
        {
            // Login as Receptionist.
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

            var authResponse =
                await loginResponse.Content
                    .ReadFromJsonAsync<AuthResponseDto>();

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse.Token));

            // Attach Receptionist JWT.
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    authResponse.Token);

            // Receptionist CAN read dental services.
            var getResponse =
                await _client.GetAsync(
                    "/api/dentalservices");

            Assert.Equal(
                HttpStatusCode.OK,
                getResponse.StatusCode);

            // Receptionist CANNOT create dental services.
            var createResponse =
                await _client.PostAsJsonAsync(
                    "/api/dentalservices",
                    new
                    {
                        name = "RBAC Integration Test",
                        category = "Testing",
                        description =
                            "This should never be created.",
                        price = 100,
                        durationMinutes = 30
                    });

            Assert.Equal(
                HttpStatusCode.Forbidden,
                createResponse.StatusCode);
        }
        finally
        {
            // Remove JWT from HttpClient.
            _client.DefaultRequestHeaders.Authorization = null;

            // Delete temporary Receptionist.
            var testUser =
                await dbContext.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == email);

            if (testUser != null)
            {
                dbContext.Users.Remove(testUser);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    // =========================================================
    // TEST 2:
    // Admin can access an Admin-only endpoint.
    // =========================================================

    [Fact]
    public async Task Admin_CanAccessAdminOnlyUsersEndpoint()
    {
        const string email =
            "integration.admin.rbac@dentistclinic.test";

        const string password =
            "IntegrationTest123!";

        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<DentistDbContext>();

        // Remove leftover test user if a previous test
        // was interrupted.
        var existingUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)
        {
            dbContext.Users.Remove(existingUser);
            await dbContext.SaveChangesAsync();
        }

        // Create temporary Admin.
        var user = new User
        {
            FirstName = "Integration",
            LastName = "Admin",
            Email = email,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var passwordHasher =
            new PasswordHasher<User>();

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        try
        {
            // Login as Admin.
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

            var authResponse =
                await loginResponse.Content
                    .ReadFromJsonAsync<AuthResponseDto>();

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse.Token));

            // Attach Admin JWT.
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    authResponse.Token);

            // Admin CAN access Admin-only Users API.
            var usersResponse =
                await _client.GetAsync(
                    "/api/users");

            Assert.Equal(
                HttpStatusCode.OK,
                usersResponse.StatusCode);
        }
        finally
        {
            // Remove JWT from HttpClient.
            _client.DefaultRequestHeaders.Authorization = null;

            // Delete temporary Admin.
            var testUser =
                await dbContext.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == email);

            if (testUser != null)
            {
                dbContext.Users.Remove(testUser);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}