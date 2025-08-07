using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that enforces XUnit v3 usage for testing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseXUnitV3Analyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use XUnit v3 for testing";
	private static readonly LocalizableString MessageFormat = "Use XUnit v3 instead of '{0}' for testing";
	private static readonly LocalizableString Description = "XUnit v3 should be used for all testing instead of other testing frameworks like MSTest or NUnit.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseXUnitV3,
		Title,
		MessageFormat,
		DiagnosticCategories.Testing,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
		context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
	}

	private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
	{
		var attribute = (AttributeSyntax)context.Node;
		var attributeName = attribute.Name.ToString();

		// Check for forbidden testing framework attributes
		var forbiddenAttributes = new[]
		{
			"TestMethod",
			"TestClass",
			"TestInitialize",
			"TestCleanup",
			"ClassInitialize",
			"ClassCleanup",
			"AssemblyInitialize",
			"AssemblyCleanup",
			"Test",
			"TestCase",
			"TestFixture",
			"SetUp",
			"TearDown",
			"OneTimeSetUp",
			"OneTimeTearDown",
			"TestFixtureSetUp",
			"TestFixtureTearDown"
		};

		foreach (var forbidden in forbiddenAttributes)
		{
			if (attributeName == forbidden || attributeName.EndsWith("." + forbidden))
			{
				var framework = GetFrameworkName(forbidden);
				var diagnostic = Diagnostic.Create(
					Rule,
					attribute.GetLocation(),
					framework);

				context.ReportDiagnostic(diagnostic);
				break;
			}
		}
	}

	private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
	{
		var usingDirective = (UsingDirectiveSyntax)context.Node;
		var namespaceName = usingDirective.Name?.ToString();

		if (namespaceName == null)
			return;

		// TDD Fix: Detect XUnit v2 but allow XUnit v3
		if (namespaceName == "Xunit")
		{
			// Report diagnostic for XUnit v2 (should upgrade to v3)
			var diagnostic = Diagnostic.Create(
				Rule,
				usingDirective.GetLocation(),
				"XUnit v2");
			context.ReportDiagnostic(diagnostic);
			return;
		}

		// Check for other forbidden testing framework namespaces
		var forbiddenNamespaces = new[]
		{
			"Microsoft.VisualStudio.TestTools.UnitTesting",
			"NUnit.Framework",
			"NUnit"
		};

		foreach (var forbidden in forbiddenNamespaces)
		{
			if (namespaceName == forbidden || namespaceName.StartsWith(forbidden + "."))
			{
				var framework = GetFrameworkNameFromNamespace(forbidden);
				var diagnostic = Diagnostic.Create(
					Rule,
					usingDirective.GetLocation(),
					framework);

				context.ReportDiagnostic(diagnostic);
				break;
			}
		}
	}

	private static string GetFrameworkName(string attributeName)
	{
		return attributeName switch
		{
			"TestMethod" or "TestClass" or "TestInitialize" or "TestCleanup" or 
			"ClassInitialize" or "ClassCleanup" or "AssemblyInitialize" or "AssemblyCleanup" => "MSTest",
			"Test" or "TestCase" or "TestFixture" or "SetUp" or "TearDown" or 
			"OneTimeSetUp" or "OneTimeTearDown" or "TestFixtureSetUp" or "TestFixtureTearDown" => "NUnit",
			_ => "unknown testing framework"
		};
	}

	private static string GetFrameworkNameFromNamespace(string namespaceName)
	{
		return namespaceName switch
		{
			var ns when ns.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting") => "MSTest",
			var ns when ns.StartsWith("NUnit") => "NUnit",
			_ => "unknown testing framework"
		};
	}
}