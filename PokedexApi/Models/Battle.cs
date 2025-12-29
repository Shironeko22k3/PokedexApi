using PokedexApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokeDexApi.Models
{
    /// <summary>
    /// Battle history record
    /// </summary>
    public class BattleHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Player1Id { get; set; }

        [ForeignKey("Player1Id")]
        public virtual User Player1 { get; set; }

        [Required]
        public int Player2Id { get; set; }

        [ForeignKey("Player2Id")]
        public virtual User Player2 { get; set; }

        public int? WinnerId { get; set; }

        [ForeignKey("WinnerId")]
        public virtual User Winner { get; set; }

        [Required]
        public int Player1TeamId { get; set; }

        [ForeignKey("Player1TeamId")]
        public virtual Team Player1Team { get; set; }

        [Required]
        public int Player2TeamId { get; set; }

        [ForeignKey("Player2TeamId")]
        public virtual Team Player2Team { get; set; }

        [Required]
        public int TotalTurns { get; set; }

        [Column(TypeName = "text")]
        public string BattleLog { get; set; }

        public BattleResult Result { get; set; }

        [Required]
        public DateTime StartedAt { get; set; }

        [Required]
        public DateTime EndedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsReported { get; set; }

        public string ReportReason { get; set; }

        [NotMapped]
        public TimeSpan Duration => EndedAt - StartedAt;
    }

    public enum BattleResult
    {
        Player1Win,
        Player2Win,
        Draw,
        Forfeit
    }

    /// <summary>
    /// Battle statistics for a user
    /// </summary>
    public class BattleStats
    {
        public int UserId { get; set; }
        public int TotalBattles { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestWinStreak { get; set; }
        public int TotalTurnsPlayed { get; set; }
        public double AverageTurnsPerBattle { get; set; }
        public string FavoritePokemon { get; set; }
        public int Rating { get; set; }
        public string Rank { get; set; }
    }

    /// <summary>
    /// Leaderboard entry
    /// </summary>
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Rating { get; set; }
        public int TotalBattles { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
    }

    /// <summary>
    /// Active battle session (for real-time battles)
    /// </summary>
    public class BattleSession
    {
        [Key]
        public string BattleId { get; set; }

        [Required]
        public int Player1Id { get; set; }

        [Required]
        public string Player1SocketId { get; set; }

        public int? Player2Id { get; set; }

        public string Player2SocketId { get; set; }

        [Required]
        public int Player1TeamId { get; set; }

        public int? Player2TeamId { get; set; }

        [Column(TypeName = "text")]
        public string BattleState { get; set; }

        public BattleSessionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public enum BattleSessionStatus
    {
        Waiting,
        Active,
        Finished,
        Cancelled
    }

    /// <summary>
    /// Matchmaking queue entry
    /// </summary>
    public class MatchmakingQueue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; }

        [Required]
        public string SocketId { get; set; }

        public int Rating { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// User rating/ranking system
    /// </summary>
    public class UserRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int Rating { get; set; } = 1000; // Starting ELO rating

        public int Peak { get; set; } = 1000;

        public int TotalBattles { get; set; }

        public int Wins { get; set; }

        public int Losses { get; set; }

        public int Draws { get; set; }

        public int CurrentStreak { get; set; }

        public int LongestWinStreak { get; set; }

        public DateTime LastBattleAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public double WinRate => TotalBattles > 0 ? (double)Wins / TotalBattles * 100 : 0;

        [NotMapped]
        public string Rank
        {
            get
            {
                if (Rating >= 2400) return "Master";
                if (Rating >= 2200) return "Diamond";
                if (Rating >= 2000) return "Platinum";
                if (Rating >= 1800) return "Gold";
                if (Rating >= 1600) return "Silver";
                if (Rating >= 1400) return "Bronze";
                return "Beginner";
            }
        }
    }

    /// <summary>
    /// Battle report (for moderation)
    /// </summary>
    public class BattleReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BattleId { get; set; }

        [ForeignKey("BattleId")]
        public virtual BattleHistory Battle { get; set; }

        [Required]
        public int ReportedById { get; set; }

        [ForeignKey("ReportedById")]
        public virtual User ReportedBy { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        public ReportStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedById { get; set; }

        public string ReviewNotes { get; set; }
    }

    public enum ReportStatus
    {
        Pending,
        Reviewing,
        Resolved,
        Rejected
    }
}