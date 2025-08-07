using ExxerRules.Tests.Testing;
using ExxerRules.Tests.TestCases;

namespace ExxerRules.Tests;

/// <summary>
/// Refactored manual test runner using SRP (Single Responsibility Principle).
/// Each class now has a single, well-defined responsibility.
/// SRP: This class is responsible only for orchestrating the test execution workflow.
/// </summary>
public static class RefactoredManualTestRunner
{
	/// <summary>
	/// Runs all tests using the refactored SRP-based architecture.
	/// </summary>
	public static void RunAllTests()
	{
		// Create components following SRP
		var testRunner = new TestRunner();        // SRP: Executes tests
		var testReporter = new TestReporter();    // SRP: Reports results
		var testSuite = CreateTestSuite();        // SRP: Manages test collection

		Console.WriteLine("=== Refactored Manual Test Runner - SRP Architecture ===");
		Console.WriteLine();

		// Execute all tests with progress reporting
		var results = testSuite.Execute(testRunner, testReporter);

		// Generate comprehensive summary report
		testReporter.ReportSummary(results);
	}

	/// <summary>
	/// Creates and configures the complete test suite.
	/// SRP: Responsible only for test suite composition and configuration.
	/// </summary>
	/// <returns>A configured test suite with all test cases.</returns>
	private static TestSuite CreateTestSuite()
	{
		var suite = new TestSuite("ExxerRules Analyzers Test Suite");

		// Testing Standards Tests
		suite.AddTest("Should_NotReportDiagnostic_When_TestMethodFollowsNamingConvention", 
					  "Testing Standards", 
					  TestingStandardsTests.Should_NotReportDiagnostic_When_TestMethodFollowsNamingConvention)
			 
			 .AddTest("Should_ReportDiagnostic_When_TestMethodDoesNotFollowNamingConvention",
					  "Testing Standards",
					  TestingStandardsTests.Should_ReportDiagnostic_When_TestMethodDoesNotFollowNamingConvention)
			 
			 .AddTest("Should_ReportDiagnostic_When_UsingMoq",
					  "Testing Standards",
					  TestingStandardsTests.Should_ReportDiagnostic_When_UsingMoq)
			 
			 .AddTest("Should_NotReportDiagnostic_When_UsingNSubstitute",
					  "Testing Standards",
					  TestingStandardsTests.Should_NotReportDiagnostic_When_UsingNSubstitute)
			 
			 .AddTest("Should_ReportDiagnostic_When_UsingFluentAssertions",
					  "Testing Standards",
					  TestingStandardsTests.Should_ReportDiagnostic_When_UsingFluentAssertions)
			 
			 .AddTest("Should_NotReportDiagnostic_When_UsingShouldly",
					  "Testing Standards",
					  TestingStandardsTests.Should_NotReportDiagnostic_When_UsingShouldly);

		// Functional Patterns Tests
		suite.AddTest("Should_ReportDiagnostic_When_ThrowStatementIsUsed",
					  "Functional Patterns",
					  FunctionalPatternsTests.Should_ReportDiagnostic_When_ThrowStatementIsUsed)
			 
			 .AddTest("Should_NotReportDiagnostic_When_ReturningResult",
					  "Functional Patterns",
					  FunctionalPatternsTests.Should_NotReportDiagnostic_When_ReturningResult)
			 
			 .AddTest("Should_ReportDiagnostic_When_ThrowingInsteadOfReturningResult",
					  "Functional Patterns",
					  FunctionalPatternsTests.Should_ReportDiagnostic_When_ThrowingInsteadOfReturningResult)
			 
			 .AddTest("Should_NotReportDiagnostic_When_UsingResultPattern",
					  "Functional Patterns",
					  FunctionalPatternsTests.Should_NotReportDiagnostic_When_UsingResultPattern);

		// Code Formatting Tests
		suite.AddTest("Should_ReportHiddenDiagnostic_When_ProjectFormattingAnalyzer",
					  "Code Formatting",
					  TestCases.CodeFormattingTests.Should_ReportHiddenDiagnostic_When_ProjectFormattingAnalyzer)
			 
			 .AddTest("Should_ReportDiagnostic_When_FormattingIssuesDetected",
					  "Code Formatting",
					  TestCases.CodeFormattingTests.Should_ReportDiagnostic_When_FormattingIssuesDetected)
			 
			 .AddTest("Should_NotReportDiagnostic_When_CodeIsWellFormatted",
					  "Code Formatting",
					  TestCases.CodeFormattingTests.Should_NotReportDiagnostic_When_CodeIsWellFormatted);

		// TODO: Add remaining test categories here:
		// - Null Safety Tests
		// - Async Patterns Tests  
		// - Documentation Tests
		// - Code Quality Tests
		// - Logging Tests
		// - Performance Tests
		// - Architecture Tests

		return suite;
	}

	/// <summary>
	/// Demonstrates the flexibility of the SRP architecture by running tests by category.
	/// </summary>
	/// <param name="category">The category to run.</param>
	public static void RunTestsByCategory(string category)
	{
		var testRunner = new TestRunner();
		var testReporter = new TestReporter();
		var testSuite = CreateTestSuite();

		var categoryTests = testSuite.GetTestsByCategory(category).ToList();
		
		if (!categoryTests.Any())
		{
			Console.WriteLine($"No tests found for category: {category}");
			return;
		}

		Console.WriteLine($"=== Running {category} Tests Only ===");
		Console.WriteLine();

		var results = testRunner.RunTests(categoryTests, testReporter.ReportProgress);
		testReporter.ReportSummary(results);
	}
}