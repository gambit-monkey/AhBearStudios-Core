# AhBearStudios Logging System Overview

## Introduction

The AhBearStudios Logging System is a modular, high-performance, and Burst-compatible logging framework designed for Unity. It supports managed and unmanaged logging workflows with support for multithreaded and job-based systems.

---

## Core Features

- **Burst-Compatible Logging** – Log from jobs using `IBurstLogger` and `UnsafeLogQueue`.
- **Managed Logging** – Log directly from MonoBehaviours and game systems.
- **Configurable Middleware Pipeline** – Filter, enrich, or transform logs.
- **Log Targets** – Output logs to console, file, Unity UI, or custom destinations.
- **Log Tagging System** – Group and filter logs by domains.
- **Custom Attributes** – Auto-tag logs via annotations.
- **Events** – Hook into log lifecycle events.
- **Formatters** – Control output format for each target.
- **Extension Methods** – Cleaner syntax for logging in both job and managed code.

---

## Documentation Index

| Domain              | Description |
|---------------------|-------------|
| [Logging Configuration](./loggingconfiguration.md) | How to set up and control the logging system. |
| [IBurstLogger](./iburstlogger.md) | Burst-safe logging interface for jobs. |
| [Attributes](./attributes.md) | LogTag and other attributes used to decorate log sources. |
| [Events](./events.md) | Hook into the lifecycle of log messages. |
| [Middleware](./middleware.md) | Intercept, filter, or modify log messages. |
| [Tagging](./tagging.md) | Use tags to filter or group logs by domain. |
| [Log Targets](./logtargets.md) | Where log messages go (Unity Console, files, etc). |
| [Log Data](./data.md) | Structure of log messages and metadata. |
| [Interfaces](./interfaces.md) | Core contracts: `ILogTarget`, `ILogFormatter`, etc. |
| [Extensions](./extensions.md) | Helper methods for simplified logging syntax. |
| [Formatters](./logformatters.md) | Convert log data into human-readable strings. |

---

## Example Setup

```csharp
var config = new LoggingConfiguration();
config.MinLogLevel = LogLevel.Warning;
config.AddLogTarget(new UnityLogTarget());
config.Apply();
```

---

## Logging from Burst Jobs

```csharp
[BurstCompile]
public struct MyJob : IJob
{
    public UnsafeLogQueue.Writer LogWriter;
    public IBurstLogger Logger;

    public void Execute()
    {
        Logger.Info(ref LogWriter, "Job started!");
    }
}
```

---

## Extendability

You can extend the system by:

- Implementing custom `ILogTarget` (e.g., for remote APIs or visual consoles).
- Creating a new `ILogFormatter` (e.g., JSON, colored output).
- Building custom middleware (e.g., tag-based filtering).
- Using `LogTagAttribute` to automate tagging of logs.

---

## License

MIT License – AhBearStudios
