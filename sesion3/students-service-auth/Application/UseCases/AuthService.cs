using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Students.Application.DTOs;
using Students.Domain.Entities;
using Students.Domain.Interfaces;

namespace Students.Application.UseCases;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration  _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration  = configuration;
    }

    // ──────────────────────────────────────────────────────────
    // Registro
    // ──────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Verificar que el role sea válido
        var rolesValidos = new[] { "Admin", "Teacher", "ReadOnly" };
        if (!rolesValidos.Contains(request.Role))
            throw new ArgumentException($"Rol '{request.Role}' no válido. Use: Admin, Teacher o ReadOnly.");

        // Verificar que no exista ya el usuario
        if (await _userRepository.ExistsAsync(request.Username, request.Email))
            throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario o email.");

        // Hashear la contraseña
        var passwordHash = HashPassword(request.Password);

        // Crear el usuario en la capa de dominio
        var user = User.Create(
            username:     request.Username,
            email:        request.Email,
            passwordHash: passwordHash,
            role:         request.Role);

        await _userRepository.CreateAsync(user);

        return GenerarRespuesta(user);
    }

    // ──────────────────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Credenciales incorrectas.");

        if (!VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales incorrectas.");

        return GenerarRespuesta(user);
    }

    // ──────────────────────────────────────────────────────────
    // Generación del token JWT
    // ──────────────────────────────────────────────────────────

    private AuthResponse GenerarRespuesta(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey   = jwtSettings["SecretKey"]  ?? throw new InvalidOperationException("JWT SecretKey no configurada.");
        var issuer      = jwtSettings["Issuer"]     ?? "students-service";
        var audience    = jwtSettings["Audience"]   ?? "students-client";
        var expMinutes  = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name,               user.Username),
            new Claim(ClaimTypes.Role,               user.Role),
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(expMinutes);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return new AuthResponse(
            Token:     new JwtSecurityTokenHandler().WriteToken(token),
            Username:  user.Username,
            Email:     user.Email,
            Role:      user.Role,
            ExpiresAt: expiresAt);
    }

    // ──────────────────────────────────────────────────────────
    // Hashing de contraseñas — PBKDF2 con salt aleatorio
    // ──────────────────────────────────────────────────────────

    private static string HashPassword(string password)
    {
        var salt   = RandomNumberGenerator.GetBytes(16);
        var hash   = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations:  100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength:  32);

        // Guardar salt + hash juntos en Base64
        var combined = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, combined, 0,           salt.Length);
        Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var salt     = combined[..16];
        var stored   = combined[16..];

        var computed = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations:    100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength:  32);

        return CryptographicOperations.FixedTimeEquals(computed, stored);
    }
}
