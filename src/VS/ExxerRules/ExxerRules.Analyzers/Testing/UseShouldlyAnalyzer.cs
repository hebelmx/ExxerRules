using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that enforces Shouldly usage instead of FluentAssertions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseShouldlyAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use Shouldly for assertions";
	private static readonly LocalizableString MessageFormat = "Use Shouldly instead of '{0}' for assertions";
	private static readonly LocalizableString Description = "Shouldly should be used for assertions instead of FluentAssertions or other assertion libraries.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseShouldly,
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

		context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
		context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
		context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
	}

	private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
	{
		var usingDirective = (UsingDirectiveSyntax)context.Node;
		var namespaceName = usingDirective.Name?.ToString();

		if (namespaceName == null)
			return;

		// Check for forbidden assertion library namespaces
		var forbiddenNamespaces = new[]
		{
			"FluentAssertions",
			"FluentAssertions.Extensions",
			"Microsoft.VisualStudio.TestTools.UnitTesting",
			"NUnit.Framework"
		};

		foreach (var forbidden in forbiddenNamespaces)
		{
			if (namespaceName == forbidden || namespaceName.StartsWith(forbidden + "."))
			{
				var libraryName = GetLibraryNameFromNamespace(forbidden);
				var diagnostic = Diagnostic.Create(
					Rule,
					usingDirective.GetLocation(),
					libraryName);

				context.ReportDiagnostic(diagnostic);
				break;
			}
		}
	}

	private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		// Check for FluentAssertions method calls
		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			var methodName = memberAccess.Name.Identifier.ValueText;
			
			// Common FluentAssertions methods
			var fluentAssertionsMethods = new[]
			{
				"Should",
				"Be",
				"BeEquivalentTo",
				"BeTrue",
				"BeFalse",
				"BeNull",
				"NotBeNull",
				"Contain",
				"NotContain",
				"StartWith",
				"EndWith",
				"Match",
				"BeEmpty",
				"NotBeEmpty",
				"HaveCount",
				"BeGreaterThan",
				"BeLessThan",
				"BePositive",
				"BeNegative",
				"BeCloseTo"
			};

			if (fluentAssertionsMethods.Contains(methodName))
			{
				// Check if this is likely a FluentAssertions call by examining the chain
				if (IsFluentAssertionsChain(memberAccess))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						invocation.GetLocation(),
						"FluentAssertions");

					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		// Check for MSTest assertions
		var invocationText = invocation.ToString();
		if (invocationText.StartsWith("Assert."))
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				invocation.GetLocation(),
				"MSTest Assert");

			context.ReportDiagnostic(diagnostic);
		}

		// Check for NUnit assertions
		if (invocationText.StartsWith("Assert.That") || invocationText.StartsWith("ClassicAssert."))
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				invocation.GetLocation(),
				"NUnit Assert");

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;
		var memberName = memberAccess.Name.Identifier.ValueText;

		// Check for direct FluentAssertions usage patterns
		if (memberName == "Should" && memberAccess.Expression != null)
		{
			// This might be a FluentAssertions .Should() call
			var expressionText = memberAccess.Expression.ToString();
			
			// Skip if it's clearly a Shouldly usage (variables/properties ending with common patterns)
			if (!expressionText.EndsWith("ShouldBe") && 
				!expressionText.EndsWith("ShouldNotBe") &&
				!expressionText.EndsWith("ShouldContain"))
			{
				// Check semantic model to determine if this is FluentAssertions
				var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
				if (symbolInfo.Symbol is IMethodSymbol method)
				{
					var containingNamespace = method.ContainingNamespace?.ToDisplayString();
					if (containingNamespace != null && containingNamespace.StartsWith("FluentAssertions"))
					{
						var diagnostic = Diagnostic.Create(
							Rule,
							memberAccess.GetLocation(),
							"FluentAssertions");

						context.ReportDiagnostic(diagnostic);
					}
				}
			}
		}
	}

	private static bool IsFluentAssertionsChain(MemberAccessExpressionSyntax memberAccess)
	{
		// Look for patterns like: something.Should().Be() or something.Should().BeTrue()
		if (memberAccess.Expression is InvocationExpressionSyntax invocation &&
			invocation.Expression is MemberAccessExpressionSyntax innerMemberAccess &&
			innerMemberAccess.Name.Identifier.ValueText == "Should")
		{
			return true;
		}

		return false;
	}

	private static string GetLibraryNameFromNamespace(string namespaceName)
	{
		return namespaceName switch
		{
			var ns when ns.StartsWith("FluentAssertions") => "FluentAssertions",
			var ns when ns.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting") => "MSTest Assert",
			var ns when ns.StartsWith("NUnit.Framework") => "NUnit Assert",
			_ => "unknown assertion library"
		};
	}
}