using ChatInbox.Api.Conversations;
using ChatInbox.Api.Hubs;
using ChatInbox.Api.Messaging;
using ChatInbox.Api.Webhooks.Telegram;
using ChatInbox.Infrastructure;
using ChatInbox.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TelegramWebhookSecretValidator>();
builder.Services.AddSingleton<TelegramUpdateMapper>();
builder.Services.AddHostedService<ConversationUpdatedConsumer>();
builder.Services.AddTelegramWebhookRegistration(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var app = builder.Build();

app.MapTelegramWebhook();
app.MapConversationEndpoints();
app.MapHub<InboxHub>(InboxHubContract.Path);
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();

public partial class Program;
