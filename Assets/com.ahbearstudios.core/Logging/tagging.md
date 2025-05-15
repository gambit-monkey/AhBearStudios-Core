# Log Tagging

## Overview

Tags allow categorizing logs by system or domain for better filtering and diagnostics.

### Usage

```csharp
var logger = new TaggedBurstLogger();
logger.Tag(new LogTag("AI"));
```

### Filtering
Tags can be filtered using middleware or configuration rules.