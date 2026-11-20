using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Auth;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<User?> GetUserByIdAsync(int id);
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
        string GenerateJwtToken(User user);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var normalizedUsername = dto.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            {
                throw new InvalidOperationException("Cet email est déjà utilisé par un autre compte.");
            }

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername.ToLower()))
            {
                throw new InvalidOperationException("Ce nom d'utilisateur est déjà pris.");
            }

            var user = new User
            {
                Username = normalizedUsername,
                Email = normalizedEmail,
                PasswordHash = HashPassword(dto.Password),
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "Client" : dto.Role.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Initialiser un compte de fidélité pour les clients avec un bonus de bienvenue
            int welcomePoints = 50;
            if (user.Role.Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                var loyalty = new LoyaltyAccount
                {
                    UserId = user.Id,
                    SavePointsBalance = welcomePoints
                };
                _context.LoyaltyAccounts.Add(loyalty);
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpiryHours()),
                LoyaltyPoints = welcomePoints
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var identifier = dto.EmailOrUsername.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.LoyaltyAccount)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == identifier || u.Username.ToLower() == identifier);

            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Identifiants de connexion invalides.");
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpiryHours()),
                LoyaltyPoints = user.LoyaltyAccount?.SavePointsBalance ?? 0
            };
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.LoyaltyAccount)
                .Include(u => u.Restaurants)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: salt,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split(':');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expectedHash = Convert.FromBase64String(parts[1]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password: Encoding.UTF8.GetBytes(password),
                    salt: salt,
                    iterations: 100000,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    outputLength: 32);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "SaveMyMealSuperSecretFallbackKey2026!#";
            var issuer = _configuration["Jwt:Issuer"] ?? "SaveMyMealBackend";
            var audience = _configuration["Jwt:Audience"] ?? "SaveMyMealApp";
            var expiryHours = GetTokenExpiryHours();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("role", user.Role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(expiryHours),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private double GetTokenExpiryHours()
        {
            if (double.TryParse(_configuration["Jwt:ExpiryInHours"], out var hours))
            {
                return hours;
            }
            return 24;
        }
    }
}
