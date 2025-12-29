using PokedexApi.Models;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> Register(RegisterDto registerDto);
        Task<AuthResponseDto> Login(LoginDto loginDto);
        Task<User> GetUserById(int userId);
        Task<UserProfileDto> GetUserProfile(int userId);
        Task<AuthResponseDto> RefreshToken(string refreshToken);
        Task<bool> VerifyEmail(string token);
        Task<bool> ResendVerificationEmail(int userId);
        Task<bool> ForgotPassword(string email);
        Task<bool> ResetPassword(string token, string newPassword);
        Task<bool> ChangePassword(int userId, string currentPassword, string newPassword);
        Task<UserProfileDto> UpdateProfile(int userId, UpdateProfileDto updateDto);
        Task<bool> DeleteAccount(int userId);
    }
}