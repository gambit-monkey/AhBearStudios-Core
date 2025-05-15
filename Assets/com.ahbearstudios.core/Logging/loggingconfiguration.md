# LoggingConfiguration

## Overview

`LoggingConfiguration` is the central class used to set up, configure, and manage loggers, targets, and settings in the AhBearStudios Logging System.

### Example

```csharp
var config = new LoggingConfiguration();
config.MinLogLevel = LogLevel.Warning;
config.AddLogTarget(new UnityLogTarget());
config.Apply();
```

## Features
- Set global log level
- Register `ILogTarget`s
- Register loggers
- Control log flushing and initialization