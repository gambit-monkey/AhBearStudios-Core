# Logging Events

## Overview

The logging system exposes events such as `OnLogReceived` which allow external systems to listen for log entries.

### Example

```csharp
LoggingEvents.OnLogReceived += entry => Debug.Log(entry.Message);
```