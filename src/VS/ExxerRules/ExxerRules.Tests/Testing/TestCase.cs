using System.Diagnostics;

namespace ExxerRules.Tests.Testing;

/// <summary>
/// Represents a single test case with its execution logic.
/// SRP: Encapsulates test execution with timing and error handling.
/// </summary>
public sealed class TestCase : ITestCase
{
	private readonly Func<bool> _testMethod;

	/// <inheritdoc/>
	public string Name { get; }
	
	/// <inheritdoc/>
	public string Category { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TestCase"/> class.
	/// </summary>
	/// <param name="name">The test name.</param>
	/// <param name="category">The test category.</param>
	/// <param name="testMethod">The test method to execute.</param>
	public TestCase(string name, string category, Func<bool> testMethod)
	{
		Name = name;
		Category = category;
		_testMethod = testMethod;
	}

	/// <inheritdoc/>
	public TestResult Execute()
	{
		var stopwatch = Stopwatch.StartNew();
		
		try
		{
			var passed = _testMethod();
			stopwatch.Stop();
			
			return passed 
				? TestResult.Success(Name, stopwatch.Elapsed)
				: TestResult.Failure(Name, "Test assertion failed", stopwatch.Elapsed);
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			return TestResult.FromException(Name, ex, stopwatch.Elapsed);
		}
	}
}