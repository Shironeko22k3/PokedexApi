using PokedexApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public interface IBattleService
    {
        Task<List<BattleHistory>> GetUserBattlesAsync(int userId);
        Task<BattleHistory> GetBattleByIdAsync(int battleId);
        Task<BattleStats> GetUserStatsAsync(int userId);
        Task<List<LeaderboardEntry>> GetLeaderboardAsync(int limit);
        Task<BattleHistory> SaveBattleAsync(SaveBattleRequest request);
        Task<bool> DeleteBattleAsync(int battleId);
        Task<bool> ReportBattleAsync(int battleId, int reportedById, string reason);
        Task<UserRating> GetOrCreateUserRatingAsync(int userId);
        Task UpdateUserRatingAsync(int userId, bool won, int opponentRating);
        Task<BattleSession> CreateBattleSessionAsync(int player1Id, string player1SocketId, int player2Id, string player2SocketId, int player1TeamId, int player2TeamId);
        Task<BattleSession> GetBattleSessionAsync(string battleId);
        Task UpdateBattleSessionAsync(BattleSession session);
        Task DeleteBattleSessionAsync(string battleId);
        Task<List<BattleSession>> GetExpiredSessionsAsync(int expiryMinutes);
    }
}