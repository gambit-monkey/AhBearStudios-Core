# AhBearStudios Core - Development Commands Reference

Quick reference for common development tasks in the AhBearStudios Core Unity project.

## Building the Project

### Core Assemblies
```bash
# Build all core assemblies
dotnet build AhBearStudios.Core.csproj

# Build specific system
dotnet build AhBearStudios.Core.Logging.csproj
dotnet build AhBearStudios.Core.Messaging.csproj
dotnet build AhBearStudios.Core.Pooling.csproj
```

## Running Tests

### Edit Mode Tests (Unit Tests)
```bash
# Run all edit mode tests
dotnet test AhBearStudios.Core.EditMode.Tests.csproj

# Run specific system tests
dotnet test AhBearStudios.Core.Logging.EditMode.Tests.csproj
dotnet test AhBearStudios.Core.Messaging.EditMode.Tests.csproj
dotnet test AhBearStudios.Core.Pooling.EditMode.Tests.csproj
```

### Play Mode Tests (Integration Tests)
```bash
# Run all play mode tests
dotnet test AhBearStudios.Core.PlayMode.Tests.csproj

# Run specific system tests
dotnet test AhBearStudios.Core.Logging.PlayMode.Tests.csproj
dotnet test AhBearStudios.Core.Messaging.PlayMode.Tests.csproj
```

### Unity Test Runner
```bash
# Open Unity Test Runner from Unity Editor:
# Window > General > Test Runner
# 
# Or run from command line:
unity -runTests -testPlatform EditMode -testResults results.xml
unity -runTests -testPlatform PlayMode -testResults results.xml
```

## Unity-Specific Commands

### Project Management
```bash
# Open Unity project
unity -projectPath .

# Open specific Unity version
/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity -projectPath .
```

### Building for Platforms
```bash
# Build for Windows 64-bit
unity -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -buildPath ./Builds/Windows/

# Build for macOS
unity -batchmode -quit -projectPath . -buildTarget StandaloneOSX -buildPath ./Builds/macOS/

# Build for Linux
unity -batchmode -quit -projectPath . -buildTarget StandaloneLinux64 -buildPath ./Builds/Linux/

# Build for Android
unity -batchmode -quit -projectPath . -buildTarget Android -buildPath ./Builds/Android/

# Build for iOS
unity -batchmode -quit -projectPath . -buildTarget iOS -buildPath ./Builds/iOS/
```

### Development Builds
```bash
# Development build with debugging
unity -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -developmentBuild -buildPath ./Builds/Dev/

# Build with profiler connection
unity -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -developmentBuild -connectProfiler -buildPath ./Builds/Profile/
```

## Package Management

### Unity Package Manager
```bash
# Add package via CLI (if available)
unity -batchmode -quit -projectPath . -addPackage com.unity.collections

# List installed packages
unity -batchmode -quit -projectPath . -listPackages
```

### Custom Package Development
```bash
# Create package link for development
# Edit Packages/manifest.json to add:
# "com.ahbearstudios.core": "file:../path/to/package"
```

## Performance and Profiling

### Unity Profiler
```bash
# Connect to running build with profiler
unity -projectPath . -profilerConnectionGUID <build_guid>

# Batch mode profiling
unity -batchmode -quit -projectPath . -runTests -testPlatform PlayMode -enableCodeCoverage
```

### Memory Profiling
```bash
# Memory profiler package commands (from Package Manager)
# Window > Analysis > Memory Profiler
```

## Code Quality

### Code Formatting
```bash
# Format code (if using external formatter)
dotnet format AhBearStudios.Core.sln

# Run code analysis
dotnet build --verbosity normal -warnaserror
```

### Documentation Generation
```bash
# Generate XML documentation
dotnet build -p:GenerateDocumentationFile=true
```

## Git Integration

### Useful Git Commands for Unity
```bash
# Unity-specific gitignore patterns
curl -o .gitignore https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore

# Large file tracking for Unity assets
git lfs track "*.psd" "*.fbx" "*.exr" "*.hdr"
```

## Common Troubleshooting

### Clear Unity Cache
```bash
# Clear Unity Editor cache
rm -rf ~/Library/Unity/Editor/Cache/
rm -rf ~/Library/Unity/Editor/AssetDatabase/

# Windows equivalent:
# rmdir /s "%LOCALAPPDATA%\Unity\Editor\Cache"
```

### Reimport Assets
```bash
# Force reimport all assets
unity -batchmode -quit -projectPath . -reimportAssets
```

### Reset Package Cache
```bash
# Clear Unity Package Manager cache
rm -rf ~/Library/Unity/cache/packages/
```

## Environment Variables

### Unity Hub Integration
```bash
# Set Unity path
export UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity"

# Use in scripts
$UNITY_PATH -batchmode -quit -projectPath .
```

### Build Automation
```bash
# Set environment for CI/CD
export UNITY_LICENSE_CONTENT="<license_content>"
export UNITY_EMAIL="<your_email>"
export UNITY_PASSWORD="<your_password>"
```