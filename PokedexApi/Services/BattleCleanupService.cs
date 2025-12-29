using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokeDexApi.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PokeDexApi.Services
{
    public class BattleCleanupService : BackgroundService
    {
        private readonly ILogger<BattleCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private readonly int _sessionExpiryMinutes = 30;

        public BattleCleanupService(
            ILogger<BattleCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Battle Cleanup Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during battle cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Battle Cleanup Service is stopping");
        }

        private async Task CleanupExpiredSessionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();

            try
            {
                var expiredSessions = await battleService.GetExpiredSessionsAsync(_sessionExpiryMinutes);

                foreach (var session in expiredSessions)
                {
                    _logger.LogInformation($"Cleaning up expired battle session: {session.BattleId}");
                    await battleService.DeleteBattleSessionAsync(session.BattleId);
                }

                if (expiredSessions.Count > 0)
                {
                    _logger.LogInformation($"Cleaned up {expiredSessions.Count} expired battle sessions");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up battle sessions");
            }
        }
    }
}