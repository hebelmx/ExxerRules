using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ExxerRules.Analyzers.Testing;
using ExxerRules.Analyzers.NullSafety;
using ExxerRules.Analyzers.Async;
using ExxerRules.Analyzers.Documentation;
using ExxerRules.Analyzers.CodeQuality;
using ExxerRules.Analyzers.Logging;
using ExxerRules.Analyzers.FunctionalPatterns;
using ExxerRules.Analyzers.Performance;
using ExxerRules.Analyzers.ModernCSharp;
using ExxerRules.Analyzers.Architecture;
using ExxerRules.Analyzers;
using System.Collections.Immutable;
using System.Linq;

namespace ExxerRules.Tests;

/// <summary>
/// Manual test runner to verify analyzer functionality without XUnit framework issues.
/// This follows TDD principles by creating tests we can make pass.
/// </summary>
public static class ManualTestRunner
{
	/// <summary>
	/// Tests that valid test naming convention does not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_TestMethodFollowsNamingConvention()
	{
		// Arrange - Test code that follows naming convention
		const string testCode = @"
using Xunit;

namespace TestProject
{
	public class CalculatorTests
	{
		[Fact]
		public void Should_Add_When_TwoPositiveNumbers()
		{
			// Test implementation
		}

		[Theory]
		public void Should_Subtract_When_FirstNumberIsLarger()
		{
			// Test implementation
		}
	}
}";

		// Act - Run the analyzer
		var diagnostics = RunAnalyzer(testCode, new TestNamingConventionAnalyzer());

		// Assert - No diagnostics should be reported
		return diagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that invalid test naming convention reports diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_TestMethodDoesNotFollowNamingConvention()
	{
		// Arrange - Test code that violates naming convention
		const string testCode = @"
using Xunit;

namespace TestProject
{
	public class CalculatorTests
	{
		[Fact]
		public void AddTest()
		{
			// Test implementation
		}

		[Theory]
		public void TestSubtraction()
		{
			// Test implementation
		}
	}
}";

		// Act - Run the analyzer
		var diagnostics = RunAnalyzer(testCode, new TestNamingConventionAnalyzer());

		// Assert - Two diagnostics should be reported (one for each bad method name)
		return diagnostics.Length == 2 &&
			   diagnostics.All(d => d.Id == DiagnosticIds.TestNamingConvention);
	}

	/// <summary>
	/// Runs a diagnostic analyzer on the given source code.
	/// </summary>
	private static ImmutableArray<Diagnostic> RunAnalyzer(string sourceCode, DiagnosticAnalyzer analyzer)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		var compilation = CSharpCompilation.Create("TestAssembly")
			.AddSyntaxTrees(syntaxTree)
			.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
			.AddReferences(MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location));

		// Add XUnit reference for test attributes
		try
		{
			var xunitAssembly = System.Reflection.Assembly.Load("xunit.core");
			compilation = compilation.AddReferences(MetadataReference.CreateFromFile(xunitAssembly.Location));
		}
		catch
		{
			// If XUnit is not available, create a mock Fact attribute
			const string factAttributeCode = @"
namespace Xunit
{
	public class FactAttribute : System.Attribute { }
	public class TheoryAttribute : System.Attribute { }
}";
			var factSyntaxTree = CSharpSyntaxTree.ParseText(factAttributeCode);
			compilation = compilation.AddSyntaxTrees(factSyntaxTree);
		}

		// Add Microsoft.Extensions.Logging reference for logging tests
		try
		{
			var loggingAssembly = System.Reflection.Assembly.Load("Microsoft.Extensions.Logging.Abstractions");
			compilation = compilation.AddReferences(MetadataReference.CreateFromFile(loggingAssembly.Location));
		}
		catch
		{
			// If Microsoft.Extensions.Logging is not available, create mock interfaces
			const string loggingCode = @"
namespace Microsoft.Extensions.Logging
{
	public interface ILogger
	{
		void LogInformation(string message, params object[] args);
		void LogError(string message, params object[] args);
		void LogWarning(string message, params object[] args);
		void LogDebug(string message, params object[] args);
		void LogTrace(string message, params object[] args);
		void LogCritical(string message, params object[] args);
	}

	public interface ILogger<T> : ILogger
	{
	}
}";
			var loggingSyntaxTree = CSharpSyntaxTree.ParseText(loggingCode);
			compilation = compilation.AddSyntaxTrees(loggingSyntaxTree);
		}

		var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
		var result = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

