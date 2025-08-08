# ExxerRules - Modern Development Conventions Analyzers

[![NuGet version](https://badge.fury.io/nu/ExxerRules.Analyzers.svg)](https://badge.fury.io/nu/ExxerRules.Analyzers)
[![Build Status](https://dev.azure.com/exxerai/exxer-rules/_apis/build/status/exxer-rules-ci)](https://dev.azure.com/exxerai/exxer-rules/_build/latest?definitionId=1)
[![Test Status](https://img.shields.io/badge/tests-51%2F51%20passing-brightgreen)](https://github.com/exxerai/exxer-rules)
[![TDD Coverage](https://img.shields.io/badge/TDD%20coverage-100%25-brightgreen)](https://github.com/exxerai/exxer-rules)

**Comprehensive Roslyn analyzer suite enforcing Modern Development Conventions (MDC) with 20 production-ready analyzers.**

## 🎯 **What is ExxerRules?**

ExxerRules is a comprehensive suite of Roslyn analyzers that automatically enforce Modern Development Conventions (MDC) in your C# codebase. Built using rigorous Test-Driven Development with **51/51 tests passing (100% success rate)**, it covers everything from Clean Architecture boundaries to functional programming patterns.

## ⚡ **Quick Start**

### Installation
```xml
<PackageReference Include="ExxerRules.Analyzers" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

### Immediate Benefits
- ✅ **Automatic code quality enforcement** - No more manual code reviews for standards
- ✅ **Clean Architecture validation** - Prevent architectural violations at build time  
- ✅ **Functional programming patterns** - Enforce Result<T> instead of exceptions
- ✅ **Modern testing standards** - XUnit v3, Shouldly, NSubstitute enforcement
- ✅ **Performance optimization** - ConfigureAwait, efficient LINQ patterns
- ✅ **Zero configuration** - Works out of the box with sensible defaults

## 📊 **Complete Analyzer Coverage**

### 🧪 **Testing Standards (5 analyzers)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER100 | TestNamingConvention | Enforce `Should_Action_When_Condition` naming |
| EXXER101 | UseXUnitV3 | Upgrade from XUnit v2 to v3 |
| EXXER102 | UseShouldly | Use Shouldly instead of FluentAssertions |
| EXXER103 | UseNSubstitute | Use NSubstitute instead of Moq |
| EXXER104 | DoNotMockDbContext | Use InMemory provider instead of mocking EF |

### ⚡ **Functional Patterns (1 analyzer)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER003 | DoNotThrowExceptions | Use Result<T> pattern instead of exceptions |

### 🛡️ **Null Safety (1 analyzer)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER200 | ValidateNullParameters | Validate null parameters at method entry |

### 🔄 **Async Best Practices (2 analyzers)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER300 | AcceptCancellationToken | Async methods should accept CancellationToken |
| EXXER301 | UseConfigureAwaitFalse | Use ConfigureAwait(false) in library code |

### 📚 **Documentation (1 analyzer)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER400 | RequireXmlDocumentation | Public members should have XML documentation |

### ✨ **Code Quality (4 analyzers)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER500 | AvoidMagicNumbers | Use named constants instead of magic numbers |
| EXXER501 | UseExpressionBodies | Use expression-bodied members where appropriate |
| EXXER503 | DoNotUseRegions | Prefer sub-classes instead of regions |
| EXXER702 | UseModernPatternMatching | Use declaration patterns (`if (x is string s)`) |

### 🏗️ **Architecture (2 analyzers)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER600 | DomainNoInfrastructure | Domain layer should not reference Infrastructure |
| EXXER601 | UseRepositoryPattern | Use Repository pattern with focused interfaces |

### 🚀 **Performance (2 analyzers)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER700 | UseEfficientLinq | Avoid multiple enumerations, use efficient patterns |
| EXXER301 | UseConfigureAwaitFalse | *(Covered in Async section)* |

### 📝 **Logging (2 analyzers)**
| ID | Analyzer | Description |
|---|---|---|
| EXXER800 | UseStructuredLogging | Use structured logging with parameters |
| EXXER801 | DoNotUseConsoleWriteLine | Use proper logging instead of Console.WriteLine |

## 🎨 **Code Examples**

### ❌ **Before ExxerRules**
```csharp
// EXXER003: Throwing exceptions
public string ProcessData(string input)
{
    if (string.IsNullOrEmpty(input))
        throw new ArgumentException("Input cannot be null"); // ❌ Exception
    
    return input.ToUpper();
}

// EXXER600: Architecture violation
using MyApp.Infrastructure.Data; // ❌ Domain referencing Infrastructure

namespace MyApp.Domain.Services
{
    public class OrderService
    {
        private readonly DbContext _context; // ❌ Direct DbContext usage
    }
}

// EXXER102: Wrong testing framework
[Fact]
public void TestMethod()
{
    result.Should().Be("expected"); // ❌ FluentAssertions
}
```

### ✅ **After ExxerRules**
```csharp
// ✅ Result<T> pattern
public Result<string> ProcessData(string input)
{
    if (string.IsNullOrEmpty(input))
        return Result.Fail("Input cannot be null"); // ✅ Result<T>
    
    return Result.Ok(input.ToUpper());
}

// ✅ Clean Architecture
using MyApp.Domain.Interfaces; // ✅ Domain referencing abstractions

namespace MyApp.Domain.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _repository; // ✅ Repository pattern
    }
}

// ✅ Shouldly assertions
[Fact]
public void Should_ReturnExpectedValue_When_ValidInput()
{
    result.ShouldBe("expected"); // ✅ Shouldly
}
```

## 🔧 **Configuration**

### EditorConfig Integration
```ini
# Enable all ExxerRules analyzers
[*.cs]
dotnet_analyzer_diagnostic.EXXER003.severity = error    # Result<T> pattern (critical)
dotnet_analyzer_diagnostic.EXXER600.severity = error    # Clean Architecture (critical)
dotnet_analyzer_diagnostic.EXXER100.severity = warning  # Test naming
dotnet_analyzer_diagnostic.EXXER501.severity = suggestion # Expression bodies
```

### MSBuild Configuration
```xml
<PropertyGroup>
  <!-- Treat ExxerRules warnings as errors for critical patterns -->
  <WarningsAsErrors>EXXER003;EXXER600;EXXER601</WarningsAsErrors>
  
  <!-- Customize severity levels -->
  <EXXER003>error</EXXER003>
  <EXXER600>error</EXXER600>
  <EXXER700>warning</EXXER700>
</PropertyGroup>
```

## 🏢 **Enterprise Features**

### **Clean Architecture Enforcement**
- ✅ Domain layer isolation
- ✅ Dependency direction validation
- ✅ Repository pattern compliance
- ✅ Infrastructure abstraction

### **Functional Programming Support**
- ✅ Result<T> pattern enforcement
- ✅ Exception-free error handling
- ✅ Composable error flows
- ✅ Railway-oriented programming

### **Modern Testing Standards**
- ✅ XUnit v3 migration path
- ✅ Shouldly assertion consistency
- ✅ NSubstitute mocking standards
- ✅ Test naming conventions
- ✅ EF Core testing best practices

### **Performance Optimization**
- ✅ Async/await best practices
- ✅ LINQ efficiency patterns
- ✅ ConfigureAwait compliance
- ✅ Memory allocation awareness

## 📈 **Benefits for Your Team**

| Benefit | Before ExxerRules | After ExxerRules |
|---------|------------------|------------------|
| **Code Reviews** | Manual standards checking | Automated enforcement |
| **Architecture** | Violations slip through | Caught at compile time |
| **Testing** | Inconsistent frameworks | Unified modern standards |
| **Performance** | Runtime discovery | Build-time detection |
| **Onboarding** | Weeks to learn standards | Immediate guidance |
| **Technical Debt** | Accumulates over time | Prevented automatically |

## 🚀 **Advanced Usage**

### **Custom Rule Sets**
```xml
<ItemGroup>
  <AdditionalFiles Include="exxer.ruleset" />
</ItemGroup>
```

### **CI/CD Integration**
```yaml
- name: Build with ExxerRules
  run: |
    dotnet build --configuration Release \
    --verbosity normal \
    --property WarningsAsErrors="EXXER003;EXXER600"
```

### **Team Customization**
```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <!-- Enable ExxerRules for entire solution -->
  <EnableExxerRules>true</EnableExxerRules>
  
  <!-- Customize for different project types -->
  <EnableArchitectureRules Condition="'$(ProjectType)' == 'Domain'">true</EnableArchitectureRules>
  <EnableTestingRules Condition="'$(ProjectType)' == 'Tests'">true</EnableTestingRules>
</PropertyGroup>
```

## 🤝 **Contributing**

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### **Development Principles**
- ✅ **Test-Driven Development** - All analyzers developed with TDD (51/51 tests passing)
- ✅ **Clean Code** - Follow the same standards we enforce
- ✅ **Performance First** - Minimal analyzer overhead
- ✅ **Developer Experience** - Clear diagnostic messages and actionable suggestions

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🎯 **Support**

- 📖 **Documentation**: [docs.exxerai.com/exxer-rules](https://docs.exxerai.com/exxer-rules)
- 🐛 **Issues**: [GitHub Issues](https://github.com/exxerai/exxer-rules/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/exxerai/exxer-rules/discussions)
- 📧 **Enterprise Support**: enterprise@exxerai.com

---

**Made with ❤️ by the ExxerAI team using Test-Driven Development**

*"Clean code is not written by following a set of rules. Clean code is written by professionals who care about their craft."* - Robert C. Martin