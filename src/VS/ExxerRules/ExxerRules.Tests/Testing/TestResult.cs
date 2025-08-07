namespace ExxerRules.Tests.Testing;

/// <summary>
/// Represents the result of executing a test case.
/// SRP: Encapsulates test execution outcome and details.
/// </summary>
public sealed class TestResult
{
	/// <summary>
	/// Gets the name of the test that was executed.
	/// </summary>
	public string TestName { get; }
	
	/// <summary>
	/// Gets a value indicating whether the test passed.
	/// </summary>
	public bool Passed { get; }
	
	/// <summary>
	/// Gets the error message if the test failed.
	/// </summary>
	public string? ErrorMessage { get; }
	
	/// <summary>
	/// Gets the exception that caused the test to fail, if any.
	/// </summary>
	public Exception? Exception { get; }
	
	/// <summary>
	/// Gets the execution time of the test.
	/// </summary>
	public TimeSpan ExecutionTime { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TestResult"/> class.
	/// </summary>
	/// <param name="testName">The name of the test.</param>
	/// <param name="passed">Whether the test passed.</param>
	/// <param name="errorMessage">The error message if failed.</param>
	/// <param name="exception">The exception if failed.</param>
	/// <param name="executionTime">The test execution time.</param>
	public TestResult(string testName, bool passed, string? errorMessage = null, Exception? exception = null, TimeSpan executionTime = default)
	{
		TestName = testName;
		Passed = passed;
		ErrorMessage = errorMessage;
		Exception = exception;
		ExecutionTime = executionTime;
	}
	
	/// <summary>
	/// Creates a successful test result.
	/// </summary>
	/// <param name="testName">The name of the test.</param>
	/// <param name="executionTime">The execution time.</param>
	/// <returns>A successful test result.</returns>
	public static TestResult Success(string testName, TimeSpan executionTime = default)
		=> new(testName, passed: true, executionTime: executionTime);
	
	/// <summary>
	/// Creates a failed test result.
	/// </summary>
	/// <param name="testName">The name of the test.</param>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="executionTime">The execution time.</param>
	/// <returns>A failed test result.</returns>
	public static TestResult Failure(string testName, string errorMessage, TimeSpan executionTime = default)
		=> new(testName, passed: false, errorMessage: errorMessage, executionTime: executionTime);
	
	/// <summary>
	/// Creates a failed test result with an exception.
	/// </summary>
	/// <param name="testName">The name of the test.</param>
	/// <param name="exception">The exception that caused the failure.</param>
	/// <param name="executionTime">The execution time.</param>
	/// <returns>A failed test result.</returns>
	public static TestResult FromException(string testName, Exception exception, TimeSpan executionTime = default)
		=> new(testName, passed: false, exception.Message, exception, executionTime);
}