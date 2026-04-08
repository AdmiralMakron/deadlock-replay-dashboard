# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for restore-layer caching.
COPY DeadlockDashboard.sln ./
COPY DeadlockDashboard.Core/DeadlockDashboard.Core.csproj DeadlockDashboard.Core/
COPY DeadlockDashboard.Shared/DeadlockDashboard.Shared.csproj DeadlockDashboard.Shared/
COPY DeadlockDashboard.Api/DeadlockDashboard.Api.csproj DeadlockDashboard.Api/
COPY DeadlockDashboard.Web/DeadlockDashboard.Web.csproj DeadlockDashboard.Web/
COPY DeadlockDashboard.Host/DeadlockDashboard.Host.csproj DeadlockDashboard.Host/
RUN dotnet restore DeadlockDashboard.sln

# Copy the rest and publish.
COPY DeadlockDashboard.Core/ DeadlockDashboard.Core/
COPY DeadlockDashboard.Shared/ DeadlockDashboard.Shared/
COPY DeadlockDashboard.Api/ DeadlockDashboard.Api/
COPY DeadlockDashboard.Web/ DeadlockDashboard.Web/
COPY DeadlockDashboard.Host/ DeadlockDashboard.Host/
RUN dotnet publish DeadlockDashboard.Host/DeadlockDashboard.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for pulling demo files
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates && rm -rf /var/lib/apt/lists/*

# Create demos directory and pull sample demo file
RUN mkdir -p /app/demos && \
    curl -L https://github.com/AdmiralMakron/deadlock-replay-dashboard/releases/download/v1.0.0/48525700.dem -o /app/demos/48525700.dem && \
    echo "5aa5e9840f1c5ab7162defe0732e311698af95f27d330554c30a9c4d7a0d3c61  /app/demos/48525700.dem" | sha256sum -c -

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DeadlockDashboard.Host.dll"]