		return result;
	}

	/// <summary>
	/// Tests that null parameter validation is reported when missing.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_MethodLacksNullParameterValidation()
	{
		// Arrange - Method that takes reference parameters but doesn't validate them
		const string testCode = @"
namespace TestProject
{
	public class Calculator
	{
		public Result<int> ProcessData(string input, object data)
		{
			// Missing null parameter validation
			return Result.Ok(input.Length + data.GetHashCode());
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());

		// Assert - Should report diagnostic for missing null validation
		return diagnostics.Length == 1 &&
			   diagnostics[0].Id == DiagnosticIds.ValidateNullParameters;
	}

	/// <summary>
	/// Tests that proper null parameter validation does not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_MethodHasNullParameterValidation()
	{
		// Arrange - Method that properly validates null parameters
		const string testCode = @"
using FluentResults;

namespace TestProject
{
	public class Calculator
	{
		public Result<int> ProcessData(string input, object data)
		{
			if (input == null) return Result.Fail(""input cannot be null"");
			if (data == null) return Result.Fail(""data cannot be null"");

			return Result.Ok(input.Length + data.GetHashCode());
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());

		// Assert - No diagnostics should be reported
		return diagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that methods with no reference parameters don't report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_MethodHasNoReferenceParameters()
	{
		// Arrange - Method with only value type parameters
		const string testCode = @"
namespace TestProject
{
	public class Calculator
	{
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());

		// Assert - No diagnostics should be reported
		return diagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that async methods without CancellationToken report diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_AsyncMethodLacksCancellationToken()
	{
		// Arrange - Async method without CancellationToken
		const string testCode = @"
using System.Threading.Tasks;

namespace TestProject
{
	public class DataService
	{
		public async Task<string> LoadDataAsync()
		{
			await Task.Delay(1000);
			return ""data"";
		}

		public async Task<int> ProcessAsync(string input)
		{
			await Task.Delay(500);
			return input.Length;
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());

		// Assert - Should report diagnostic for missing CancellationToken
		return diagnostics.Length == 2 &&
			   diagnostics.All(d => d.Id == DiagnosticIds.AsyncMethodsShouldAcceptCancellationToken);
	}

	/// <summary>
	/// Tests that async methods with CancellationToken do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_AsyncMethodHasCancellationToken()
	{
		// Arrange - Async method with proper CancellationToken
		const string testCode = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
	public class DataService
	{
		public async Task<string> LoadDataAsync(CancellationToken cancellationToken = default)
		{
			await Task.Delay(1000, cancellationToken);
			return ""data"";
		}

		public async Task ProcessAsync(string input, CancellationToken cancellationToken)
		{
			await Task.Delay(500, cancellationToken);
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());

		// Assert - No diagnostics should be reported
		return diagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that non-async methods do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_MethodIsNotAsyncNamed()
	{
		// Arrange - Non-async method
		const string testCode = @"
namespace TestProject
{
	public class Calculator
	{
		public int Add(int a, int b)
		{
			return a + b;
		}

		public string ProcessData(string input)
		{
			return input.ToUpper();
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());

		// Assert - No diagnostics should be reported
		return diagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that async void methods (event handlers) do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_AsyncVoidEventHandler()
	{
		// Arrange - Async void event handler
		const string testCode = @"
using System.Threading.Tasks;

namespace TestProject
{
	public class EventHandlers
	{
		public async void Button_Click(object sender, EventArgs e)
		{
			await Task.Delay(100);
		}

		private async void OnDataReceived()
		{
			await ProcessDataAsync();
		}

		private async Task ProcessDataAsync()
		{
			await Task.Delay(50);
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());

		// Assert - Should only report diagnostic for ProcessDataAsync, not event handlers
		return diagnostics.Length == 1 &&
			   diagnostics[0].Id == DiagnosticIds.AsyncMethodsShouldAcceptCancellationToken;
	}

	/// <summary>
	/// Tests that public members without XML documentation report diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_PublicMemberLacksXmlDocumentation()
	{
		// Arrange - Public class and method without XML documentation
		const string testCode = @"
namespace TestProject
{
	public class Calculator
	{
		public int Add(int a, int b)
		{
			return a + b;
		}

		public string Format(string value)
		{
			return value.ToUpper();
		}
	}
}";

		// Act - Run the analyzer
		var diagnostics = RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());

		// Filter out mock XUnit diagnostics to focus on our test code
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for missing XML documentation
		return relevantDiagnostics.Length == 3 && // class + 2 methods
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.PublicMembersShouldHaveXmlDocumentation);
	}

	/// <summary>
	/// Tests that public members with XML documentation do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_PublicMemberHasXmlDocumentation()
	{
		// Arrange - Public class and method with proper XML documentation
		const string testCode = @"
namespace TestProject
{
	/// <summary>
	/// Calculator for mathematical operations.
	/// </summary>
	public class Calculator
	{
		/// <summary>
		/// Adds two integers.
		/// </summary>
		/// <param name=""a"">First number.</param>
		/// <param name=""b"">Second number.</param>
		/// <returns>Sum of the two numbers.</returns>
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}";

		// Act - Run the analyzer
		var diagnostics = RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());

		// Filter out mock XUnit diagnostics to focus on our test code
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for properly documented members
		return relevantDiagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that non-public members do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_MemberIsNotPublic()
	{
		// Arrange - Private/internal members without XML documentation
		const string testCode = @"
namespace TestProject
{
	internal class InternalCalculator
	{
		private int PrivateAdd(int a, int b)
		{
			return a + b;
		}

		protected int ProtectedAdd(int a, int b)
		{
			return a + b;
		}

		internal int InternalAdd(int a, int b)
		{
			return a + b;
		}
	}
}";

		// Act - Run the analyzer
		var diagnostics = RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());

		// Filter out mock XUnit diagnostics to focus on our test code
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for non-public members
		return relevantDiagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that public properties without XML documentation report diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_PublicPropertyLacksXmlDocumentation()
	{
		// Arrange - Public property without XML documentation
		const string testCode = @"
namespace TestProject
{
	public class Person
	{
		public string Name { get; set; }
		public int Age { get; set; }
		public string Email { get; private set; }
	}
}";

		// Act - Run the analyzer
		var diagnostics = RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());

		// Filter out mock XUnit diagnostics to focus on our test code
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for class + 3 properties
		return relevantDiagnostics.Length == 4 &&
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.PublicMembersShouldHaveXmlDocumentation);
	}

	/// <summary>
	/// Debug helper to understand XML documentation detection issues.
	/// </summary>
	public static void DebugXmlDocumentationAnalyzer()
	{
		Console.WriteLine("=== XML Documentation Analyzer Debug ===");
		Console.WriteLine();

		// Test 1: Simple public class without XML doc
		const string testCode1 = @"
namespace TestProject
{
	public class Calculator
	{
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}";

		var diagnostics1 = RunAnalyzer(testCode1, new PublicMembersShouldHaveXmlDocumentationAnalyzer());
		Console.WriteLine($"Test 1 - Public class without XML doc: {diagnostics1.Length} diagnostics");
		foreach (var diag in diagnostics1)
		{
			Console.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
		}
		Console.WriteLine();

		// Test 2: Simple public class with XML doc
		const string testCode2 = @"
namespace TestProject
{
	/// <summary>
	/// Calculator class.
	/// </summary>
	public class Calculator
	{
		/// <summary>
		/// Adds two numbers.
		/// </summary>
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}";

		var diagnostics2 = RunAnalyzer(testCode2, new PublicMembersShouldHaveXmlDocumentationAnalyzer());
		Console.WriteLine($"Test 2 - Public class with XML doc: {diagnostics2.Length} diagnostics");
		foreach (var diag in diagnostics2)
		{
			Console.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
		}
		Console.WriteLine();
	}

	/// <summary>
	/// Runs all manual tests and reports results.
	/// </summary>
	public static void RunAllTests()
	{
		Console.WriteLine("=== Manual Test Runner - TDD Approach ===");
		Console.WriteLine();

		var tests = new[]
		{
			// Test Naming Convention Tests
			("Should_NotReportDiagnostic_When_TestMethodFollowsNamingConvention",
			 (Func<bool>)Should_NotReportDiagnostic_When_TestMethodFollowsNamingConvention),
			("Should_ReportDiagnostic_When_TestMethodDoesNotFollowNamingConvention",
			 (Func<bool>)Should_ReportDiagnostic_When_TestMethodDoesNotFollowNamingConvention),

			// Null Safety Tests
			("Should_ReportDiagnostic_When_MethodLacksNullParameterValidation",
			 (Func<bool>)Should_ReportDiagnostic_When_MethodLacksNullParameterValidation),
			("Should_NotReportDiagnostic_When_MethodHasNullParameterValidation",
			 (Func<bool>)Should_NotReportDiagnostic_When_MethodHasNullParameterValidation),
			("Should_NotReportDiagnostic_When_MethodHasNoReferenceParameters",
			 (Func<bool>)Should_NotReportDiagnostic_When_MethodHasNoReferenceParameters),

			// Async CancellationToken Tests
			("Should_ReportDiagnostic_When_AsyncMethodLacksCancellationToken",
			 (Func<bool>)Should_ReportDiagnostic_When_AsyncMethodLacksCancellationToken),
			("Should_NotReportDiagnostic_When_AsyncMethodHasCancellationToken",
			 (Func<bool>)Should_NotReportDiagnostic_When_AsyncMethodHasCancellationToken),
			("Should_NotReportDiagnostic_When_MethodIsNotAsync",
			 (Func<bool>)Should_NotReportDiagnostic_When_MethodIsNotAsyncNamed),
			("Should_NotReportDiagnostic_When_AsyncVoidEventHandler",
			 (Func<bool>)Should_NotReportDiagnostic_When_AsyncVoidEventHandler),

			// XML Documentation Tests
			("Should_ReportDiagnostic_When_PublicMemberLacksXmlDocumentation",
			 (Func<bool>)Should_ReportDiagnostic_When_PublicMemberLacksXmlDocumentation),
			("Should_NotReportDiagnostic_When_PublicMemberHasXmlDocumentation",
			 (Func<bool>)Should_NotReportDiagnostic_When_PublicMemberHasXmlDocumentation),
			("Should_NotReportDiagnostic_When_MemberIsNotPublic",
			 (Func<bool>)Should_NotReportDiagnostic_When_MemberIsNotPublic),
			("Should_ReportDiagnostic_When_PublicPropertyLacksXmlDocumentation",
			 (Func<bool>)Should_ReportDiagnostic_When_PublicPropertyLacksXmlDocumentation),

			// Magic Numbers and Strings Tests
			("Should_ReportDiagnostic_When_MagicNumbersAreUsed",
			 (Func<bool>)Should_ReportDiagnostic_When_MagicNumbersAreUsed),
			("Should_ReportDiagnostic_When_MagicStringsAreUsed",
			 (Func<bool>)Should_ReportDiagnostic_When_MagicStringsAreUsed),
			("Should_NotReportDiagnostic_When_NamedConstantsAreUsed",
			 (Func<bool>)Should_NotReportDiagnostic_When_NamedConstantsAreUsed),
			("Should_NotReportDiagnostic_When_CommonNumbersAreUsed",
			 (Func<bool>)Should_NotReportDiagnostic_When_CommonNumbersAreUsed),

			// Regions Tests
			("Should_ReportDiagnostic_When_RegionsAreUsed",
			 (Func<bool>)Should_ReportDiagnostic_When_RegionsAreUsed),
			("Should_NotReportDiagnostic_When_NoRegionsAreUsed",
			 (Func<bool>)Should_NotReportDiagnostic_When_NoRegionsAreUsed),

			// Structured Logging Tests
			("Should_ReportDiagnostic_When_StringConcatenationIsUsedInLogging",
			 (Func<bool>)Should_ReportDiagnostic_When_StringConcatenationIsUsedInLogging),
			("Should_NotReportDiagnostic_When_StructuredLoggingIsUsed",
			 (Func<bool>)Should_NotReportDiagnostic_When_StructuredLoggingIsUsed),

			// Console.WriteLine Tests
			("Should_ReportDiagnostic_When_ConsoleWriteLineIsUsed",
			 (Func<bool>)Should_ReportDiagnostic_When_ConsoleWriteLineIsUsed),
			("Should_NotReportDiagnostic_When_ProperLoggingIsUsedInsteadOfConsole",
			 (Func<bool>)Should_NotReportDiagnostic_When_ProperLoggingIsUsedInsteadOfConsole),

			// Result<T> Pattern Enforcement Tests (EXXER003)
			("Should_ReportDiagnostic_When_ThrowStatementIsUsed",
			 (Func<bool>)Should_ReportDiagnostic_When_ThrowStatementIsUsed),
			("Should_NotReportDiagnostic_When_ReturningResult",
			 (Func<bool>)Should_NotReportDiagnostic_When_ReturningResult),
			("Should_ReportDiagnostic_When_ThrowingInsteadOfReturningResult",
			 (Func<bool>)Should_ReportDiagnostic_When_ThrowingInsteadOfReturningResult),
			("Should_NotReportDiagnostic_When_UsingResultPattern",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingResultPattern),

			// Testing Standards Compliance Tests (EXXER600-699)
			("Should_ReportDiagnostic_When_UsingMoq",
			 (Func<bool>)Should_ReportDiagnostic_When_UsingMoq),
			("Should_NotReportDiagnostic_When_UsingNSubstitute",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingNSubstitute),
			("Should_ReportDiagnostic_When_UsingFluentAssertions",
			 (Func<bool>)Should_ReportDiagnostic_When_UsingFluentAssertions),
			("Should_NotReportDiagnostic_When_UsingShouldly",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingShouldly),

			// Additional Testing Standards Tests (XUnit v3, DbContext)
			("Should_ReportDiagnostic_When_UsingXUnitV2",
			 (Func<bool>)Should_ReportDiagnostic_When_UsingXUnitV2),
			("Should_NotReportDiagnostic_When_UsingXUnitV3",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingXUnitV3),
			("Should_ReportDiagnostic_When_MockingDbContext",
			 (Func<bool>)Should_ReportDiagnostic_When_MockingDbContext),
			("Should_NotReportDiagnostic_When_UsingInMemoryDbContext",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingInMemoryDbContext),

			// Performance Pattern Tests (ConfigureAwait, LINQ)
			("Should_ReportDiagnostic_When_AwaitWithoutConfigureAwait",
			 (Func<bool>)Should_ReportDiagnostic_When_AwaitWithoutConfigureAwait),
			("Should_NotReportDiagnostic_When_AwaitWithConfigureAwait",
			 (Func<bool>)Should_NotReportDiagnostic_When_AwaitWithConfigureAwait),
			("Should_ReportDiagnostic_When_UsingInEfficientLinq",
			 (Func<bool>)Should_ReportDiagnostic_When_UsingInEfficientLinq),
			("Should_NotReportDiagnostic_When_UsingEfficientLinq",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingEfficientLinq),

			// Modern C# Feature Tests (EXXER501, EXXER702)
			("Should_ReportDiagnostic_When_MethodCanUseExpressionBody",
			 (Func<bool>)Should_ReportDiagnostic_When_MethodCanUseExpressionBody),
			("Should_NotReportDiagnostic_When_MethodAlreadyUsesExpressionBody",
			 (Func<bool>)Should_NotReportDiagnostic_When_MethodAlreadyUsesExpressionBody),
			("Should_ReportDiagnostic_When_PropertyCanUseExpressionBody",
			 (Func<bool>)Should_ReportDiagnostic_When_PropertyCanUseExpressionBody),
			("Should_NotReportDiagnostic_When_PropertyAlreadyUsesExpressionBody",
			 (Func<bool>)Should_NotReportDiagnostic_When_PropertyAlreadyUsesExpressionBody),
			("Should_ReportDiagnostic_When_UsingOldPatternMatching",
			 (Func<bool>)Should_ReportDiagnostic_When_UsingOldPatternMatching),
			("Should_NotReportDiagnostic_When_UsingModernPatternMatching",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingModernPatternMatching),

			// Architecture Layer Validation Tests (EXXER600-699)
			("Should_ReportDiagnostic_When_DomainReferencesInfrastructure",
			 (Func<bool>)Should_ReportDiagnostic_When_DomainReferencesInfrastructure),
			("Should_NotReportDiagnostic_When_DomainReferencesOnlyAllowedLayers",
			 (Func<bool>)Should_NotReportDiagnostic_When_DomainReferencesOnlyAllowedLayers),
			("Should_ReportDiagnostic_When_NotUsingRepositoryPattern",
			 (Func<bool>)Should_ReportDiagnostic_When_NotUsingRepositoryPattern),
			("Should_NotReportDiagnostic_When_UsingRepositoryPattern",
			 (Func<bool>)Should_NotReportDiagnostic_When_UsingRepositoryPattern),
			("Should_ReportDiagnostic_When_RepositoryLacksInterface",
			 (Func<bool>)Should_ReportDiagnostic_When_RepositoryLacksInterface),
			("Should_NotReportDiagnostic_When_RepositoryHasInterface",
			 (Func<bool>)Should_NotReportDiagnostic_When_RepositoryHasInterface)
		};

		var passed = 0;
		var total = tests.Length;

		foreach (var (testName, testMethod) in tests)
		{
			try
			{
				var result = testMethod();
				var status = result ? "PASS" : "FAIL";
				Console.WriteLine($"[{status}] {testName}");

				if (result)
				{
					passed++;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] {testName}: {ex.Message}");
			}
		}

		Console.WriteLine();
		Console.WriteLine($"Results: {passed}/{total} tests passed");
		Console.WriteLine($"Success Rate: {(passed * 100.0 / total):F1}%");
	}

	/// <summary>
	/// Tests that magic numbers report diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_MagicNumbersAreUsed()
	{
		// Arrange - Code with magic numbers
		const string testCode = @"
namespace TestProject
{
	public class OrderValidator
	{
		public bool ValidateOrder(Order order)
		{
			if (order.Items.Count > 100) // Magic number
				return false;

			if (order.TotalAmount < 10.00m) // Magic number
				return false;

			return order.DiscountRate <= 0.85m; // Magic number
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for magic numbers
		return relevantDiagnostics.Length == 3 &&
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.AvoidMagicNumbersAndStrings);
	}

	/// <summary>
	/// Tests that magic strings report diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_MagicStringsAreUsed()
	{
		// Arrange - Code with magic strings
		const string testCode = @"
namespace TestProject
{
	public class OrderProcessor
	{
		public string ProcessOrder(Order order)
		{
			if (order.Status == ""Pending"") // Magic string
				return ""Order is pending""; // Magic string

			if (order.Currency == ""USD"") // Magic string
				return ""Order in dollars""; // Magic string

			return ""Unknown status""; // Magic string
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for magic strings
		return relevantDiagnostics.Length == 5 &&
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.AvoidMagicNumbersAndStrings);
	}

	/// <summary>
	/// Tests that named constants do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_NamedConstantsAreUsed()
	{
		// Arrange - Code using named constants
		const string testCode = @"
namespace TestProject
{
	public static class OrderConstants
	{
		public const int MaxOrderItems = 100;
		public const decimal MinimumOrderAmount = 10.00m;
		public const decimal WorldClassOeeThreshold = 0.85m;
		public const string PendingStatus = ""Pending"";
		public const string UsdCurrency = ""USD"";
	}

	public class OrderValidator
	{
		public bool ValidateOrder(Order order)
		{
			if (order.Items.Count > OrderConstants.MaxOrderItems)
				return false;

			if (order.TotalAmount < OrderConstants.MinimumOrderAmount)
				return false;

			return order.DiscountRate <= OrderConstants.WorldClassOeeThreshold;
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for named constants
		return relevantDiagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that common numbers (0, 1, -1, etc.) do not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_CommonNumbersAreUsed()
	{
		// Arrange - Code using common numbers
		const string testCode = @"
namespace TestProject
{
	public class Calculator
	{
		public int ProcessArray(int[] values)
		{
			if (values.Length == 0) // Common number - array empty
				return -1; // Common number - error indicator

			var sum = 0; // Common number - initialization
			for (int i = 0; i < values.Length; i++) // Common numbers - loop
			{
				sum += values[i];
			}

			return sum / values.Length;
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for common numbers
		return relevantDiagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that regions report diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_RegionsAreUsed()
	{
		// Arrange - Code with regions
		const string testCode = @"
namespace TestProject
{
	public class OrderProcessor
	{
		#region Validation Methods

		public bool ValidateCustomer(Customer customer)
		{
			return customer != null;
		}

		public bool ValidateItems(List<Item> items)
		{
			return items?.Count > 0;
		}

		#endregion Validation Methods

		#region Processing Methods

		public Result<Order> ProcessOrder(Order order)
		{
			return Result.Ok(order);
		}

		#endregion Processing Methods
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new DoNotUseRegionsAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for each #region directive
		return relevantDiagnostics.Length == 2 &&
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.DoNotUseRegions);
	}

	/// <summary>
	/// Tests that code without regions does not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_NoRegionsAreUsed()
	{
		// Arrange - Code without regions using good organization
		const string testCode = @"
namespace TestProject
{
	public class OrderValidator
	{
		public bool ValidateCustomer(Customer customer)
		{
			return customer != null;
		}

		public bool ValidateItems(List<Item> items)
		{
			return items?.Count > 0;
		}
	}

	public class OrderProcessor
	{
		private readonly OrderValidator _validator;

		public OrderProcessor(OrderValidator validator)
		{
			_validator = validator;
		}

		public Result<Order> ProcessOrder(Order order)
		{
			return Result.Ok(order);
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new DoNotUseRegionsAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for well-organized code
		return relevantDiagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that string concatenation in logging reports diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_StringConcatenationIsUsedInLogging()
	{
		// Arrange - Code with string concatenation in logging
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class OrderService
	{
		private readonly ILogger<OrderService> _logger;

		public OrderService(ILogger<OrderService> logger)
		{
			_logger = logger;
		}

		public void ProcessOrder(Order order)
		{
			_logger.LogInformation(""Processing order: "" + order.Id); // String concatenation

			if (order.Amount < 0)
			{
				_logger.LogError(""Invalid amount: "" + order.Amount); // String concatenation
			}

			_logger.LogInformation($""Order {order.Id} processed successfully""); // String interpolation
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new UseStructuredLoggingAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for string concatenation/interpolation in logging
		return relevantDiagnostics.Length == 3 &&
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.UseStructuredLogging);
	}

	/// <summary>
	/// Tests that structured logging does not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_StructuredLoggingIsUsed()
	{
		// Arrange - Code using structured logging correctly
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class OrderService
	{
		private readonly ILogger<OrderService> _logger;

		public OrderService(ILogger<OrderService> logger)
		{
			_logger = logger;
		}

		public void ProcessOrder(Order order)
		{
			_logger.LogInformation(""Processing order: {OrderId}"", order.Id); // Structured

			if (order.Amount < 0)
			{
				_logger.LogError(""Invalid amount: {Amount} for order {OrderId}"", order.Amount, order.Id); // Structured
			}

			_logger.LogInformation(""Order {OrderId} processed successfully with amount {Amount}"",
				order.Id, order.Amount); // Structured
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new UseStructuredLoggingAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for structured logging
		return relevantDiagnostics.Length == 0;
	}

	/// <summary>
	/// Tests that Console.WriteLine usage reports diagnostic.
	/// </summary>
	public static bool Should_ReportDiagnostic_When_ConsoleWriteLineIsUsed()
	{
		// Arrange - Code using Console.WriteLine in production code
		const string testCode = @"
using System;

namespace TestProject
{
	public class OrderService
	{
		public void ProcessOrder(Order order)
		{
			Console.WriteLine(""Processing order: "" + order.Id); // Console.WriteLine

			if (order.Amount < 0)
			{
				Console.WriteLine(""Invalid amount: "" + order.Amount); // Console.WriteLine
				Console.Write(""Error detected""); // Console.Write
			}

			Console.WriteLine($""Order {order.Id} processed""); // Console.WriteLine with interpolation
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new DoNotUseConsoleWriteLineAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - Should report diagnostic for Console.WriteLine and Console.Write usage
		return relevantDiagnostics.Length == 4 &&
			   relevantDiagnostics.All(d => d.Id == DiagnosticIds.DoNotUseConsoleWriteLine);
	}

	/// <summary>
	/// Tests that proper logging does not report diagnostic.
	/// </summary>
	public static bool Should_NotReportDiagnostic_When_ProperLoggingIsUsedInsteadOfConsole()
	{
		// Arrange - Code using proper logging instead of Console.WriteLine
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class OrderService
	{
		private readonly ILogger<OrderService> _logger;

		public OrderService(ILogger<OrderService> logger)
		{
			_logger = logger;
		}

		public void ProcessOrder(Order order)
		{
			_logger.LogInformation(""Processing order: {OrderId}"", order.Id); // Proper logging

			if (order.Amount < 0)
			{
				_logger.LogError(""Invalid amount: {Amount} for order {OrderId}"", order.Amount, order.Id);
			}

			_logger.LogInformation(""Order {OrderId} processed successfully"", order.Id);
		}
	}
}";

		// Act - Run the analyzer (to be implemented)
		var diagnostics = RunAnalyzer(testCode, new DoNotUseConsoleWriteLineAnalyzer());

		// Filter out mock XUnit diagnostics
		var relevantDiagnostics = diagnostics.Where(d =>
			!d.GetMessage().Contains("FactAttribute") &&
			!d.GetMessage().Contains("TheoryAttribute")).ToArray();

		// Assert - No diagnostics should be reported for proper logging
		return relevantDiagnostics.Length == 0;
	}

	// Result<T> Pattern Enforcement Tests (EXXER100)
	private static bool Should_ReportDiagnostic_When_ThrowStatementIsUsed()
	{
		const string testCode = @"
using System;

public class TestClass
{
	public void TestMethod()
	{
		throw new ArgumentException(""This should be flagged"");
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.DoNotThrowExceptions;
	}

	private static bool Should_NotReportDiagnostic_When_ReturningResult()
	{
		const string testCode = @"
using FluentResults;

public class TestClass
{
	public Result TestMethod()
	{
		return Result.Fail(""Error occurred"");
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_ThrowingInsteadOfReturningResult()
	{
		const string testCode = @"
using System;

public class TestClass
{
	public string ProcessData(string input)
	{
		if (string.IsNullOrEmpty(input))
			throw new ArgumentNullException(nameof(input));

		return input.ToUpper();
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.DoNotThrowExceptions;
	}

	private static bool Should_NotReportDiagnostic_When_UsingResultPattern()
	{
		const string testCode = @"
using FluentResults;

public class TestClass
{
	public Result<string> ProcessData(string input)
	{
		if (string.IsNullOrEmpty(input))
			return Result.Fail<string>(""Input cannot be null or empty"");

		return Result.Ok(input.ToUpper());
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());

		return diagnostics.Length == 0;
	}

	// Testing Standards Compliance Tests (EXXER600-699)
	private static bool Should_ReportDiagnostic_When_UsingMoq()
	{
		const string testCode = @"
using Moq;
using System;

public class TestClass
{
	public void TestMethod()
	{
		var mock = new Mock<IService>();
		mock.Setup(x => x.DoSomething()).Returns(""test"");
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotUseMoqAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.UseNSubstitute;
	}

	private static bool Should_NotReportDiagnostic_When_UsingNSubstitute()
	{
		const string testCode = @"
using NSubstitute;

public class TestClass
{
	public void TestMethod()
	{
		var substitute = Substitute.For<IService>();
		substitute.DoSomething().Returns(""test"");
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotUseMoqAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_UsingFluentAssertions()
	{
		const string testCode = @"
using FluentAssertions;

public class TestClass
{
	public void TestMethod()
	{
		var result = ""test"";
		result.Should().Be(""test"");
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotUseFluentAssertionsAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.UseShouldly;
	}

	private static bool Should_NotReportDiagnostic_When_UsingShouldly()
	{
		const string testCode = @"
using Shouldly;

public class TestClass
{
	public void TestMethod()
	{
		var result = ""test"";
		result.ShouldBe(""test"");
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotUseFluentAssertionsAnalyzer());

		return diagnostics.Length == 0;
	}

	// Additional Testing Standards Tests (EXXER600-699)
	private static bool Should_ReportDiagnostic_When_UsingXUnitV2()
	{
		const string testCode = @"
using Xunit;

public class TestClass
{
	[Fact]
	public void TestMethod()
	{
		Assert.True(true);
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseXUnitV3Analyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.UseXUnitV3;
	}

	private static bool Should_NotReportDiagnostic_When_UsingXUnitV3()
	{
		const string testCode = @"
using Xunit.v3;

public class TestClass
{
	[Fact]
	public void TestMethod()
	{
		Assert.True(true);
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseXUnitV3Analyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_MockingDbContext()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;
using Moq;

public class TestClass
{
	public void TestMethod()
	{
		var mockContext = new Mock<DbContext>();
		mockContext.Setup(x => x.SaveChanges()).Returns(1);
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotMockDbContextAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.DoNotMockDbContext;
	}

	private static bool Should_NotReportDiagnostic_When_UsingInMemoryDbContext()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;

public class TestClass
{
	public void TestMethod()
	{
		var options = new DbContextOptionsBuilder<MyContext>()
			.UseInMemoryDatabase(""TestDb"")
			.Options;
		var context = new MyContext(options);
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DoNotMockDbContextAnalyzer());

		return diagnostics.Length == 0;
	}

	// Performance Pattern Tests (EXXER1000-1099)
	private static bool Should_ReportDiagnostic_When_AwaitWithoutConfigureAwait()
	{
		const string testCode = @"
using System.Threading.Tasks;

public class AsyncService
{
	public async Task ProcessDataAsync()
	{
		await SomeAsyncMethod(); // Missing ConfigureAwait(false)
		await AnotherAsyncMethod(); // Missing ConfigureAwait(false)
	}

	private Task SomeAsyncMethod() => Task.CompletedTask;
	private Task AnotherAsyncMethod() => Task.CompletedTask;
}";

		var diagnostics = RunAnalyzer(testCode, new UseConfigureAwaitFalseAnalyzer());

		return diagnostics.Length == 2 && diagnostics.All(d => d.Id == DiagnosticIds.UseConfigureAwaitFalse);
	}

	private static bool Should_NotReportDiagnostic_When_AwaitWithConfigureAwait()
	{
		const string testCode = @"
using System.Threading.Tasks;

public class AsyncService
{
	public async Task ProcessDataAsync()
	{
		await SomeAsyncMethod().ConfigureAwait(false);
		await AnotherAsyncMethod().ConfigureAwait(false);
	}

	private Task SomeAsyncMethod() => Task.CompletedTask;
	private Task AnotherAsyncMethod() => Task.CompletedTask;
}";

		var diagnostics = RunAnalyzer(testCode, new UseConfigureAwaitFalseAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_UsingInEfficientLinq()
	{
		const string testCode = @"
using System.Linq;
using System.Collections.Generic;

public class DataProcessor
{
	public bool ProcessData(IEnumerable<int> data)
	{
		// Inefficient LINQ - multiple enumerations
		return data.Any() && data.First() > 0;
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseEfficientLinqAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.UseEfficientLinq;
	}

	private static bool Should_NotReportDiagnostic_When_UsingEfficientLinq()
	{
		const string testCode = @"
using System.Linq;
using System.Collections.Generic;

public class DataProcessor
{
	public bool ProcessData(IEnumerable<int> data)
	{
		// Efficient LINQ - single enumeration
		var firstItem = data.FirstOrDefault();
		return firstItem > 0;
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseEfficientLinqAnalyzer());

		return diagnostics.Length == 0;
	}

	// Modern C# Feature Tests (EXXER501, EXXER702)
	private static bool Should_ReportDiagnostic_When_MethodCanUseExpressionBody()
	{
		const string testCode = @"
public class Calculator
{
	public int Add(int a, int b)
	{
		return a + b; // Can be expression-bodied
	}

	public string GetMessage()
	{
		return ""Hello World""; // Can be expression-bodied
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());

		return diagnostics.Length == 2 && diagnostics.All(d => d.Id == DiagnosticIds.UseExpressionBodiedMembers);
	}

	private static bool Should_NotReportDiagnostic_When_MethodAlreadyUsesExpressionBody()
	{
		const string testCode = @"
public class Calculator
{
	public int Add(int a, int b) => a + b;
	public string GetMessage() => ""Hello World"";
}";

		var diagnostics = RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_PropertyCanUseExpressionBody()
	{
		const string testCode = @"
public class Person
{
	private string _name = ""Test"";

	public string Name
	{
		get { return _name; } // Can be expression-bodied
	}

	public string FullName
	{
		get { return $""{_name} Lastname""; } // Can be expression-bodied
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());

		return diagnostics.Length == 2 && diagnostics.All(d => d.Id == DiagnosticIds.UseExpressionBodiedMembers);
	}

	private static bool Should_NotReportDiagnostic_When_PropertyAlreadyUsesExpressionBody()
	{
		const string testCode = @"
public class Person
{
	private string _name = ""Test"";

	public string Name => _name;
	public string FullName => $""{_name} Lastname"";
}";

		var diagnostics = RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_UsingOldPatternMatching()
	{
		const string testCode = @"
public class PatternMatcher
{
	public string ProcessValue(object value)
	{
		if (value is string)
		{
			return ((string)value).ToUpper(); // Old-style cast after is check
		}

		if (value is int)
		{
			return ((int)value).ToString(); // Old-style cast after is check
		}

		return ""Unknown"";
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseModernPatternMatchingAnalyzer());

		return diagnostics.Length == 2 && diagnostics.All(d => d.Id == DiagnosticIds.UseModernPatternMatching);
	}

	private static bool Should_NotReportDiagnostic_When_UsingModernPatternMatching()
	{
		const string testCode = @"
public class PatternMatcher
{
	public string ProcessValue(object value)
	{
		if (value is string str)
		{
			return str.ToUpper(); // Modern pattern matching with declaration
		}

		if (value is int number)
		{
			return number.ToString(); // Modern pattern matching with declaration
		}

		return ""Unknown"";
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseModernPatternMatchingAnalyzer());

		return diagnostics.Length == 0;
	}

	// Architecture Layer Validation Tests (EXXER600-699)
	private static bool Should_ReportDiagnostic_When_DomainReferencesInfrastructure()
	{
		const string testCode = @"
using System;
using MyApp.Infrastructure.Data; // Domain should not reference Infrastructure
using MyApp.Infrastructure.Email; // Domain should not reference Infrastructure

namespace MyApp.Domain.Entities
{
	public class Order
	{
		private readonly IDbContext _context; // Direct infrastructure dependency

		public Order(IDbContext context)
		{
			_context = context;
		}
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DomainShouldNotReferenceInfrastructureAnalyzer());

		return diagnostics.Length == 2 && diagnostics.All(d => d.Id == DiagnosticIds.DomainShouldNotReferenceInfrastructure);
	}

	private static bool Should_NotReportDiagnostic_When_DomainReferencesOnlyAllowedLayers()
	{
		const string testCode = @"
using System;
using MyApp.Domain.ValueObjects; // Domain can reference Domain
using MyApp.Domain.Common; // Domain can reference Domain

namespace MyApp.Domain.Entities
{
	public class Order
	{
		public OrderStatus Status { get; set; }
		public Money Total { get; set; } // Value object reference is allowed
	}
}";

		var diagnostics = RunAnalyzer(testCode, new DomainShouldNotReferenceInfrastructureAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_NotUsingRepositoryPattern()
	{
		const string testCode = @"
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Application.Services
{
	public class OrderService
	{
		private readonly DbContext _context;

		public OrderService(DbContext context)
		{
			_context = context;
		}

		public List<Order> GetActiveOrders()
		{
			return _context.Set<Order>().Where(o => o.IsActive).ToList(); // Direct DbContext usage
		}
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.UseRepositoryPattern;
	}

	private static bool Should_NotReportDiagnostic_When_UsingRepositoryPattern()
	{
		const string testCode = @"
using System.Collections.Generic;

namespace MyApp.Application.Services
{
	public class OrderService
	{
		private readonly IOrderRepository _orderRepository;

		public OrderService(IOrderRepository orderRepository)
		{
			_orderRepository = orderRepository;
		}

		public List<Order> GetActiveOrders()
		{
			return _orderRepository.GetActiveOrders(); // Using repository pattern
		}
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());

		return diagnostics.Length == 0;
	}

	private static bool Should_ReportDiagnostic_When_RepositoryLacksInterface()
	{
		const string testCode = @"
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Infrastructure.Data
{
	public class OrderRepository // Missing interface
	{
		private readonly DbContext _context;

		public OrderRepository(DbContext context)
		{
			_context = context;
		}

		public List<Order> GetAll()
		{
			return _context.Set<Order>().ToList();
		}
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());

		return diagnostics.Length == 1 && diagnostics[0].Id == DiagnosticIds.UseRepositoryPattern;
	}

	private static bool Should_NotReportDiagnostic_When_RepositoryHasInterface()
	{
		const string testCode = @"
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Infrastructure.Data
{
	public interface IOrderRepository
	{
		List<Order> GetAll();
	}

	public class OrderRepository : IOrderRepository // Implements interface
	{
		private readonly DbContext _context;

		public OrderRepository(DbContext context)
		{
			_context = context;
		}

		public List<Order> GetAll()
		{
			return _context.Set<Order>().ToList();
		}
	}
}";

		var diagnostics = RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());

		return diagnostics.Length == 0;
	}
}
