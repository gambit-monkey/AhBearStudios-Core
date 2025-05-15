# Log Data Structures

## Overview

The `LogData` struct carries log information such as message, timestamp, log level, and tags.

### Fields
- `Message`
- `Level`
- `Tag`
- `Timestamp`

### Example

```csharp
LogData data = new LogData(LogLevel.Info, "Startup complete");
```