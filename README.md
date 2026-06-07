# SampleIOT

A simulated IoT device monitoring API built with ASP.NET Core. Provides real-time telemetry data from 29 virtual devices — appliances, light fixtures, and solar panels — via REST and Server-Sent Events.

[Get started](#getting-started) · [Endpoints](#endpoints) · [Architecture](#architecture) · [Testing](#testing)

## Overview

SampleIOT replays time-of-day telemetry from CSV files on a daily cycle, advancing every 5 seconds. Clients can:

- Browse and query the device catalog
- Retrieve historical telemetry (with optional limits and disaggregation)
- Subscribe to live updates via an SSE stream

Responses support both `application/json` and `text/html` (HTMX) via `Accept` header negotiation.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    ASP.NET Core App                      │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ DeviceCtrl   │  │ TelemetryCtrl│  │ SSE Controller│  │
│  └──────┬───────┘  └──────┬───────┘  └───────┬───────┘  │
│         │                 │                   │          │
│  ┌──────▼───────┐  ┌──────▼───────────────────▼───────┐  │
│  │ DeviceService│  │      TelemetryService (timer)     │  │
│  └──────┬───────┘  └──────┬───────────────────────────┘  │
│         │                 │                              │
└─────────┼─────────────────┼──────────────────────────────┘
          │                 │
   Data/Device/      Data/Telemetry/
   Devices.json      *.csv (29 files)
```

- **DeviceService** — loads `Devices.json` at startup; serves the device catalog in memory.
- **TelemetryService** — loads per-device CSV files, preloads past readings, then runs a 5-second timer to inject future readings into the live store and push SSE notifications.

## Endpoints

> Base URL: `https://sampleiot.zackcheng.com`

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/Device` · `/devices` | List all devices. Query: `?sort=id\|type` |
| `GET` | `/api/Device/{id}` · `/devices/{id}` | Get a single device by ID |
| `GET` | `/api/Telemetry/{id}` · `/telemetry/{id}` | Get telemetry for a device. Query: `?limit=N&disaggregated=true` |
| `GET` | `/api/TelemetrySSE/Subscribe/{deviceId}` · `/telemetry/subscribe/{deviceId}` | SSE stream for real-time telemetry |

### Response formats

| `Accept` header | Response |
|-----------------|----------|
| `application/json` | JSON |
| `text/html` | HTML (HTMX fragments) |

## Device types

| Type | Count | Telemetry |
|------|-------|-----------|
| Appliance | 6 | `Power_Consumed` |
| LightFixture | 12 | `Power_Consumed`, `Luminous_Efficacy` |
| SolarPanel | 12 | `Power_Generated`, `Temperature`, `Voltage` |

## Getting started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop/) (optional, for containerized runs)

### Run locally

```bash
cd SampleIOT.API
dotnet run
```

The API starts on `https://localhost:5001` with Swagger UI at `/swagger`.

### Run with Docker

```bash
docker build -t sampleiot -f SampleIOT.API/Dockerfile .
docker run -p 8080:80 sampleiot
```

## Testing

```bash
dotnet test
```

The test suite uses **xUnit** with:
- **Integration tests** — full HTTP surface via `WebApplicationFactory` (routing, CORS, SSE, content negotiation)
- **Unit tests** — service logic with **Moq** mocks
- **Coverage** — **coverlet** collector

## Project structure

```
SampleIOT.API/
├── Controllers/          # DeviceController, TelemetryController, TelemetrySSEController
├── Services/             # DeviceService, TelemetryService (+ interfaces)
├── Models/               # Device, Telemetry, DeviceTelemetry
├── Data/
│   ├── Device/           # Devices.json
│   └── Telemetry/        # per-device CSV files
├── Program.cs            # entry point, DI, middleware pipeline
└── Dockerfile

SampleIOT.API.Tests/
├── IntegrationTests/     # WebApplicationFactory-based HTTP tests
└── UnitTests/            # Moq-based service tests
```

## Live demo

- **API:** <https://sampleiot.zackcheng.com>
- **Swagger UI:** <https://sampleiot.zackcheng.com/swagger>
- **Frontend:** <https://iothouse.zackcheng.com> · <https://zkkzkk32312.github.io/IOTHouse/>

> [!NOTE]
> The telemetry simulation runs on a daily cycle. At midnight (server time) the cycle resets.


