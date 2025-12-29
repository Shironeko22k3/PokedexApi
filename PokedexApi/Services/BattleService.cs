using Microsoft.EntityFrameworkCore;
using PokedexApi.Data;
using PokedexApi.Services;
using PokeDexApi.Data;
using PokeDexApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokeDexApi.Services
{
    public interface IBattleService
    {
        Task<List<BattleHistory>> GetUserBattlesAsync(int userId);
        Task<BattleHistory> GetBattleByIdAsync(int id);
        Task<BattleStats> GetUserStatsAsync(int userId);
        Task<List<LeaderboardEntry>> GetLeaderboardAsync(int limit);
        Task<BattleHistory> SaveBattleAsync(SaveBattleRequest request);
        Task DeleteBattleAsync(int id);
        Task ReportBattleAsync(int battleId, int reportedById, string reason);
        Task<UserRating> GetOrCreateUserRatingAsync(int userId);
        Task UpdateUserRatingAsync(int userId, bool won, int opponentRating);
        Task<BattleSession> CreateBattleSessionAsync(int player1Id, string player1SocketId, int teamId);
        Task<BattleSession> GetBattleSessionAsync(string battleId);
        Task UpdateBattleSessionAsync(BattleSession session);
        Task DeleteBattleSessionAsync(string battleId);
        Task<List<BattleSession>> GetExpiredSessionsAsync(int minutesOld);
    }

    public class BattleService : IBattleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITeamService _teamService;

        public BattleService(ApplicationDbContext context, ITeamService teamService)
        {
            _context = context;
            _teamService = teamService;
        }

        public async Task<List<BattleHistory>> GetUserBattlesAsync(int userId)
        {
            return await _context.BattleHistories
                .Include(b => b.Player1)
                .Include(b => b.Player2)
                .Include(b => b.Winner)
                .Include(b => b.Player1Team)
                .Include(b => b.Player2Team)
                .Where(b => b.Player1Id == userId || b.Player2Id == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<BattleHistory> GetBattleByIdAsync(int id)
        {
            return await _context.BattleHistories
                .Include(b => b.Player1)
                .Include(b => b.Player2)
                .Include(b => b.Winner)
                .Include(b => b.Player1Team)
                .Include(b => b.Player2Team)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BattleStats> GetUserStatsAsync(int userId)
        {
            var rating = await GetOrCreateUserRatingAsync(userId);
            var battles = await _context.BattleHistories
                .Where(b => b.Player1Id == userId || b.Player2Id == userId)
                .ToListAsync();

            var totalTurns = battles.Sum(b => b.TotalTurns);
            var avgTurns = battles.Count > 0 ? (double)totalTurns / battles.Count : 0;

            // Get most used Pokemon (simplified - would need more complex logic)
            var favoritePokemon = "Pikachu"; // TODO: Implement favorite Pokemon logic

            return new BattleStats
            {
                UserId = userId,
                TotalBattles = rating.TotalBattles,
                Wins = rating.Wins,
                Losses = rating.Losses,
                Draws = rating.Draws,
                WinRate = rating.WinRate,
                CurrentStreak = rating.CurrentStreak,
                LongestWinStreak = rating.LongestWinStreak,
                TotalTurnsPlayed = totalTurns,
                AverageTurnsPerBattle = avgTurns,
                FavoritePokemon = favoritePokemon,
                Rating = rating.Rating,
                Rank = rating.Rank
            };
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int limit)
        {
            var topPlayers = await _context.UserRatings
                .Include(r => r.User)
                .OrderByDescending(r => r.Rating)
                .Take(limit)
                .ToListAsync();

            var leaderboard = topPlayers.Select((r, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                UserId = r.UserId,
                Username = r.User.Username,
                Rating = r.Rating,
                TotalBattles = r.TotalBattles,
                Wins = r.Wins,
                Losses = r.Losses,
                WinRate = r.WinRate
            }).ToList();

            return leaderboard;
        }

        public async Task<BattleHistory> SaveBattleAsync(SaveBattleRequest request)
        {
            // Determine result
            BattleResult result;
            if (request.WinnerId == 0)
            {
                result = BattleResult.Draw;
            }
            else if (request.WinnerId == request.Player1Id)
            {
                result = BattleResult.Player1Win;
            }
            else
            {
                result = BattleResult.Player2Win;
            }

            var battle = new BattleHistory
            {
                Player1Id = request.Player1Id,
                Player2Id = request.Player2Id,
                WinnerId = request.WinnerId > 0 ? request.WinnerId : (int?)null,
                Player1TeamId = request.Player1TeamId,
                Player2TeamId = request.Player2TeamId,
                TotalTurns = request.TotalTurns,
                BattleLog = request.BattleLog,
                Result = result,
                StartedAt = request.StartedAt,
                EndedAt = request.EndedAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.BattleHistories.Add(battle);
            await _context.SaveChangesAsync();

            // Update user ratings
            var player1Rating = await GetOrCreateUserRatingAsync(request.Player1Id);
            var player2Rating = await GetOrCreateUserRatingAsync(request.Player2Id);

            if (result != BattleResult.Draw)
            {
                await UpdateUserRatingAsync(
                    request.Player1Id,
                    result == BattleResult.Player1Win,
                    player2Rating.Rating
                );

                await UpdateUserRatingAsync(
                    request.Player2Id,
                    result == BattleResult.Player2Win,
                    player1Rating.Rating
                );
            }
            else
            {
                // Handle draw
                player1Rating.TotalBattles++;
                player1Rating.Draws++;
                player1Rating.CurrentStreak = 0;
                player1Rating.LastBattleAt = DateTime.UtcNow;

                player2Rating.TotalBattles++;
                player2Rating.Draws++;
                player2Rating.CurrentStreak = 0;
                player2Rating.LastBattleAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            return battle;
        }

        public async Task DeleteBattleAsync(int id)
        {
            var battle = await _context.BattleHistories.FindAsync(id);
            if (battle != null)
            {
                _context.BattleHistories.Remove(battle);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReportBattleAsync(int battleId, int reportedById, string reason)
        {
            var battle = await _context.BattleHistories.FindAsync(battleId);
            if (battle == null)
            {
                throw new Exception("Battle not found");
            }

            battle.IsReported = true;
            battle.ReportReason = reason;

            var report = new BattleReport
            {
                BattleId = battleId,
                ReportedById = reportedById,
                Reason = reason,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.BattleReports.Add(report);
            await _context.SaveChangesAsync();
        }

        public async Task<UserRating> GetOrCreateUserRatingAsync(int userId)
        {
            var rating = await _context.UserRatings
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (rating == null)
            {
                rating = new UserRating
                {
                    UserId = userId,
                    Rating = 1000,
                    Peak = 1000,
                    TotalBattles = 0,
                    Wins = 0,
                    Losses = 0,
                    Draws = 0,
                    CurrentStreak = 0,
                    LongestWinStreak = 0,
                    LastBattleAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserRatings.Add(rating);
                await _context.SaveChangesAsync();
            }

            return rating;
        }

        public async Task UpdateUserRatingAsync(int userId, bool won, int opponentRating)
        {
            var rating = await GetOrCreateUserRatingAsync(userId);

            // ELO rating calculation
            const int K = 32; // K-factor
            double expectedScore = 1.0 / (1.0 + Math.Pow(10, (opponentRating - rating.Rating) / 400.0));
            double actualScore = won ? 1.0 : 0.0;
            int ratingChange = (int)Math.Round(K * (actualScore - expectedScore));

            rating.Rating += ratingChange;
            rating.TotalBattles++;

            if (won)
            {
                rating.Wins++;
                rating.CurrentStreak = rating.CurrentStreak > 0 ? rating.CurrentStreak + 1 : 1;

                if (rating.CurrentStreak > rating.LongestWinStreak)
                {
                    rating.LongestWinStreak = rating.CurrentStreak;
                }
            }
            else
            {
                rating.Losses++;
                rating.CurrentStreak = rating.CurrentStreak < 0 ? rating.CurrentStreak - 1 : -1;
            }

            if (rating.Rating > rating.Peak)
            {
                rating.Peak = rating.Rating;
            }

            rating.LastBattleAt = DateTime.UtcNow;
            rating.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Battle Session Management
        public async Task<BattleSession> CreateBattleSessionAsync(int player1Id, string player1SocketId, int teamId)
        {
            var battleId = GenerateBattleId();

            var session = new BattleSession
            {
                BattleId = battleId,
                Player1Id = player1Id,
                Player1SocketId = player1SocketId,
                Player1TeamId = teamId,
                Status = BattleSessionStatus.Waiting,
                CreatedAt = DateTime.UtcNow
            };

            _context.BattleSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<BattleSession> GetBattleSessionAsync(string battleId)
        {
            return await _context.BattleSessions
                .FirstOrDefaultAsync(s => s.BattleId == battleId);
        }

        public async Task UpdateBattleSessionAsync(BattleSession session)
        {
            session.UpdatedAt = DateTime.UtcNow;
            _context.BattleSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBattleSessionAsync(string battleId)
        {
            var session = await _context.BattleSessions
                .FirstOrDefaultAsync(s => s.BattleId == battleId);

            if (session != null)
            {
                _context.BattleSessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<BattleSession>> GetExpiredSessionsAsync(int minutesOld)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutesOld);

            return await _context.BattleSessions
                .Where(s => s.CreatedAt < cutoffTime && s.Status == BattleSessionStatus.Waiting)
                .ToListAsync();
        }

        private string GenerateBattleId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class SaveBattleRequest
    {
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int WinnerId { get; set; }
        public int Player1TeamId { get; set; }
        public int Player2TeamId { get; set; }
        public int TotalTurns { get; set; }
        public string BattleLog { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
    }
}