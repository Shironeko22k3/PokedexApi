using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokedexApi.Services;
using PokedexApi.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PokedexApi.Controllers
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

        [HttpGet("{id}")]
        public async Task<ActionResult<BattleHistory>> GetBattle(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var battle = await _battleService.GetBattleByIdAsync(id);

                if (battle == null) return NotFound(new { message = "Battle not found" });
                if (battle.Player1Id != userId && battle.Player2Id != userId) return Forbid();

                return Ok(battle);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

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

        [HttpPost("save")]
        public async Task<ActionResult<BattleHistory>> SaveBattle([FromBody] SaveBattleRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (request.Player1Id != userId && request.Player2Id != userId) return Forbid();

                var battle = await _battleService.SaveBattleAsync(request);
                return Ok(battle);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBattle(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var battle = await _battleService.GetBattleByIdAsync(id);

                if (battle == null) return NotFound(new { message = "Battle not found" });
                if (battle.Player1Id != userId && battle.Player2Id != userId) return Forbid();

                await _battleService.DeleteBattleAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

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

    public class ReportRequest
    {
        public string Reason { get; set; }
    }
}