namespace ExxerRules.Tests;

/// <summary>
/// Simple console program to run manual tests following TDD principles.
/// </summary>
public static class Program
{
	public static void Main(string[] args)
	{
		ManualTestRunner.RunAllTests();

		Console.WriteLine();
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
	}
}
