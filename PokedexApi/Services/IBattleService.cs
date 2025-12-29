using PokedexApi.Models;
using PokeDexApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public interface IBattleService
    {
        // Battle History
        Task<List<BattleHistory>> GetUserBattlesAsync(int userId);
        Task<BattleHistory> GetBattleByIdAsync(int battleId);

        // Battle Stats
        Task<BattleStats> GetUserStatsAsync(int userId);
        Task<List<LeaderboardEntry>> GetLeaderboardAsync(int limit);

        // Battle Management
        Task<BattleHistory> SaveBattleAsync(SaveBattleRequest request);
        Task<bool> DeleteBattleAsync(int battleId);
        Task<bool> ReportBattleAsync(int battleId, int reportedById, string reason);

        // User Rating (ELO)
        Task<UserRating> GetOrCreateUserRatingAsync(int userId);
        Task UpdateUserRatingAsync(int userId, bool won, int opponentRating);

        // Battle Sessions
        Task<BattleSession> CreateBattleSessionAsync(int player1Id, string player1SocketId, int player2Id, string player2SocketId, int player1TeamId, int player2TeamId);
        Task<BattleSession> GetBattleSessionAsync(string battleId);
        Task UpdateBattleSessionAsync(BattleSession session);
        Task DeleteBattleSessionAsync(string battleId);
        Task<List<BattleSession>> GetExpiredSessionsAsync(int expiryMinutes);
    }

    // Request DTOs
    public class SaveBattleRequest
    {
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int? WinnerId { get; set; }
        public int Player1TeamId { get; set; }
        public int Player2TeamId { get; set; }
        public int TotalTurns { get; set; }
        public string BattleLog { get; set; } = string.Empty;
        public BattleResult Result { get; set; }
    }
}