# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY Fenrir.SecurityPlatform.slnx ./
COPY src/Fenrir.Api/Fenrir.Api.csproj src/Fenrir.Api/
COPY src/Fenrir.Application/Fenrir.Application.csproj src/Fenrir.Application/
COPY src/Fenrir.Contracts/Fenrir.Contracts.csproj src/Fenrir.Contracts/
COPY src/Fenrir.Domain/Fenrir.Domain.csproj src/Fenrir.Domain/
COPY src/Fenrir.Infrastructure/Fenrir.Infrastructure.csproj src/Fenrir.Infrastructure/
COPY agent/Fenrir.Agent.csproj agent/
COPY tests/Fenrir.Tests/Fenrir.Tests.csproj tests/Fenrir.Tests/
RUN dotnet restore Fenrir.SecurityPlatform.slnx

COPY . .
RUN dotnet publish src/Fenrir.Api/Fenrir.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data/darkweb \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /app/data /home/app/.aspnet

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
USER app

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Fenrir.Api.dll"]
