FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/ChatInbox.Domain/ChatInbox.Domain.csproj src/ChatInbox.Domain/
COPY src/ChatInbox.Application/ChatInbox.Application.csproj src/ChatInbox.Application/
COPY src/ChatInbox.Contracts/ChatInbox.Contracts.csproj src/ChatInbox.Contracts/
COPY src/ChatInbox.Infrastructure/ChatInbox.Infrastructure.csproj src/ChatInbox.Infrastructure/
COPY src/ChatInbox.Worker/ChatInbox.Worker.csproj src/ChatInbox.Worker/
RUN dotnet restore src/ChatInbox.Worker/ChatInbox.Worker.csproj

COPY src/ src/
RUN dotnet publish src/ChatInbox.Worker/ChatInbox.Worker.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ChatInbox.Worker.dll"]
