using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymAPI.Data;
using GymAPI.DTOs;
using GymAPI.Models;

namespace GymAPI.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
}

public class AuthService : IAuthService
{
    private readonly GymDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(GymDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return new AuthResponse(GenerateToken(user), user.Name, user.Email, user.Role);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var plan = await _db.Plans.FindAsync(request.PlanId);
        if (plan == null) return null;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Member"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var member = new Member
        {
            UserId = user.Id,
            Phone = request.Phone,
            BirthDate = DateTime.SpecifyKind(
                request.BirthDate,
                DateTimeKind.Utc
            ),
            PlanId = request.PlanId,
            PlanStartDate = DateTime.UtcNow,
            PlanEndDate = DateTime.UtcNow.AddMonths(plan.DurationMonths)
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        return new AuthResponse(GenerateToken(user), user.Name, user.Email, user.Role);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpiryHours"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
