using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokedexApi.Services;
using PokeDexApi.Models;
using PokeDexApi.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PokeDexApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BattleController : ControllerBase
    {
        private readonly IBattleService _battleService;
        private readonly ITeamService _teamService;

        public BattleController(IBattleService battleService, ITeamService teamService)
        {
            _battleService = battleService;
            _teamService = teamService;
        }

        /// <summary>
        /// Get all battles for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<BattleHistory>>> GetUserBattles()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var battles = await _battleService.GetUserBattlesAsync(userId);
                return Ok(battles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get battle by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BattleHistory>> GetBattle(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var battle = await _battleService.GetBattleByIdAsync(id);

                if (battle == null)
                {
                    return NotFound(new { message = "Battle not found" });
                }

                // Check if user is part of this battle
                if (battle.Player1Id != userId && battle.Player2Id != userId)
                {
                    return Forbid();
                }

                return Ok(battle);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get user battle statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<BattleStats>> GetBattleStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var stats = await _battleService.GetUserStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get leaderboard
        /// </summary>
        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<ActionResult<List<LeaderboardEntry>>> GetLeaderboard([FromQuery] int limit = 100)
        {
            try
            {
                var leaderboard = await _battleService.GetLeaderboardAsync(limit);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Save battle result (called by Socket.IO server)
        /// </summary>
        [HttpPost("save")]
        public async Task<ActionResult<BattleHistory>> SaveBattle([FromBody] SaveBattleRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Validate that the user is part of this battle
                if (request.Player1Id != userId && request.Player2Id != userId)
                {
                    return Forbid();
                }

                var battle = await _battleService.SaveBattleAsync(request);
                return Ok(battle);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete battle history
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBattle(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var battle = await _battleService.GetBattleByIdAsync(id);

                if (battle == null)
                {
                    return NotFound(new { message = "Battle not found" });
                }

                // Only allow users to delete their own battles
                if (battle.Player1Id != userId && battle.Player2Id != userId)
                {
                    return Forbid();
                }

                await _battleService.DeleteBattleAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Report battle (for cheating, etc.)
        /// </summary>
        [HttpPost("{id}/report")]
        public async Task<ActionResult> ReportBattle(int id, [FromBody] ReportRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _battleService.ReportBattleAsync(id, userId, request.Reason);
                return Ok(new { message = "Battle reported successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

    public class ReportRequest
    {
        public string Reason { get; set; }
    }
}