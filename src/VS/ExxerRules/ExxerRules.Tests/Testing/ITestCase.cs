namespace ExxerRules.Tests.Testing;

/// <summary>
/// Defines a contract for a test case that can be executed.
/// SRP: Represents a single test case with its execution logic.
/// </summary>
public interface ITestCase
{
	/// <summary>
	/// Gets the name of the test case.
	/// </summary>
	string Name { get; }
	
	/// <summary>
	/// Gets the category of the test case for grouping purposes.
	/// </summary>
	string Category { get; }
	
	/// <summary>
	/// Executes the test case and returns the result.
	/// </summary>
	/// <returns>The test execution result.</returns>
	TestResult Execute();
}