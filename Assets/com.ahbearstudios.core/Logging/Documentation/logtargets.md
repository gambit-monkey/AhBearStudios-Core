# Log Targets

## Overview

Log targets are endpoints where log messages are written to (console, file, remote).

### Built-in Targets
- `UnityLogTarget`
- `FileLogTarget`

### Custom Target Example

```csharp
public class MyCustomTarget : ILogTarget
{
    public void Write(LogData data) {
        // Send to remote server
    }
}
```