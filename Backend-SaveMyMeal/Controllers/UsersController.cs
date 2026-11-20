using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Users;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.LoyaltyAccount)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    LoyaltyPoints = u.LoyaltyAccount != null ? u.LoyaltyAccount.SavePointsBalance : 0
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.LoyaltyAccount)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = $"Utilisateur avec l'ID {id} introuvable." });
            }

            var dto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LoyaltyPoints = user.LoyaltyAccount?.SavePointsBalance ?? 0
            };

            return Ok(dto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .Include(u => u.LoyaltyAccount)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = $"Utilisateur avec l'ID {id} introuvable." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var usernameExists = await _context.Users.AnyAsync(u => u.Id != id && u.Username.ToLower() == dto.Username.ToLower());
                if (usernameExists)
                {
                    return BadRequest(new { message = "Ce nom d'utilisateur est déjà utilisé." });
                }
                user.Username = dto.Username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == dto.Email.ToLower());
                if (emailExists)
                {
                    return BadRequest(new { message = "Cette adresse email est déjà utilisée." });
                }
                user.Email = dto.Email.Trim().ToLowerInvariant();
            }

            await _context.SaveChangesAsync();

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LoyaltyPoints = user.LoyaltyAccount?.SavePointsBalance ?? 0
            });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"Utilisateur avec l'ID {id} introuvable." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
