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
	// Constants for error messages to satisfy EXXER500 and IDE1006 (camelCase for private constants)
	private const string methodNameNullErrorMessage = "Method name cannot be null or empty";
	private const string patternNullErrorMessage = "Pattern cannot be null or empty";
	private const string invalidRegexErrorMessage = "Invalid regex pattern: {0}";
	private const string methodNullErrorMessage = "Method cannot be null";
	private const string semanticModelNullErrorMessage = "Semantic model cannot be null";
	private const string classDeclarationNullErrorMessage = "Class declaration cannot be null";
	private const string cannotBeNullSuffix = " cannot be null";
	
	// Test framework constants
	private const string xunitFramework = "Xunit";
	private const string factAttribute = "Fact";
	private const string theoryAttribute = "Theory";
	private const string nunitFramework = "NUnit";
	private const string testAttribute = "Test";
	private const string msTestFramework = "Microsoft.VisualStudio.TestTools";
	private const string testMethodAttribute = "TestMethod";
	private const string xunitFactAttribute = "Xunit.Fact";
	private const string xunitTheoryAttribute = "Xunit.Theory";
	private const string nunitTestAttribute = "NUnit.Framework.Test";
	private const string msTestMethodAttribute = "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod";
	
	// Test class constants
	private const string testsSuffix = "Tests";
	private const string testSuffix = "Test";
	private const string specsSuffix = "Specs";
	private const string specSuffix = "Spec";
	private const string testClassAttribute = "TestClass";
	private const string testFixtureAttribute = "TestFixture";

	/// <summary>
	/// Detects if a method follows the specified naming pattern.
	/// </summary>
	/// <param name="methodName">The method name to validate.</param>
	/// <param name="pattern">The regex pattern to match against.</param>
	/// <returns>A result indicating if the pattern matches.</returns>
	public static Result<bool> ValidateMethodNaming(string methodName, string pattern)
	{
		// EXXER200: Validate null parameters
		if (methodName is null)
		{
			return AnalysisResult.Failure<bool>(nameof(methodName) + cannotBeNullSuffix);
		}
		
		if (pattern is null)
		{
			return AnalysisResult.Failure<bool>(nameof(pattern) + cannotBeNullSuffix);
		}

		if (string.IsNullOrWhiteSpace(methodName))
		{
			return AnalysisResult.Failure<bool>(methodNameNullErrorMessage);
		}

		if (string.IsNullOrWhiteSpace(pattern))
		{
			return AnalysisResult.Failure<bool>(patternNullErrorMessage);
		}

		try
		{
			var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
			var isMatch = regex.IsMatch(methodName);
			return AnalysisResult.Success(isMatch);
		}
		catch (ArgumentException ex)
		{
			return AnalysisResult.Failure<bool>(string.Format(invalidRegexErrorMessage, ex.Message));
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
		// EXXER200: Validate null parameters
		if (method is null)
		{
			return AnalysisResult.Failure<TestAttributeInfo>(methodNullErrorMessage);
		}

		if (semanticModel is null)
		{
			return AnalysisResult.Failure<TestAttributeInfo>(semanticModelNullErrorMessage);
		}

		var testAttributes = new[]
		{
			factAttribute, theoryAttribute, testAttribute, testMethodAttribute,
			xunitFactAttribute, xunitTheoryAttribute,
			nunitTestAttribute,
			msTestMethodAttribute
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
		// EXXER200: Validate null parameters
		if (classDeclaration is null)
		{
			return AnalysisResult.Failure<bool>(classDeclarationNullErrorMessage);
		}

		var className = classDeclaration.Identifier.ValueText;

		// Check naming patterns
		var testClassSuffixes = new[] { testsSuffix, testSuffix, specsSuffix, specSuffix };
		var hasTestSuffix = testClassSuffixes.Any(suffix => className.EndsWith(suffix));

		// Check class attributes
		var testClassAttributes = new[] { testClassAttribute, testFixtureAttribute };
		var hasTestAttribute = classDeclaration.AttributeLists
			.SelectMany(list => list.Attributes)
			.Any(attr => testClassAttributes.Any(testAttr =>
				attr.Name.ToString().Contains(testAttr)));

		var isTestClass = hasTestSuffix || hasTestAttribute;
		return AnalysisResult.Success(isTestClass);
	}

	private static TestFramework GetTestFramework(List<string> attributes)
	{
		// EXXER200: Validate null parameters - return safe default instead of throwing
		if (attributes is null)
		{
			return TestFramework.Unknown;
		}

		if (attributes.Any(a => a.Contains(xunitFramework) || a == factAttribute || a == theoryAttribute))
		{
			return TestFramework.XUnit;
		}

		if (attributes.Any(a => a.Contains(nunitFramework) || a == testAttribute))
		{
			return TestFramework.NUnit;
		}

		if (attributes.Any(a => a.Contains(msTestFramework) || a == testMethodAttribute))
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
	public IReadOnlyList<string> AttributeNames { get; } = attributeNames ?? Array.Empty<string>();

	/// <summary>
	/// Gets a value indicating whether any test attributes were found.
	/// </summary>
	public bool HasTestAttributes { get; } = hasTestAttributes;

	/// <summary>
	/// Gets the detected test framework.
	/// </summary>
	public TestFramework Framework { get; } = framework;
	
	/// <summary>
	/// Gets a value indicating whether the attribute names are valid (not null).
	/// </summary>
	public bool IsValid => AttributeNames != null;
}

/// <summary>
/// Enumeration of supported test frameworks.
/// </summary>
public enum TestFramework
{
	/// <summary>
	/// Unknown or unsupported test framework.
	/// </summary>
	Unknown,
	
	/// <summary>
	/// xUnit test framework.
	/// </summary>
	XUnit,
	
	/// <summary>
	/// NUnit test framework.
	/// </summary>
	NUnit,
	
	/// <summary>
	/// MSTest test framework.
	/// </summary>
	MSTest
}
