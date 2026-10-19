using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatInbox.Infrastructure.Telegram;

public sealed class TelegramWebhookRegistrationHostedService(
    ITelegramWebhookRegistrar registrar,
    ILogger<TelegramWebhookRegistrationHostedService> logger)
    : IHostedService
{
    private const int MaximumAttempts = 5;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                await registrar.RegisterAsync(cancellationToken);
                logger.LogInformation("Telegram webhook registered");
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "Telegram webhook registration attempt {Attempt} failed with {FailureType}",
                    attempt,
                    exception.GetType().Name);

                if (attempt == MaximumAttempts)
                {
                    break;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Telegram webhook registration failed during startup");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
