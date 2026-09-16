# Server Monitoring and Notification System

A distributed system for collecting server statistics (CPU, memory), detecting anomalies and high-usage conditions, and broadcasting real-time alerts — built with .NET, RabbitMQ, MongoDB, and SignalR.

## Architecture Overview

The system is composed of three independently deployable services that communicate through a message broker and a real-time hub. All cross-cutting concerns (messaging, persistence) are implemented behind abstractions to keep the services decoupled from specific technology choices (RabbitMQ, MongoDB).

```
Stat-Collector-Service
        │  (IMessagePublisher → publishes "ServerStatistics.<ServerIdentifier>")
        ▼
      RabbitMQ  (Topic Exchange)
        │  (IMessageConsumer → subscribes "ServerStatistics.*")
        ▼
AnomalyDetectionService
        │
        ├──► IStatisticsRepository ──► MongoDB
        │
        └──► hosts AlertsHub (SignalR)
                    │
                    ▼
          Event-Consumer-Service (SignalR client)
```

## Projects

### Executable Services

| Project | Type | Responsibility |
|---|---|---|
| `Stat-Collector-Service` | Worker Service | Collects CPU/memory/available-memory stats every `SamplingIntervalSeconds` and publishes them to the message queue. |
| `AnomalyDetectionService` | ASP.NET Core | Consumes statistics, persists them to MongoDB, runs anomaly/high-usage detection, hosts the SignalR hub, and broadcasts alerts. |
| `Event-Consumer-Service` | Console App | Connects to the SignalR hub as a client and prints incoming alerts to the console. |

### Class Libraries (Abstractions & Implementations)

| Project | Type | Contains |
|---|---|---|
| `Shared` | Class Library | The shared `ServerStatistics` data contract used across services. |
| `Messaging.Abstractions` | Class Library | `IMessagePublisher`, `IMessageConsumer` — messaging contracts, no broker-specific code. |
| `Messaging.RabbitMQ` | Class Library | `RabbitMqPublisher`, `RabbitMqConsumer` — the RabbitMQ implementation of the messaging contracts (reusable client library, Optional Task 4). |
| `Persistence.Abstractions` | Class Library | `IStatisticsRepository` — persistence contract, no database-specific code. |
| `Persistence.MongoDB` | Class Library | `MongoDbRepository` — the MongoDB implementation of the persistence contract. |

This structure follows the **Dependency Inversion Principle**: the executable services depend only on the `*.Abstractions` interfaces. Swapping RabbitMQ for Kafka, or MongoDB for another database, only requires a new implementation project and a one-line change in each service's dependency-injection registration — no changes to the services' business logic.

## How It Works

1. **`Stat-Collector-Service`** samples memory usage, available memory, and CPU usage via `System.Diagnostics.PerformanceCounter`, wraps them in a `ServerStatistics` object (with `ServerIdentifier` and `Timestamp`), and publishes it via `IMessagePublisher` under the topic `ServerStatistics.<ServerIdentifier>`.
2. **RabbitMQ** routes the message through a **topic exchange**, matching any binding pattern such as `ServerStatistics.*`.
3. **`AnomalyDetectionService`** subscribes to `ServerStatistics.*` via `IMessageConsumer`. For every message received, it:
   - Persists the reading to MongoDB via `IStatisticsRepository`.
   - Compares it against the previous reading for the same server (kept in an in-memory `ConcurrentDictionary`) to detect **anomalies** (sudden spikes).
   - Compares the current reading against configured thresholds to detect **high usage**.
   - Broadcasts an `AnomalyAlert` or `HighUsageAlert` event via the hosted SignalR hub (`AlertsHub`) when a condition is triggered.
4. **`Event-Consumer-Service`** connects to `AlertsHub` as a SignalR client and prints any alert it receives to the console.

## Alert Logic

**Anomaly Alerts** (compare current reading to the previous reading for the same server):
```
Memory: CurrentMemoryUsage > PreviousMemoryUsage * (1 + MemoryUsageAnomalyThresholdPercentage)
CPU:    CurrentCpuUsage    > PreviousCpuUsage    * (1 + CpuUsageAnomalyThresholdPercentage)
```

