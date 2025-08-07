namespace ExxerRules.Tests;

/// <summary>
/// Simple console program to run manual tests following TDD principles.
/// Now uses SRP-based architecture for better maintainability.
/// </summary>
public static class Program
{
	public static void Main(string[] args)
	{
		// Use the refactored SRP-based test runner
		RefactoredManualTestRunner.RunAllTests();

		// Demonstrate category-specific testing capability
		Console.WriteLine();
		Console.WriteLine("=== Category-Specific Test Example ===");
		RefactoredManualTestRunner.RunTestsByCategory("Testing Standards");

		Console.WriteLine();
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
	}
}
