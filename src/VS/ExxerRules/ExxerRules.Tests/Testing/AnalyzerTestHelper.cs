using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Tests.Testing;

/// <summary>
/// Provides helper methods for testing diagnostic analyzers.
/// SRP: Encapsulates analyzer execution logic and compilation details.
/// </summary>
public static class AnalyzerTestHelper
{
	/// <summary>
	/// Runs a diagnostic analyzer on the given source code and returns the diagnostics.
	/// </summary>
	/// <param name="sourceCode">The source code to analyze.</param>
	/// <param name="analyzer">The analyzer to run.</param>
	/// <returns>The diagnostics reported by the analyzer.</returns>
	public static ImmutableArray<Diagnostic> RunAnalyzer(string sourceCode, DiagnosticAnalyzer analyzer) => RunAnalyzer(sourceCode, analyzer, includeHidden: false);

	/// <summary>
	/// Runs a diagnostic analyzer on the given source code and returns the diagnostics.
	/// </summary>
	/// <param name="sourceCode">The source code to analyze.</param>
	/// <param name="analyzer">The analyzer to run.</param>
	/// <param name="includeHidden">Whether to include hidden diagnostics in the results.</param>
	/// <returns>The diagnostics reported by the analyzer.</returns>
	public static ImmutableArray<Diagnostic> RunAnalyzer(string sourceCode, DiagnosticAnalyzer analyzer, bool includeHidden)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		var references = GetMetadataReferences();

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[analyzer]);

		var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
		return includeHidden
			? [.. diagnostics]
			: [.. diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden)];
	}

	/// <summary>
	/// Gets the metadata references required for compilation.
	/// </summary>
	/// <returns>An array of metadata references.</returns>
	private static MetadataReference[] GetMetadataReferences()
	{
		// Get basic .NET references
		var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator);

		var neededAssemblies = new[]
		{
			"System.Runtime",
			"System.Collections",
			"System.Linq",
			"System.Console",
			"netstandard",
			"mscorlib"
		};

		var references = trustedAssembliesPaths
			.Where(p => neededAssemblies.Any(na => Path.GetFileNameWithoutExtension(p) == na))
			.Select(p => MetadataReference.CreateFromFile(p))
			.ToList();

		// Add specific references for testing framework
		try
		{
			// Try to add XUnit reference if available
			var xunitPath = Path.Combine(AppContext.BaseDirectory, "xunit.core.dll");
			if (File.Exists(xunitPath))
			{
				references.Add(MetadataReference.CreateFromFile(xunitPath));
			}
		}
		catch
		{
			// If XUnit isn't available, continue without it
		}

		return [.. references];
	}
}
