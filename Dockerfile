# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies first (cached layer)
COPY DeadlockDashboard.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

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
# TODO: Replace with your actual GitHub release URLs and checksums
# RUN curl -L https://github.com/YOUR_USER/DeadlockDashboard/releases/download/v1.0.0/match1.dem -o /app/demos/match1.dem \
#     && echo "EXPECTED_SHA256  /app/demos/match1.dem" | sha256sum -c -

# Copy published app from build stage
COPY --from=build /app/publish ./

# Blazor Server listens on 8080 by default in .NET 8+
EXPOSE 8080

ENTRYPOINT ["dotnet", "DeadlockDashboard.dll"]
