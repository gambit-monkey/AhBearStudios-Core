# Log Formatters

## Overview

Formatters control how `LogData` is converted into strings for output.

### Built-in Formatters
- `DefaultLogFormatter`

### Custom Formatter Example

```csharp
public class JsonFormatter : ILogFormatter
{
    public string Format(LogData data)
    {
        return JsonUtility.ToJson(data);
    }
}
```