using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PokedexApi.Models;
using PokedexApi.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PokedexApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(ITeamService teamService, ILogger<TeamsController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTeams()
        {
            try
            {
                var userId = GetUserId();
                var teams = await _teamService.GetAllTeams(userId);
                return Ok(teams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get teams failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeam(int id)
        {
            try
            {
                var userId = GetUserId();
                var team = await _teamService.GetTeamById(id, userId);
                return Ok(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get team failed");
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] TeamDto teamDto)
        {
            try
            {
                var userId = GetUserId();
                var team = await _teamService.CreateTeam(userId, teamDto);
                return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create team failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, [FromBody] TeamDto teamDto)
        {
            try
            {
                var userId = GetUserId();
                var team = await _teamService.UpdateTeam(id, userId, teamDto);
                return Ok(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update team failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            try
            {
                var userId = GetUserId();
                await _teamService.DeleteTeam(id, userId);
                return Ok(new { message = "Team deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete team failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetTeamCount()
        {
            try
            {
                var userId = GetUserId();
                var count = await _teamService.GetTeamCount(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get team count failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new Exception("Invalid user ID");
            }
            return userId;
        }
    }
}