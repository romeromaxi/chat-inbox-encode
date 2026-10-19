FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/ChatInbox.Domain/ChatInbox.Domain.csproj src/ChatInbox.Domain/
COPY src/ChatInbox.Application/ChatInbox.Application.csproj src/ChatInbox.Application/
COPY src/ChatInbox.Contracts/ChatInbox.Contracts.csproj src/ChatInbox.Contracts/
COPY src/ChatInbox.Infrastructure/ChatInbox.Infrastructure.csproj src/ChatInbox.Infrastructure/
COPY src/ChatInbox.Api/ChatInbox.Api.csproj src/ChatInbox.Api/
RUN dotnet restore src/ChatInbox.Api/ChatInbox.Api.csproj

COPY src/ src/
RUN dotnet publish src/ChatInbox.Api/ChatInbox.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
COPY --from=build /app/publish .
HEALTHCHECK --interval=5s --timeout=5s --start-period=10s --retries=12 \
    CMD curl --fail --silent --show-error http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "ChatInbox.Api.dll"]
