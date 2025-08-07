namespace ExxerRules.Tests.Testing;

/// <summary>
/// Executes a collection of test cases and collects the results.
/// SRP: Responsible only for test execution orchestration.
/// </summary>
public sealed class TestRunner
{
	/// <summary>
	/// Runs all provided test cases and returns the results.
	/// </summary>
	/// <param name="testCases">The test cases to execute.</param>
	/// <returns>A collection of test results.</returns>
	public IReadOnlyList<TestResult> RunTests(IEnumerable<ITestCase> testCases)
	{
		var results = new List<TestResult>();
		
		foreach (var testCase in testCases)
		{
			var result = testCase.Execute();
			results.Add(result);
		}
		
		return results;
	}
	
	/// <summary>
	/// Runs all provided test cases and returns the results with progress reporting.
	/// </summary>
	/// <param name="testCases">The test cases to execute.</param>
	/// <param name="progressCallback">Optional callback for progress reporting.</param>
	/// <returns>A collection of test results.</returns>
	public IReadOnlyList<TestResult> RunTests(IEnumerable<ITestCase> testCases, Action<TestResult>? progressCallback = null)
	{
		var results = new List<TestResult>();
		
		foreach (var testCase in testCases)
		{
			var result = testCase.Execute();
			results.Add(result);
			progressCallback?.Invoke(result);
		}
		
		return results;
	}
}