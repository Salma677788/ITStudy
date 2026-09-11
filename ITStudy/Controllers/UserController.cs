using ITStudy.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using ITStudy.Models;
using ITStudy.DTOs;
using BCrypt.Net;

namespace ITStudy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    PreferredLanguage = u.PreferredLanguage,
                    PreferredTheme = u.PreferredTheme
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User account was not found." });
            }

            return Ok(new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                PreferredLanguage = user.PreferredLanguage,
                PreferredTheme = user.PreferredTheme
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} was not found." });
            }

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                PreferredLanguage = user.PreferredLanguage,
                PreferredTheme = user.PreferredTheme
            };

            return Ok(userDto);
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query cannot be empty." });
            }

            var searchQuery = query.Trim().ToLower();

            var users = await _context.Users
                .Where(u => u.Username.ToLower().Contains(searchQuery) ||
                            u.Email.ToLower().Contains(searchQuery))
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    PreferredLanguage = u.PreferredLanguage,
                    PreferredTheme = u.PreferredTheme
                })
                .ToListAsync();

            if (!users.Any())
            {
                return NotFound(new { message = $"No users found matching '{query}'." });
            }

            return Ok(users);
        }

        [HttpGet("suggestions")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetUserSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Ok(new List<string>());
            }

            var searchQuery = query.Trim().ToLower();

            var suggestions = await _context.Users
                .Where(u => u.Username.ToLower().Contains(searchQuery))
                .Select(u => u.Username)
                .Distinct()
                .Take(5)
                .ToListAsync();

            return Ok(suggestions);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(UserRegisterDto registerDto)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = registerDto.Role.ToString(),
                PreferredLanguage = "en",
                PreferredTheme = "light"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var resultDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                PreferredLanguage = user.PreferredLanguage,
                PreferredTheme = user.PreferredTheme
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, resultDto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return BadRequest(new { message = "Invalid email or password." });
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                user = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    PreferredLanguage = user.PreferredLanguage,
                    PreferredTheme = user.PreferredTheme
                }
            });
        }

        [HttpPatch("preferences")]
        [Authorize]
        public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user identity token." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            user.PreferredLanguage = dto.PreferredLanguage.ToLower();
            user.PreferredTheme = dto.PreferredTheme.ToLower();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User preferences updated successfully.",
                preferredLanguage = user.PreferredLanguage,
                preferredTheme = user.PreferredTheme
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} to delete was not found." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully." });
        }
    }
}