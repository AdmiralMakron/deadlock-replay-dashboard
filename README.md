# Deadlock Replay Dashboard

A Blazor Server web application that parses Deadlock `.dem` replay files using [demofile-net](https://github.com/saul/demofile-net) and renders an interactive post-game analytics dashboard in the browser.

This repo serves as a **clean initial state** for evaluating CLI-based LLM coding agents on a feature development task. Two agents start from this identical baseline and are each given a time-boxed session to build out dashboard features. The containerized setup ensures reproducibility and fair comparison.

## What's Included

- Scaffolded Blazor Server project (.NET 10)
- `DemoFile.Game.Deadlock` NuGet dependency (demofile-net)
- Multi-stage Dockerfile for reproducible builds
- Docker Compose for one-command startup
- Demo file pull from GitHub Releases (no large files in git)

## What Agents Are Expected to Build

Starting from this baseline, agents should implement:

- A file selection UI for choosing a `.dem` file
- Server-side parsing of the demo via demofile-net
- An interactive dashboard displaying match analytics (scoreboard, damage charts, kill timeline, hero performance)
- Configurable display settings and filters

## Prerequisites

- Docker Desktop with WSL2 integration (or Docker on Linux)
- Git

## Quick Start

```bash
git clone https://github.com/YOUR_USER/deadlock-replay-dashboard.git
cd deadlock-replay-dashboard
docker compose up --build
```

Open `http://localhost:8080` in your browser.

## Demo Files

The `.dem` replay files are hosted as GitHub Release assets and pulled automatically during the Docker build. To run locally without Docker:

```bash
mkdir -p demos
curl -L https://github.com/YOUR_USER/deadlock-replay-dashboard/releases/download/v1.0.0/match1.dem -o demos/match1.dem
```

You can also drop any Deadlock `.dem` file into the `demos/` directory manually.

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
│   └── Pages/           # Page components (dashboard goes here)
├── wwwroot/             # Static assets (CSS, images)
├── demos/               # Demo files (gitignored, pulled at build)
├── Program.cs           # App entry point
├── Dockerfile           # Multi-stage build
├── docker-compose.yml   # One-command startup
└── DeadlockDashboard.csproj
```

## Tech Stack

- **Framework:** Blazor Server (.NET 10)
- **Demo Parser:** [demofile-net](https://github.com/saul/demofile-net) (`DemoFile.Game.Deadlock`)
- **Containerization:** Docker with multi-stage builds
- **Web Server:** Kestrel (built into .NET)

## License

MIT — see [LICENSE](LICENSE) for details.

This project uses [DemoFile.Net](https://github.com/saul/demofile-net) by Saul Rennison - see [ACKNOWLEDGEMENTS](ACKNOWLEDGEMENTS).
```
