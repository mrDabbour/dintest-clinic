using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using dentist_clinic_api.Data;
using dentist_clinic_api.DTOs.Users;
using dentist_clinic_api.Models.Auth;

namespace dentist_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly DentistDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    private static readonly string[] AllowedRoles =
    {
        "Admin",
        "Receptionist",
        "Dentist"
    };

    public UsersController(DentistDbContext context)
    {
        _context = context;
    }


    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return Ok(users);
    }


    // GET: api/users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(user);
    }


    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(
        CreateUserDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
        }

        var role = NormalizeRole(dto.Role);

        if (role == null)
        {
            return BadRequest(new
            {
                message = "Role must be Admin, Receptionist or Dentist."
            });
        }

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = MapToResponse(user);

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            response);
    }


    // PUT: api/users/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(
        int id,
        UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var email = dto.Email.Trim().ToLowerInvariant();

        var duplicateEmail = await _context.Users
            .AnyAsync(u =>
                u.Email == email &&
                u.Id != id);

        if (duplicateEmail)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
        }

        var role = NormalizeRole(dto.Role);

        if (role == null)
        {
            return BadRequest(new
            {
                message = "Role must be Admin, Receptionist or Dentist."
            });
        }

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = email;
        user.Role = role;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToResponse(user));
    }


    private static string? NormalizeRole(string role)
    {
        return AllowedRoles.FirstOrDefault(
            allowedRole =>
                allowedRole.Equals(
                    role.Trim(),
                    StringComparison.OrdinalIgnoreCase));
    }


    private static UserResponseDto MapToResponse(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}