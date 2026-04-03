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

# Create demo directories.
#  - /app/demos        : bind-mount target for user-supplied .dem files (docker-compose maps ./demos here)
#  - /app/bundled-demos: baked into the image so the sample demo is always available even when /app/demos is mounted empty
RUN mkdir -p /app/demos /app/bundled-demos

# Pull bundled sample demo from GitHub Release into the image.
RUN curl -L https://github.com/AdmiralMakron/deadlock-replay-dashboard/releases/download/v1.0.0/48525700.dem -o /app/bundled-demos/48525700.dem \
    && echo "5aa5e9840f1c5ab7162defe0732e311698af95f27d330554c30a9c4d7a0d3c61  /app/bundled-demos/48525700.dem" | sha256sum -c -

# Copy published app from build stage
COPY --from=build /app/publish ./

# Blazor Server listens on 8080 by default in .NET 8+
EXPOSE 8080

ENTRYPOINT ["dotnet", "DeadlockDashboard.dll"]
