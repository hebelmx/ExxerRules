namespace ExxerRules.Tests.Testing;

/// <summary>
/// Manages a collection of test cases and provides organization capabilities.
/// SRP: Responsible for test case collection management and suite organization.
/// </summary>
public sealed class TestSuite
{
	private readonly List<ITestCase> _testCases = new();

	/// <summary>
	/// Gets the name of the test suite.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the total number of tests in the suite.
	/// </summary>
	public int TestCount => _testCases.Count;

	/// <summary>
	/// Initializes a new instance of the <see cref="TestSuite"/> class.
	/// </summary>
	/// <param name="name">The name of the test suite.</param>
	public TestSuite(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Adds a test case to the suite.
	/// </summary>
	/// <param name="testCase">The test case to add.</param>
	/// <returns>This test suite for method chaining.</returns>
	public TestSuite AddTest(ITestCase testCase)
	{
		_testCases.Add(testCase);
		return this;
	}

	/// <summary>
	/// Adds a test case to the suite using a simple method.
	/// </summary>
	/// <param name="name">The test name.</param>
	/// <param name="category">The test category.</param>
	/// <param name="testMethod">The test method.</param>
	/// <returns>This test suite for method chaining.</returns>
	public TestSuite AddTest(string name, string category, Func<bool> testMethod)
	{
		_testCases.Add(new TestCase(name, category, testMethod));
		return this;
	}

	/// <summary>
	/// Gets all test cases in the suite.
	/// </summary>
	/// <returns>A read-only collection of test cases.</returns>
	public IReadOnlyList<ITestCase> GetTests()
	{
		return _testCases.AsReadOnly();
	}

	/// <summary>
	/// Gets test cases filtered by category.
	/// </summary>
	/// <param name="category">The category to filter by.</param>
	/// <returns>Test cases matching the category.</returns>
	public IEnumerable<ITestCase> GetTestsByCategory(string category)
	{
		return _testCases.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Gets all unique categories in the test suite.
	/// </summary>
	/// <returns>A collection of category names.</returns>
	public IEnumerable<string> GetCategories()
	{
		return _testCases.Select(t => t.Category).Distinct().OrderBy(c => c);
	}

	/// <summary>
	/// Executes all tests in the suite.
	/// </summary>
	/// <param name="runner">The test runner to use.</param>
	/// <param name="reporter">Optional reporter for progress updates.</param>
	/// <returns>The test results.</returns>
	public IReadOnlyList<TestResult> Execute(TestRunner runner, TestReporter? reporter = null)
	{
		Console.WriteLine($"=== {Name} ===");
		Console.WriteLine();

		if (reporter != null)
		{
			return runner.RunTests(_testCases, reporter.ReportProgress);
		}
		
		return runner.RunTests(_testCases);
	}
}