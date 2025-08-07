namespace ExxerRules.Tests.Testing;

/// <summary>
/// Responsible for formatting and displaying test results.
/// SRP: Handles all test result reporting and formatting concerns.
/// </summary>
public sealed class TestReporter
{
	/// <summary>
	/// Reports the progress of a single test as it completes.
	/// </summary>
	/// <param name="result">The test result to report.</param>
	public void ReportProgress(TestResult result)
	{
		var status = result.Passed ? "[PASS]" : "[FAIL]";
		var color = result.Passed ? ConsoleColor.Green : ConsoleColor.Red;
		
		var originalColor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.Write(status);
		Console.ForegroundColor = originalColor;
		
		Console.WriteLine($" {result.TestName}");
		
		if (!result.Passed && !string.IsNullOrEmpty(result.ErrorMessage))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"      Error: {result.ErrorMessage}");
			Console.ForegroundColor = originalColor;
		}
	}
	
	/// <summary>
	/// Reports a comprehensive summary of all test results.
	/// </summary>
	/// <param name="results">The test results to summarize.</param>
	public void ReportSummary(IReadOnlyList<TestResult> results)
	{
		if (!results.Any())
		{
			Console.WriteLine("No tests were executed.");
			return;
		}

		var passed = results.Count(r => r.Passed);
		var failed = results.Count(r => !r.Passed);
		var total = results.Count;
		var successRate = (double)passed / total * 100;

		Console.WriteLine();
		Console.WriteLine("=== Test Summary ===");
		
		// Overall results
		Console.WriteLine($"Results: {passed}/{total} tests passed");
		
		var summaryColor = passed == total ? ConsoleColor.Green : ConsoleColor.Yellow;
		Console.ForegroundColor = summaryColor;
		Console.WriteLine($"Success Rate: {successRate:F1}%");
		Console.ResetColor();
		
		// Execution time summary
		var totalTime = results.Sum(r => r.ExecutionTime.TotalMilliseconds);
		Console.WriteLine($"Total Execution Time: {totalTime:F0}ms");
		
		// Category breakdown
		ReportCategoryBreakdown(results);
		
		// Failed tests details
		if (failed > 0)
		{
			ReportFailedTests(results.Where(r => !r.Passed));
		}
	}
	
	/// <summary>
	/// Reports test results grouped by category.
	/// </summary>
	/// <param name="results">The test results to group and report.</param>
	private void ReportCategoryBreakdown(IReadOnlyList<TestResult> results)
	{
		var categories = results
			.GroupBy(r => GetCategoryFromTestName(r.TestName))
			.OrderBy(g => g.Key);

		if (categories.Count() <= 1) return;

		Console.WriteLine();
		Console.WriteLine("=== Results by Category ===");
		
		foreach (var category in categories)
		{
			var categoryResults = category.ToList();
			var categoryPassed = categoryResults.Count(r => r.Passed);
			var categoryTotal = categoryResults.Count;
			var categoryRate = (double)categoryPassed / categoryTotal * 100;
			
			Console.WriteLine($"{category.Key}: {categoryPassed}/{categoryTotal} ({categoryRate:F0}%)");
		}
	}
	
	/// <summary>
	/// Reports detailed information about failed tests.
	/// </summary>
	/// <param name="failedTests">The failed test results.</param>
	private void ReportFailedTests(IEnumerable<TestResult> failedTests)
	{
		Console.WriteLine();
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine("=== Failed Tests ===");
		Console.ResetColor();
		
		foreach (var result in failedTests)
		{
			Console.WriteLine($"❌ {result.TestName}");
			if (!string.IsNullOrEmpty(result.ErrorMessage))
			{
				Console.WriteLine($"   Error: {result.ErrorMessage}");
			}
			if (result.Exception != null)
			{
				Console.WriteLine($"   Exception: {result.Exception.GetType().Name}");
				Console.WriteLine($"   Message: {result.Exception.Message}");
			}
		}
	}
	
	/// <summary>
	/// Extracts category information from test name.
	/// </summary>
	/// <param name="testName">The test name.</param>
	/// <returns>The inferred category.</returns>
	private static string GetCategoryFromTestName(string testName)
	{
		// Extract category from test method names like "Should_ReportDiagnostic_When_UsingMoq"
		if (testName.Contains("Moq") || testName.Contains("FluentAssertions") || testName.Contains("XUnit") || testName.Contains("TestNaming"))
			return "Testing Standards";
		
		if (testName.Contains("Null") || testName.Contains("Parameter"))
			return "Null Safety";
		
		if (testName.Contains("Async") || testName.Contains("CancellationToken") || testName.Contains("ConfigureAwait"))
			return "Async Patterns";
		
		if (testName.Contains("Xml") || testName.Contains("Documentation"))
			return "Documentation";
		
		if (testName.Contains("Magic") || testName.Contains("Region") || testName.Contains("ExpressionBod") || testName.Contains("PatternMatching"))
			return "Code Quality";
		
		if (testName.Contains("Throw") || testName.Contains("Result"))
			return "Functional Patterns";
		
		if (testName.Contains("Logging") || testName.Contains("Console"))
			return "Logging";
		
		if (testName.Contains("Linq") || testName.Contains("Efficient"))
			return "Performance";
		
		if (testName.Contains("Domain") || testName.Contains("Infrastructure") || testName.Contains("Repository"))
			return "Architecture";
		
		return "General";
	}
}