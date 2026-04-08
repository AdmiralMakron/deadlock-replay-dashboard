# Deadlock Replay Dashboard

A .NET 10 project for parsing Deadlock `.dem` replay files using [demofile-net](https://github.com/saul/demofile-net) and rendering interactive post-game analytics dashboards in the browser.

## Starting State

This repository contains a minimal scaffolded baseline for building a Deadlock replay analytics application. It is not a finished product. The starting state includes:

- A scaffolded Blazor Server project targeting .NET 10
- The `DemoFile.Game.Deadlock` NuGet package already referenced in the project file
- The full [demofile-net](https://github.com/saul/demofile-net) source code vendored in the `demofile-net/` directory as a read-only reference for understanding the parser API, available entity types, and event handlers
- A multi-stage Dockerfile that builds and runs the application inside a container
- A Docker Compose configuration for one-command startup
- A sample `.dem` file hosted as a GitHub Release asset, automatically pulled into the `demos/` directory during the Docker build (large files are not committed to git)
- This README and an acknowledgements file

The Blazor scaffold includes the default template pages (Home, Counter, Weather) which are placeholder content and can be freely modified or removed.

## Prerequisites

- Docker Desktop with WSL2 integration (or Docker on Linux)
- Git

## Quick Start

```bash
git clone https://github.com/AdmiralMakron/deadlock-replay-dashboard.git
cd deadlock-replay-dashboard
docker compose up --build
```

Open `http://localhost:5100` in your browser.

## Demo Files

The `.dem` replay files are hosted as GitHub Release assets and pulled automatically during the Docker build. The sample demo file is `48525700.dem` and is available at `/app/demos/48525700.dem` inside the container once built.

To populate the demos directory manually without Docker:

```bash
mkdir -p demos
curl -L https://github.com/AdmiralMakron/deadlock-replay-dashboard/releases/download/v1.0.0/48525700.dem -o demos/48525700.dem
```

You can also drop any other Deadlock `.dem` file into the `demos/` directory.

## Running Without Docker

Requires .NET 10 SDK installed locally.

```bash
dotnet restore
dotnet run
```

The app will start on `http://localhost:5000` or `https://localhost:5001`.

## Project Structure

```
├── Components/
│   ├── Layout/          # Blazor layout components
│   └── Pages/           # Default Blazor template pages
├── demofile-net/        # Vendored demofile-net source code (read-only reference)
├── wwwroot/             # Static assets (CSS, images)
├── demos/               # Demo files (gitignored, pulled at build time)
├── Program.cs           # App entry point
├── Dockerfile           # Multi-stage build
├── docker-compose.yml   # One-command startup
└── DeadlockDashboard.csproj
```

## Tech Stack

- **Framework:** Blazor Server on .NET 10
- **Demo Parser:** [demofile-net](https://github.com/saul/demofile-net) (`DemoFile.Game.Deadlock`)
- **Containerization:** Docker with multi-stage builds
- **Web Server:** Kestrel (built into .NET)

## Exploring the demofile-net Source

The `demofile-net/` directory contains a read-only copy of the demofile-net source code. The most relevant paths for working with Deadlock demos are:

- `demofile-net/src/DemoFile.Game.Deadlock/` for Deadlock-specific parser types, entity classes, and event handlers
- `demofile-net/examples/` for usage patterns and example code

This directory is excluded from compilation in the main project via the `.csproj` file and should not be modified. It exists purely as a reference.

## License

MIT — see [LICENSE](LICENSE) for details.

This project uses [DemoFile.Net](https://github.com/saul/demofile-net) by Saul Rennison — see [ACKNOWLEDGEMENTS](ACKNOWLEDGEMENTS).
