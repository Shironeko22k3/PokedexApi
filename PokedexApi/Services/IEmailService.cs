using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmail(string toEmail, string token);
        Task SendPasswordResetEmail(string toEmail, string token);
    }
}