// Induce a violation of the UseResultPatternAnalyzer
namespace NuGetTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        GetNumber();
    }

    public static int GetNumber()
    {
        throw new System.NotImplementedException();
    }
}
