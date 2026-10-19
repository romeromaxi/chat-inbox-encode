using ChatInbox.Application.Messages;
using ChatInbox.Infrastructure;
using ChatInbox.Worker.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IIncomingTextMessageHandler, IncomingTextMessageHandler>();
builder.Services.AddHostedService<IncomingTextMessageConsumer>();

using var host = builder.Build();

await host.RunAsync();
