using PokedexApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public interface ITeamService
    {
        Task<TeamResponseDto> CreateTeam(int userId, TeamDto teamDto);
        Task<TeamResponseDto> GetTeamById(int teamId, int userId);
        Task<List<TeamResponseDto>> GetAllTeams(int userId);
        Task<TeamResponseDto> UpdateTeam(int teamId, int userId, TeamDto teamDto);
        Task<bool> DeleteTeam(int teamId, int userId);
        Task<int> GetTeamCount(int userId);
    }
}