using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NetflixClone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // CREATE USER
    // POST: api/Users
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PreferredGenres = request.PreferredGenres,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            ToResponse(user));
    }

    // ============================================================
    // GET USER
    // GET: api/Users/1
    // ============================================================

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(ToResponse(user));
    }

    // ============================================================
    // GET ALL USERS
    // GET: api/Users
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                PreferredGenres = u.PreferredGenres,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // ============================================================
    // UPDATE USER
    // PUT: api/Users/1
    // ============================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(
        int id,
        UpdateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var emailExists = await _context.Users
            .AnyAsync(u =>
                u.Email == request.Email &&
                u.Id != id);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "Another user already has this email."
            });
        }

        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.PreferredGenres = request.PreferredGenres;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PreferredGenres = user.PreferredGenres,
            CreatedAt = user.CreatedAt
        };
    }
}