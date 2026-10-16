using ProjectFlow.Infrastructure.Services;

namespace ProjectFlow.Web.Services;

public sealed class DeadlineWorker(IServiceScopeFactory scopes, ILogger<DeadlineWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<DeadlineService>().SweepAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception error) { logger.LogWarning("Deadline sweep failed ({Type}); will retry next interval.", error.GetType().Name); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
