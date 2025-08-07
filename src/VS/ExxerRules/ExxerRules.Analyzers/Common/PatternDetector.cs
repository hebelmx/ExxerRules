using System.Text.RegularExpressions;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExxerRules.Analyzers.Common;

/// <summary>
/// Utility class for detecting code patterns using functional approach with FluentResults.
/// </summary>
public static class PatternDetector
{
	/// <summary>
	/// Detects if a method follows the specified naming pattern.
	/// </summary>
	/// <param name="methodName">The method name to validate.</param>
	/// <param name="pattern">The regex pattern to match against.</param>
	/// <returns>A result indicating if the pattern matches.</returns>
	public static Result<bool> ValidateMethodNaming(string methodName, string pattern)
	{
		if (string.IsNullOrWhiteSpace(methodName))
		{
			return AnalysisResult.Failure<bool>("Method name cannot be null or empty");
		}

		if (string.IsNullOrWhiteSpace(pattern))
		{
			return AnalysisResult.Failure<bool>("Pattern cannot be null or empty");
		}

		try
		{
			var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
			var isMatch = regex.IsMatch(methodName);
			return AnalysisResult.Success(isMatch);
		}
		catch (ArgumentException ex)
		{
			return AnalysisResult.Failure<bool>($"Invalid regex pattern: {ex.Message}");
		}
	}

	/// <summary>
	/// Detects if a method has test attributes.
	/// </summary>
	/// <param name="method">The method declaration to analyze.</param>
	/// <param name="semanticModel">The semantic model for type resolution.</param>
	/// <returns>A result containing information about test attributes.</returns>
	public static Result<TestAttributeInfo> DetectTestAttributes(MethodDeclarationSyntax method, SemanticModel semanticModel)
	{
		if (method == null)
		{
			return AnalysisResult.Failure<TestAttributeInfo>("Method cannot be null");
		}

		if (semanticModel == null)
		{
			return AnalysisResult.Failure<TestAttributeInfo>("Semantic model cannot be null");
		}

		var testAttributes = new[]
		{
			"Fact", "Theory", "Test", "TestMethod",
			"Xunit.Fact", "Xunit.Theory",
			"NUnit.Framework.Test",
			"Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod"
		};

		var foundAttributes = new List<string>();

		foreach (var attributeList in method.AttributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				var attributeName = attribute.Name.ToString();

				var matchingAttribute = testAttributes.FirstOrDefault(ta =>
					attributeName == ta ||
					attributeName.EndsWith("." + ta.Split('.').Last()) ||
					ta.EndsWith("." + attributeName));

				if (matchingAttribute != null)
				{
					foundAttributes.Add(matchingAttribute);
				}

				// Use semantic model for more accurate detection
				var attributeSymbol = semanticModel.GetSymbolInfo(attribute).Symbol;
				if (attributeSymbol is IMethodSymbol constructor)
				{
					var attributeType = constructor.ContainingType.ToDisplayString();
					var semanticMatch = testAttributes.FirstOrDefault(ta => attributeType.Contains(ta.Split('.').Last()));
					if (semanticMatch != null && !foundAttributes.Contains(semanticMatch))
					{
						foundAttributes.Add(semanticMatch);
					}
				}
			}
		}

		var info = new TestAttributeInfo(
			foundAttributes,
			foundAttributes.Count > 0,
			GetTestFramework(foundAttributes));

		return AnalysisResult.Success(info);
	}

	/// <summary>
	/// Detects if a class appears to be a test class based on naming and attributes.
	/// </summary>
	/// <param name="classDeclaration">The class declaration to analyze.</param>
	/// <returns>A result indicating if the class is a test class.</returns>
	public static Result<bool> DetectTestClass(ClassDeclarationSyntax classDeclaration)
	{
		if (classDeclaration == null)
		{
			return AnalysisResult.Failure<bool>("Class declaration cannot be null");
		}

		var className = classDeclaration.Identifier.ValueText;

		// Check naming patterns
		var testClassSuffixes = new[] { "Tests", "Test", "Specs", "Spec" };
		var hasTestSuffix = testClassSuffixes.Any(suffix => className.EndsWith(suffix));

		// Check class attributes
		var testClassAttributes = new[] { "TestClass", "TestFixture" };
		var hasTestAttribute = classDeclaration.AttributeLists
			.SelectMany(list => list.Attributes)
			.Any(attr => testClassAttributes.Any(testAttr =>
				attr.Name.ToString().Contains(testAttr)));

		var isTestClass = hasTestSuffix || hasTestAttribute;
		return AnalysisResult.Success(isTestClass);
	}

	private static TestFramework GetTestFramework(List<string> attributes)
	{
		if (attributes.Any(a => a.Contains("Xunit") || a == "Fact" || a == "Theory"))
		{
			return TestFramework.XUnit;
		}

		if (attributes.Any(a => a.Contains("NUnit") || a == "Test"))
		{
			return TestFramework.NUnit;
		}

		if (attributes.Any(a => a.Contains("Microsoft.VisualStudio.TestTools") || a == "TestMethod"))
		{
			return TestFramework.MSTest;
		}

		return TestFramework.Unknown;
	}
}

/// <summary>
/// Information about test attributes found on a method.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestAttributeInfo"/> class.
/// </remarks>
/// <param name="attributeNames">The names of the test attributes found.</param>
/// <param name="hasTestAttributes">Whether any test attributes were found.</param>
/// <param name="framework">The detected test framework.</param>
public class TestAttributeInfo(IReadOnlyList<string> attributeNames, bool hasTestAttributes, TestFramework framework)
{

	/// <summary>
	/// Gets the names of the test attributes found.
	/// </summary>
	public IReadOnlyList<string> AttributeNames { get; } = attributeNames ?? throw new ArgumentNullException(nameof(attributeNames));

	/// <summary>
	/// Gets a value indicating whether any test attributes were found.
	/// </summary>
	public bool HasTestAttributes { get; } = hasTestAttributes;

	/// <summary>
	/// Gets the detected test framework.
	/// </summary>
	public TestFramework Framework { get; } = framework;
}

/// <summary>
/// Enumeration of supported test frameworks.
/// </summary>
public enum TestFramework
{
	Unknown,
	XUnit,
	NUnit,
	MSTest
}
