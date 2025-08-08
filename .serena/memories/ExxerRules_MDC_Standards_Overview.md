# ExxerRules MDC Standards Overview

## Project Overview
- **Project**: ExxerRules - Implementing MDC rules as Roslyn analyzers
- **Tech Stack**: .NET 9/10, XUnit v3, Shouldly, NSubstitute
- **Architecture**: Clean Architecture with Domain-Driven Design

## Core Principles
1. **Result<T> Pattern** - Never throw exceptions, always return Result<T>
2. **Functional Programming** - Use Map, Bind, Match operations
3. **Immutability First** - Prefer readonly, init, and record types
4. **Null Safety** - Comprehensive null validation with functional patterns
5. **Clean Architecture** - Strict layer separation (Domain, Application, Infrastructure)

## Critical Standards

### Testing Standards
- **Framework**: XUnit v3 ONLY with Shouldly assertions and NSubstitute mocking
- **Pattern**: AAA (Arrange-Act-Assert) with naming convention `Should_Action_When_Condition`
- **EF Core Testing**: Use InMemory provider, NEVER mock DbContext
- **Enum Testing**: Use `nameof(Enum.Value)` in InlineData attributes

### Code Quality Standards
- **Naming**: camelCase for fields, descriptive names, no abbreviations
- **Organization**: No regions, prefer sub-classes
- **Constants**: Always define constants for magic numbers/strings
- **Async**: All async methods must accept CancellationToken
- **ConfigureAwait**: Use ConfigureAwait(false) for background operations

### Documentation Standards
- **XML Documentation**: Required for ALL public members
- **Tags**: Proper use of `<summary>`, `<param>`, `<returns>`, `<exception>`
- **Cross-references**: Use `<see cref="">` for type references

### Architecture Standards
- **Repository Pattern**: Focused interfaces following ISP
- **Value Objects**: For domain concepts to avoid primitive obsession
- **Domain Services**: Business logic that doesn't belong to entities
- **Module Organization**: Organize by domain, not by technical type

### Performance Standards
- **Memory**: Use Span<T>, ArrayPool, object pooling
- **LINQ**: Efficient operations with minimal allocations
- **Caching**: ConcurrentDictionary for thread-safe caching
- **Async**: Proper parallel processing where appropriate

### Logging Standards
- **Framework**: Serilog with structured logging
- **Correlation**: Include correlation IDs and context
- **Performance**: Log timing and metrics
- **Levels**: Appropriate log levels (Debug, Info, Warning, Error)

## Forbidden Practices
1. **No Moq** - Use NSubstitute
2. **No FluentAssertions** - Use Shouldly
3. **No MediaTr or AutoMapper**
4. **No Exception-based Control Flow**
5. **No Magic Numbers/Strings**
6. **No _camelCase for private fields**
7. **No Regions in code**

## Roslyn Analyzer Priorities
1. **Result<T> Pattern Enforcement**
2. **Testing Standards Compliance**
3. **Null Safety Validation**
4. **CancellationToken Propagation**
5. **XML Documentation Coverage**
6. **Magic Number/String Detection**
7. **Modern C# Feature Usage**
8. **Architecture Layer Validation**
9. **Performance Pattern Compliance**
10. **Structured Logging Validation**

## Command Execution
- Always use Serena server for file operations
- Wrap shell commands with timeout: `timeout 30s <command> 2>&1 | cat`
- Use refactor agent for C# code transformations
- Handle exit codes and provide proper logging

## Development Workflow
1. Reconnaissance - Read and understand existing code
2. Plan - Create detailed execution plan
3. Context - Gather necessary information
4. Execute - Implement with proper patterns
5. Verify - Test and validate changes
6. Report - Document outcomes with evidence

## Key File Patterns
- `*.mdc` - Rule definition files in .cursor/rules
- Test files: `*Tests.cs`, `*Specs.cs`
- Domain entities in Domain/Entities
- Value objects in Domain/ValueObjects
- Application services in Application/Services