**High Usage Alerts** (based on the current reading only):
```
Memory: CurrentMemoryUsage / (CurrentMemoryUsage + CurrentAvailableMemory) > MemoryUsageThresholdPercentage
CPU:    CurrentCpuUsage > CpuUsageThresholdPercentage
```

> **Note:** `CpuUsage` is measured as a percentage in the 0–100 range (via `PerformanceCounter`), while `CpuUsageThresholdPercentage` in configuration is expressed as a fraction (0–1). The threshold is multiplied by 100 at read time to keep the comparison consistent.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (targeting `net10.0`)
- [Docker](https://www.docker.com/) — to run RabbitMQ and MongoDB locally (or use hosted alternatives, e.g. CloudAMQP / MongoDB Atlas, if virtualization isn't available on your machine)
- Windows (the current `ISystemStatsProvider` implementation uses `PerformanceCounter`, which is Windows-only; a Linux/macOS implementation can be added behind the same interface)

## Running Locally

### 1. Start the infrastructure

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
docker run -d --name mongodb -p 27017:27017 mongo
```

- RabbitMQ management UI: `http://localhost:15672` (default credentials: `guest` / `guest`)
- MongoDB is reachable at `mongodb://localhost:27017`

### 2. Configure each service

Each service reads its settings from `appsettings.json`.

**`Stat-Collector-Service/appsettings.json`**
```json
{
  "ServerStatisticsConfig": {
    "SamplingIntervalSeconds": 60,
    "ServerIdentifier": "linux1"
  }
}
```

**`AnomalyDetectionService/appsettings.json`**
```json
{
  "AnomalyDetectionConfig": {
    "MemoryUsageAnomalyThresholdPercentage": 0.4,
    "CpuUsageAnomalyThresholdPercentage": 0.5,
    "MemoryUsageThresholdPercentage": 0.8,
    "CpuUsageThresholdPercentage": 0.9
  }
}
```

**`Event-Consumer-Service/appsettings.json`**
```json
{
  "SignalRConfig": {
    "SignalRUrl": "http://localhost:5085/alertsHub"
  }
}
```

### 3. Run the services (in separate terminals, in this order)

```bash
# Terminal 1
cd AnomalyDetectionService
dotnet run

# Terminal 2
cd Event-Consumer-Service
dotnet run

# Terminal 3
cd Stat-Collector-Service
dotnet run
```

If everything is wired correctly, the `Stat-Collector-Service` terminal will show statistics being published every sampling interval, the `AnomalyDetectionService` terminal will show messages being received and persisted, and the `Event-Consumer-Service` terminal will print any `AnomalyAlert` or `HighUsageAlert` broadcast.

## Design Decisions

- **`ServerStatistics` lives in a shared `Shared` project** rather than being duplicated per service, since both the publisher and the consumer need to agree on its shape (including `ServerIdentifier`, which is embedded in the message payload in addition to being part of the routing key).
- **Messaging and persistence interfaces are broker/database-agnostic.** `IMessagePublisher` / `IMessageConsumer` say nothing about exchanges, bindings, or queues — those are RabbitMQ-specific concepts confined entirely to `Messaging.RabbitMQ`.
- **The SignalR hub is hosted inside `AnomalyDetectionService`**, not as a separate process — it's a logical responsibility (an "endpoint" in the architecture diagram) rather than a separate deployable unit.
- **Windows-specific code (`PerformanceCounter`) is isolated behind `ISystemStatsProvider`**, so a future Linux/macOS implementation (e.g. reading `/proc/meminfo` and `/proc/stat`) can be added without touching the collector's core logic.

## Optional / Future Work

- [ ] **Task 4:** `Messaging.RabbitMQ` is already structured as a standalone, reusable class library.
- [ ] **Task 5:** Containerize each service, moving configuration from `appsettings.json` to environment variables.
- [ ] **Task 6:** Add a `docker-compose.yml` to orchestrate all services alongside RabbitMQ and MongoDB.
