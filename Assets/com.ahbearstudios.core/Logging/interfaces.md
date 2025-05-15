# Interfaces

## Overview

The logging system is built around interfaces for extensibility.

### Key Interfaces
- `IBurstLogger`
- `ILogTarget`
- `ILogFormatter`
- `ILoggingMiddleware`

### Example

```csharp
public interface ILogTarget
{
    void Write(LogData data);
}
```