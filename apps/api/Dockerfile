# syntax=docker/dockerfile:1
# Build from the repository root:  docker build -f apps/api/Dockerfile .
# (docker compose does this for you.)

# ---- Build stage: the full .NET SDK ------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project files alone. This layer is only rebuilt when a .csproj, a
# package version or the SDK pin changes - editing a .cs file reuses the cached restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY apps/api/src/SupportDesk.Domain/SupportDesk.Domain.csproj apps/api/src/SupportDesk.Domain/
COPY apps/api/src/SupportDesk.Application/SupportDesk.Application.csproj apps/api/src/SupportDesk.Application/
COPY apps/api/src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj apps/api/src/SupportDesk.Infrastructure/
COPY apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj apps/api/src/SupportDesk.Presentation/
RUN dotnet restore apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj

# Now the source, and publish without restoring again.
COPY apps/api/src apps/api/src
RUN dotnet publish apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- Runtime stage: only the ASP.NET runtime and the published output ---------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# Plain HTTP on 8080, no certificates. The image carries no configuration or secrets: the
# connection string, JWT signing key and environment name are supplied when it runs.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The runtime image ships an unprivileged user called "app"; never run as root.
USER app

ENTRYPOINT ["dotnet", "SupportDesk.Presentation.dll"]
