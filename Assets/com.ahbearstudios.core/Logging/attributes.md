# Logging Attributes

## Overview

Attributes are used to annotate classes, methods, or structs to control log behavior.

### Available Attributes

- `[LogTag("AI")]` – Automatically tags logs from the class with the specified tag.

### Example

```csharp
[LogTag("UI")]
public class UIManager
{
    // Logs emitted here are tagged "UI"
}
```