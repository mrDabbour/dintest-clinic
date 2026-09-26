using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using dentist_clinic_api.Data;
using dentist_clinic_api.DTOs.Auth;
using dentist_clinic_api.Models.Auth;

namespace dentist_clinic_api.Tests;

public class AuthenticationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }


    // ==========================================
    // No JWT
    // Expected: 401 Unauthorized
    // ==========================================

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response =
            await _client.GetAsync("/api/patients");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ==========================================
    // Wrong password
    // Expected: 401 Unauthorized
    // ==========================================

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var loginRequest = new
        {
            email = "admin@dentistclinic.test",
            password = "DefinitelyWrongPassword!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ==========================================
    // Correct login
    // Expected:
    // Login → 200
    // JWT returned
    // JWT accesses protected endpoint → 200
    // ==========================================

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwt_AndAccessesProtectedEndpoint()
    {
        const string email =
            "integration.admin@dentistclinic.test";

        const string password =
            "IntegrationTest123!";

        using var scope =
            _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<DentistDbContext>();


        // Clean old test user if previous run was interrupted
        var existingUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)
        {
            dbContext.Users.Remove(existingUser);
            await dbContext.SaveChangesAsync();
        }


        // Create controlled test Admin
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
            // Login through real API
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


            // Read JWT returned by API
            var authResponse =
                await loginResponse.Content
                    .ReadFromJsonAsync<AuthResponseDto>();

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse.Token));


            // Send JWT to protected endpoint
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    authResponse.Token);

            var protectedResponse =
                await _client.GetAsync(
                    "/api/patients");

            Assert.Equal(
                HttpStatusCode.OK,
                protectedResponse.StatusCode);
        }
        finally
        {
            // Delete test user
            var testUser =
                await dbContext.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == email);

            if (testUser != null)
            {
                dbContext.Users.Remove(testUser);
                await dbContext.SaveChangesAsync();
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }
    }
}