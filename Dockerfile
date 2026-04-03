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

# Create demos directory and staging directory
RUN mkdir -p /app/demos /app/demo-staging

# Pull demo files from GitHub Release into staging (survives volume mounts)
RUN curl -L https://github.com/AdmiralMakron/deadlock-replay-dashboard/releases/download/v1.0.0/48525700.dem -o /app/demo-staging/48525700.dem \
    && echo "5aa5e9840f1c5ab7162defe0732e311698af95f27d330554c30a9c4d7a0d3c61  /app/demo-staging/48525700.dem" | sha256sum -c -

# Copy published app from build stage
COPY --from=build /app/publish ./

# Startup script: copy staged demos if not already present, then run app
RUN printf '#!/bin/sh\nfor f in /app/demo-staging/*.dem; do\n  [ -f "$f" ] && bn=$(basename "$f") && [ ! -f "/app/demos/$bn" ] && cp "$f" "/app/demos/$bn"\ndone\nexec dotnet DeadlockDashboard.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

# Blazor Server listens on 8080 by default in .NET 8+
EXPOSE 8080

ENTRYPOINT ["/app/entrypoint.sh"]
