# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for cached restore
COPY DeadlockDashboard.csproj ./
COPY src/DeadlockDashboard.Core/DeadlockDashboard.Core.csproj src/DeadlockDashboard.Core/
COPY src/DeadlockDashboard.Shared/DeadlockDashboard.Shared.csproj src/DeadlockDashboard.Shared/
COPY src/DeadlockDashboard.Api/DeadlockDashboard.Api.csproj src/DeadlockDashboard.Api/
COPY src/DeadlockDashboard.Web/DeadlockDashboard.Web.csproj src/DeadlockDashboard.Web/
RUN dotnet restore DeadlockDashboard.csproj

# Copy everything else and publish
COPY . ./
RUN dotnet publish DeadlockDashboard.csproj -c Release -o /app/publish

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for pulling demo files
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create demos directory
RUN mkdir -p /app/demos

# Pull demo files from GitHub Release
RUN curl -L https://github.com/AdmiralMakron/deadlock-replay-dashboard/releases/download/v1.0.0/48525700.dem -o /app/demos/48525700.dem \
    && echo "5aa5e9840f1c5ab7162defe0732e311698af95f27d330554c30a9c4d7a0d3c61  /app/demos/48525700.dem" | sha256sum -c -

# Copy published app from build stage
COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "DeadlockDashboard.dll"]
