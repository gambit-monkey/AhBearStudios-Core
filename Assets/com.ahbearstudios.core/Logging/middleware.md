# Logging Middleware

## Overview

Middleware allows pre-processing of logs before they reach formatters or targets.

### Use Cases
- Filtering by tag
- Injecting metadata
- Modifying log messages

### Example

```csharp
public class FilterByTagMiddleware : ILoggingMiddleware
{
    public bool ShouldProcess(LogData data) => data.Tag == "Network";
}
```