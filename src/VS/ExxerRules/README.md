# ExxerRules - Roslyn Analyzers for ExxerAI Standards

A comprehensive set of Roslyn analyzers and code fixes that enforce ExxerAI coding standards and best practices.

## Overview

ExxerRules provides real-time code analysis and automatic fixes for:
- Result<T> pattern enforcement
- Testing standards compliance (XUnit v3, Shouldly, NSubstitute)
- Null safety validation
- CancellationToken propagation
- XML documentation coverage
- Clean Architecture boundaries
- Performance patterns
- And much more...

## Installation

### Visual Studio Extension
1. Download the VSIX from the releases page
2. Double-click to install
3. Restart Visual Studio

### NuGet Package
```xml
<PackageReference Include="ExxerRules.Analyzers" Version="1.0.0" />
```

## Key Analyzers

### EXXER001: Result Pattern Enforcement
- Detects exception throwing in methods that should return Result<T>
- Provides code fix to convert to Result pattern

### EXXER002: Testing Standards Compliance
- Ensures XUnit v3 usage
- Enforces test naming convention: `Should_Action_When_Condition`
- Detects forbidden testing frameworks (Moq, FluentAssertions)

### EXXER003: Null Safety Validation
- Checks for proper null parameter validation
- Enforces early null checks in constructors

### EXXER004: CancellationToken Propagation
- Ensures all async methods accept CancellationToken
- Verifies proper propagation through async calls

### EXXER005: XML Documentation Coverage
- Verifies all public APIs have complete XML documentation
- Checks for proper <summary>, <param>, <returns> tags

## Configuration

Configure analyzer severity in `.editorconfig`:

```ini
# ExxerRules analyzer configuration
dotnet_diagnostic.EXXER001.severity = error
dotnet_diagnostic.EXXER002.severity = warning
dotnet_diagnostic.EXXER003.severity = warning
dotnet_diagnostic.EXXER004.severity = error
dotnet_diagnostic.EXXER005.severity = warning
```

## Development

### Prerequisites
- Visual Studio 2022 or later
- .NET 8.0 SDK
- Visual Studio Extension Development workload

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Creating VSIX
```bash
dotnet build ExxerRules.Vsix\ExxerRules.Vsix.csproj
```

## Contributing

Please read our contributing guidelines before submitting pull requests.

## License

MIT License - see LICENSE file for details.