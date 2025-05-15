# Extension Methods

## Overview

Provides helpers to simplify logging calls, often used to reduce boilerplate.

### Example

```csharp
public static class LoggingExtensions
{
    public static void Info(this IBurstLogger logger, ref UnsafeLogQueue.Writer writer, string message)
    {
        logger.Info(ref writer, new FixedString128Bytes(message));
    }
}
```