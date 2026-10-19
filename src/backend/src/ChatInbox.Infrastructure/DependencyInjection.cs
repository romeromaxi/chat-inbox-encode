using ChatInbox.Application.Abstractions;
using ChatInbox.Application.Conversations;
using ChatInbox.Infrastructure.Messaging;
using ChatInbox.Infrastructure.Persistence;
using ChatInbox.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatInbox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is required");

        services.AddDbContextFactory<ChatInboxDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<IConversationReader, ConversationReader>();
        services.AddScoped<IMarkConversationReadHandler, MarkConversationReadHandler>();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RabbitMq:Host is required")
            .Validate(options => options.Port is > 0 and <= 65535, "RabbitMq:Port is invalid")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "RabbitMq:Username is required")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RabbitMq:Password is required")
            .ValidateOnStart();

        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        return services;
    }

    public static IServiceCollection AddTelegramWebhookRegistration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BotToken), "Telegram:BotToken is required")
            .Validate(
                options => options.WebhookSecretToken is { Length: > 0 and <= 256 },
                "Telegram:WebhookSecretToken is invalid")
            .Validate(
                options => Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps,
                "Telegram:WebhookUrl must be an absolute HTTPS URL")
            .Validate(options => options.PrivateChatsOnly, "Only private Telegram chats are supported")
            .ValidateOnStart();

        services.AddHttpClient<ITelegramWebhookRegistrar, TelegramWebhookRegistrar>(client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).RemoveAllLoggers();
        services.AddHostedService<TelegramWebhookRegistrationHostedService>();

        return services;
    }
}
