# Production image for the hosted API on Render (docs/deployment/deployment.md).
# Local development uses Dockerfile.dev through docker-compose.yml.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between source changes.
COPY Directory.Build.props ./
COPY src/Kitaphane.Domain/Kitaphane.Domain.csproj src/Kitaphane.Domain/
COPY src/Kitaphane.Application/Kitaphane.Application.csproj src/Kitaphane.Application/
COPY src/Kitaphane.Infrastructure/Kitaphane.Infrastructure.csproj src/Kitaphane.Infrastructure/
COPY src/Kitaphane.API/Kitaphane.API.csproj src/Kitaphane.API/
RUN dotnet restore src/Kitaphane.API/Kitaphane.API.csproj

COPY src/ src/
RUN dotnet publish src/Kitaphane.API/Kitaphane.API.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Workstation GC keeps memory low on small (free tier) instances.
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY --from=build /app/publish .

# The base image provides the non-root "app" user.
USER $APP_UID
EXPOSE 8080

# The host passes the port to listen on in $PORT (8080 when not set).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Kitaphane.API.dll"]
