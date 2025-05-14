# Phase 0: Project Setup

## Objective

Establish a consistent and scalable foundational project structure, with high-performance potential, clean modularity, and developer workflow optimization.

---

## Folder Structure

```
Assets/
├── AhBearStudios.Core/
│   ├── Common/
│   ├── DependencyInjection/
│   ├── Logging/
│   ├── Messaging/
│   ├── Metrics/
│   ├── Database/
│   ├── Pooling/
│   ├── Networking/
│   ├── Systems/
│   ├── Tests/
│   └── Editor/
├── ThirdParty/
├── Scenes/
├── Scripts/
└── Resources/
```

## Assembly Definitions (asmdef)

Each module has its own `*.asmdef` to enforce isolation and control dependencies.

```
AhBearStudios.Core.Common
AhBearStudios.Core.DependencyInjection
AhBearStudios.Core.Logging
AhBearStudios.Core.Messaging
AhBearStudios.Core.Metrics
AhBearStudios.Core.Database
AhBearStudios.Core.Pooling
AhBearStudios.Core.Networking
AhBearStudios.Core.Editor
AhBearStudios.Core.Tests
```

* Only `Common` should be referenced across all core systems.
* `Editor` and `Tests` assemblies are optional consumers and validators.

---

## Naming Conventions

* Namespace root: `AhBearStudios.Core`
* Interface prefix: `I` (e.g. `ILogger`, `IDatabase`)
* Generic pools: `IPool<T>`, `IObjectPool<T>`, etc.

---

## Git Version Control

* Include `.gitignore` for Unity projects
* Use GitHub with branch protections for `main` and `develop`
* Standard branching model: `feature/`, `fix/`, `hotfix/`

---

## CI/CD Setup (GitHub Actions)

* Automatic test running on all push and PR events
* Headless Unity test runners for EditMode and PlayMode
* Use `game-ci/unity-actions` for setup

**See**: `ci/unity-ci.yml`

---

## Initial Test Setup

* Place tests in `AhBearStudios.Core.Tests`
* Create one unit test per system to verify project bootstraps

```csharp
[TestFixture]
public class LoggerTests {
    [Test] public void Logger_AlwaysPasses() => Assert.IsTrue(true);
}
```

---

## Environment Setup

* Unity LTS version (e.g. `2022.3.12f1`)
* Enable Burst & Collections package (v2)
* Configure Jobs Debugger & Leak Detection for dev

---

## Outcome of Phase 0

* Modular, testable Unity project structure
* CI pipeline validating each commit
* All teams aligned on naming, structure, and boundaries

---

**Next:** [Phase 1 - Core Interfaces](02_Phase1_CoreInterfaces.md)
