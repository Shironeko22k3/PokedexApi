using Microsoft.EntityFrameworkCore;
using PokedexApi.Data;
using PokedexApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext _context;

        public TeamService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TeamResponseDto> CreateTeam(int userId, TeamDto teamDto)
        {
            var team = new Team
            {
                TeamName = teamDto.TeamName,
                TeamData = teamDto.TeamData,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            return MapToResponseDto(team);
        }

        public async Task<TeamResponseDto> GetTeamById(int teamId, int userId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.UserId == userId);

            if (team == null)
            {
                throw new Exception("Team not found");
            }

            return MapToResponseDto(team);
        }

        public async Task<List<TeamResponseDto>> GetAllTeams(int userId)
        {
            var teams = await _context.Teams
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();

            return teams.Select(MapToResponseDto).ToList();
        }

        public async Task<TeamResponseDto> UpdateTeam(int teamId, int userId, TeamDto teamDto)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.UserId == userId);

            if (team == null)
            {
                throw new Exception("Team not found");
            }

            team.TeamName = teamDto.TeamName;
            team.TeamData = teamDto.TeamData;
            team.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToResponseDto(team);
        }

        public async Task<bool> DeleteTeam(int teamId, int userId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.UserId == userId);

            if (team == null)
            {
                throw new Exception("Team not found");
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> GetTeamCount(int userId)
        {
            return await _context.Teams.CountAsync(t => t.UserId == userId);
        }

        private TeamResponseDto MapToResponseDto(Team team)
        {
            return new TeamResponseDto
            {
                Id = team.Id,
                TeamName = team.TeamName,
                TeamData = team.TeamData,
                CreatedAt = team.CreatedAt,
                UpdatedAt = team.UpdatedAt
            };
        }
    }
}