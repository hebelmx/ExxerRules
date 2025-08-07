# ExxerRules - Modern Development Conventions for VS Code

[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![VS Code Extension](https://img.shields.io/badge/VS%20Code-Extension-blue.svg)](https://marketplace.visualstudio.com/)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

**ExxerRules** is a comprehensive suite of 20 production-ready Roslyn analyzers that enforce Modern Development Conventions (MDC) for C# projects in Visual Studio Code. Designed to promote clean, maintainable, and high-performance code through automated analysis and guidance.

## 🚀 Key Features

- **20 Production-Ready Analyzers** organized across 10 categories
- **Result<T> Pattern Enforcement** for robust error handling
- **Clean Architecture Validation** to maintain separation of concerns  
- **Modern Testing Standards** (xUnit v3, NSubstitute, Shouldly)
- **Async Best Practices** with proper CancellationToken usage
- **Performance Optimization** guidance
- **Zero Configuration** - works out of the box
- **Fully Customizable** - enable/disable categories as needed

## 📦 Analyzer Categories

### 🏗️ **Architecture** (2 analyzers)
- **ER1001**: Domain Should Not Reference Infrastructure
- **ER1002**: Use Repository Pattern

### ⚡ **Async** (2 analyzers)  
- **ER2001**: Async Methods Should Accept CancellationToken
- **ER2002**: Use ConfigureAwait(false)

### 🔧 **Code Formatting** (2 analyzers)
- **ER3001**: Code Formatting Standards
- **ER3002**: Project Formatting Standards

### 💎 **Code Quality** (2 analyzers)
- **ER4001**: Avoid Magic Numbers and Strings
- **ER4002**: Do Not Use Regions

### 📚 **Documentation** (1 analyzer)
- **ER5001**: Public Members Should Have XML Documentation

### 🛡️ **Error Handling** (2 analyzers)
- **ER6001**: Avoid Throwing Exceptions
- **ER6002**: Use Result Pattern

### 🔄 **Functional Patterns** (1 analyzer)
- **ER7001**: Do Not Throw Exceptions in Functional Code

### 📊 **Logging** (2 analyzers)
- **ER8001**: Do Not Use Console.WriteLine
- **ER8002**: Use Structured Logging

### ✨ **Modern C#** (2 analyzers)
- **ER9001**: Use Expression-Bodied Members
- **ER9002**: Use Modern Pattern Matching

### 🛡️ **Null Safety** (1 analyzer)
- **ER10001**: Validate Null Parameters

### ⚡ **Performance** (1 analyzer)
- **ER11001**: Use Efficient LINQ

### 🧪 **Testing** (5 analyzers)
- **ER12001**: Do Not Mock DbContext
- **ER12002**: Do Not Use FluentAssertions
- **ER12003**: Do Not Use Moq
- **ER12004**: Test Naming Convention
- **ER12005**: Use xUnit v3

## 🔧 Installation

1. **Install from VS Code Marketplace**:
   - Open VS Code
   - Go to Extensions (Ctrl+Shift+X)
   - Search for "ExxerRules"
   - Click "Install"

2. **Install from VSIX**:
   ```bash
   code --install-extension exxer-rules-vscode-1.0.0.vsix
   ```

## ⚙️ Configuration

ExxerRules works out-of-the-box with sensible defaults. Customize via VS Code settings:

```json
{
  "exxerRules.enabled": true,
  "exxerRules.severity": "warning",
  "exxerRules.categories": {
    "architecture": true,
    "async": true,
    "errorHandling": true,
    "testing": true,
    "performance": true,
    "modernCSharp": true,
    "codeQuality": true
  }
}
```

### Configuration Options

- **`exxerRules.enabled`**: Enable/disable all analyzers (default: `true`)
- **`exxerRules.severity`**: Default severity level (`error`, `warning`, `info`, `hint`)
- **`exxerRules.categories`**: Enable/disable specific analyzer categories

## 🎯 Commands

- **`ExxerRules: Analyze Workspace`** - Run analyzers on entire workspace
- **`ExxerRules: Show Analyzer Information`** - Display detailed analyzer information

Access via Command Palette (Ctrl+Shift+P) or right-click context menu.

## 🏆 Modern Development Conventions

ExxerRules enforces industry best practices:

### ✅ **Recommended Patterns**
- **Result<T>** for error handling instead of exceptions
- **Repository Pattern** for data access abstraction
- **xUnit v3** with **NSubstitute** and **Shouldly** for testing
- **CancellationToken** for async operations
- **ConfigureAwait(false)** for library code
- **Structured logging** with **ILogger<T>**

### ❌ **Anti-Patterns Detected**
- Exception throwing for control flow
- Direct infrastructure references from domain
- Magic numbers and strings
- Console.WriteLine for logging
- Legacy test frameworks (MSTest, NUnit)
- FluentAssertions and Moq (replaced by better alternatives)

## 🛠️ Requirements

- **Visual Studio Code** 1.74.0 or higher
- **.NET SDK** 8.0 or higher
- **C# Dev Kit** extension (recommended)

## 📁 Project Structure Support

ExxerRules automatically integrates with:
- **.csproj** files (automatic analyzer reference injection)
- **Directory.Build.props** (centralized configuration)
- **EditorConfig** (formatting standards)
- **Global.json** (SDK version management)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/exxerai/exxer-rules/CONTRIBUTING.md).

## 📄 License

Licensed under the [MIT License](LICENSE). Free and open-source.

## 🔗 Links

- **GitHub Repository**: https://github.com/exxerai/exxer-rules
- **Issues & Bug Reports**: https://github.com/exxerai/exxer-rules/issues
- **Visual Studio Extension**: Available on Visual Studio Marketplace

## 📞 Support

- **GitHub Issues**: Report bugs and feature requests
- **Documentation**: Full analyzer documentation available in repository
- **Community**: Join discussions in GitHub Discussions

---

**Built with ❤️ by the ExxerAI Team**

*Making C# development more reliable, maintainable, and performant.*
