# Roslyn Analyzers Implementation Priority for ExxerRules

## Top 10 Roslyn Analyzers to Implement

### 1. **Result<T> Pattern Enforcement**
- Detect and flag exception throwing
- Enforce functional error handling with Result<T>
- Flag methods that throw exceptions instead of returning Result<T>
- Ensure proper Result<T> usage in async methods

### 2. **Testing Standards Compliance**
- Verify XUnit v3 usage (flag XUnit v2 or other test frameworks)
- Enforce test naming convention: `Should_Action_When_Condition`
- Verify AAA (Arrange-Act-Assert) pattern structure
- Flag tests using Moq or FluentAssertions
- Ensure proper use of Shouldly assertions
- Detect EF Core DbContext mocking (should use InMemory provider)

### 3. **Null Safety Validation**
- Check for proper null parameter validation
- Enforce early null checks in constructors and methods
- Verify use of null safety patterns (IsSuccessMayBeNull, IsSuccessNotNull)
- Flag missing null validation for reference types

### 4. **CancellationToken Propagation**
- Ensure all async methods accept CancellationToken parameter
- Verify CancellationToken is properly propagated to async calls
- Flag async methods missing CancellationToken parameter
- Check for proper default parameter usage (CancellationToken cancellationToken = default)

### 5. **XML Documentation Coverage**
- Verify ALL public classes have XML documentation
- Check ALL public methods have proper <summary>, <param>, <returns>
- Verify ALL public properties have documentation
- Flag missing <exception> tags where exceptions might occur
- Ensure proper use of <see cref=""> for cross-references

### 6. **Magic Number/String Detection**
- Identify and flag hardcoded numeric values
- Detect hardcoded string literals (except empty string)
- Ensure constants are defined for repeated values
- Flag inline magic numbers in calculations
- Verify proper constant naming and organization

### 7. **Modern C# Feature Usage**
- Encourage expression-bodied members where appropriate
- Promote switch expressions over switch statements
- Encourage pattern matching usage
- Flag old-style property getters/setters that could use expression body
- Promote use of 'init' properties for immutability

### 8. **Architecture Layer Validation**
- Verify Clean Architecture boundaries
- Ensure Domain layer doesn't reference Infrastructure
- Check Application layer doesn't reference Infrastructure directly
- Validate proper dependency flow
- Flag circular dependencies between layers

### 9. **Performance Pattern Compliance**
- Check for ConfigureAwait(false) in library/background code
- Detect inefficient LINQ operations (multiple enumerations)
- Flag missing async/await in async methods
- Verify proper use of ValueTask where appropriate
- Check for proper disposal patterns with using statements

### 10. **Structured Logging Validation**
- Ensure structured logging format (no string concatenation)
- Verify proper log levels usage
- Check for correlation ID inclusion
- Flag Console.WriteLine usage in production code
- Ensure proper exception logging patterns

## Key Forbidden Practices to Detect

### Testing Frameworks
- **Moq** → Must use NSubstitute
- **FluentAssertions** → Must use Shouldly
- **MSTest/NUnit** → Must use XUnit v3

### Code Patterns
- **Throwing Exceptions** → Must return Result<T>
- **_camelCase private fields** → Use camelCase without underscore
- **#region blocks** → Use sub-classes for organization
- **Mocking EF Core DbContext** → Use InMemory provider
- **MediaTr usage** → Direct service injection
- **AutoMapper usage** → Manual mapping or dedicated mappers

### Async Patterns
- **Missing CancellationToken** → All async methods must accept it
- **.Result or .Wait()** → Use proper async/await
- **async void** → Only allowed for event handlers
- **Missing ConfigureAwait(false)** → Required for library code

## Implementation Notes

### Severity Levels
- **Error**: Result<T> violations, exception throwing, forbidden frameworks
- **Warning**: Missing documentation, naming violations, performance issues
- **Info**: Modern C# feature suggestions, optimization opportunities

### Code Fix Providers
Each analyzer should have corresponding code fix providers:
- Auto-convert exceptions to Result<T>
- Add missing CancellationToken parameters
- Generate XML documentation templates
- Convert to expression-bodied members
- Replace magic numbers with constants

### Configuration
Analyzers should be configurable via .editorconfig:
- Severity levels
- Excluded files/patterns
- Custom naming conventions
- Performance thresholds

## Priority Justification

1. **Result<T> Pattern** - Core architectural decision affecting entire codebase
2. **Testing Standards** - Ensures consistent, maintainable tests
3. **Null Safety** - Prevents runtime NullReferenceExceptions
4. **CancellationToken** - Critical for responsive async operations
5. **Documentation** - Essential for API usability and maintenance
6. **Magic Numbers** - Improves code readability and maintainability
7. **Modern C#** - Leverages language features for cleaner code
8. **Architecture** - Maintains clean boundaries and dependencies
9. **Performance** - Ensures efficient resource usage
10. **Logging** - Critical for production debugging and monitoring