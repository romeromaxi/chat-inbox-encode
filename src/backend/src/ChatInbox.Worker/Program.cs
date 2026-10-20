using ChatInbox.Application.Messages;
using ChatInbox.Infrastructure;
using ChatInbox.Worker.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IIncomingTextMessageHandler, IncomingTextMessageHandler>();
builder.Services.AddScoped<IProcessOutgoingMessageHandler, ProcessOutgoingMessageHandler>();
builder.Services.AddTelegramMessaging(builder.Configuration);
builder.Services.AddHostedService<IncomingTextMessageConsumer>();
builder.Services.AddHostedService<OutgoingTextMessageConsumer>();

using var host = builder.Build();

await host.RunAsync();
