using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PokedexApi.Data;
using PokedexApi.Helpers;
using PokedexApi.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto> Register(RegisterDto registerDto)
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new Exception("Email already registered");
            }

            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                throw new Exception("Username already taken");
            }

            // Create user
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                EmailVerificationToken = GenerateToken(),
                EmailVerificationTokenExpires = DateTime.UtcNow.AddDays(1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send verification email
            await _emailService.SendVerificationEmail(user.Email, user.EmailVerificationToken);

            // Generate tokens
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = await GetUserProfile(user.Id)
            };
        }

        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.UsernameOrEmail ||
                                         u.Username == loginDto.UsernameOrEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new Exception("Invalid credentials");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = await GetUserProfile(user.Id)
            };
        }

        public async Task<User> GetUserById(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            return user;
        }

        public async Task<UserProfileDto> GetUserProfile(int userId)
        {
            var user = await GetUserById(userId);
            var teamCount = await _context.Teams.CountAsync(t => t.UserId == userId);

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                CreatedAt = user.CreatedAt,
                TotalTeams = teamCount
            };
        }

        public async Task<AuthResponseDto> RefreshToken(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpires < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired refresh token");
            }

            var token = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = newRefreshToken,
                User = await GetUserProfile(user.Id)
            };
        }

        public async Task<bool> VerifyEmail(string token)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null || user.EmailVerificationTokenExpires < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired verification token");
            }

            user.EmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ResendVerificationEmail(int userId)
        {
            var user = await GetUserById(userId);

            if (user.EmailVerified)
            {
                throw new Exception("Email already verified");
            }

            user.EmailVerificationToken = GenerateToken();
            user.EmailVerificationTokenExpires = DateTime.UtcNow.AddDays(1);
            await _context.SaveChangesAsync();

            await _emailService.SendVerificationEmail(user.Email, user.EmailVerificationToken);
            return true;
        }

        public async Task<bool> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Don't reveal if user exists
                return true;
            }

            user.PasswordResetToken = GenerateToken();
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmail(user.Email, user.PasswordResetToken);
            return true;
        }

        public async Task<bool> ResetPassword(string token, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (user == null || user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired reset token");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = await GetUserById(userId);

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                throw new Exception("Current password is incorrect");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserProfileDto> UpdateProfile(int userId, UpdateProfileDto updateDto)
        {
            var user = await GetUserById(userId);

            if (!string.IsNullOrEmpty(updateDto.Username))
            {
                if (await _context.Users.AnyAsync(u => u.Username == updateDto.Username && u.Id != userId))
                {
                    throw new Exception("Username already taken");
                }
                user.Username = updateDto.Username;
            }

            if (!string.IsNullOrEmpty(updateDto.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.Id != userId))
                {
                    throw new Exception("Email already registered");
                }
                user.Email = updateDto.Email;
                user.EmailVerified = false; // Require re-verification
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetUserProfile(userId);
        }

        public async Task<bool> DeleteAccount(int userId)
        {
            var user = await GetUserById(userId);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new Exception("JWT Issuer not configured");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new Exception("JWT Audience not configured");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

    }
}