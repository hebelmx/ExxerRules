using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that enforces using Shouldly instead of FluentAssertions for test assertions.
/// Supports the testing standards compliance principle.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotUseFluentAssertionsAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use Shouldly instead of FluentAssertions for assertions";
	private static readonly LocalizableString MessageFormat = "FluentAssertions usage detected: '{0}' - use Shouldly for consistent assertion patterns";
	private static readonly LocalizableString Description = "Shouldly provides more readable and maintainable assertion syntax than FluentAssertions. It's the preferred assertion framework for this project to ensure consistent testing patterns.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseShouldly,
		Title,
		MessageFormat,
		DiagnosticCategories.Testing,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// TDD Green phase: Focus only on using directive detection for now
		context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
		// TODO: Add member access detection in refactor phase
	}



	private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
	{
		var usingDirective = (UsingDirectiveSyntax)context.Node;

		var nameString = usingDirective.Name?.ToString();

		// Exact match for FluentAssertions (not Shouldly or other frameworks)
		if (nameString == "FluentAssertions")
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				usingDirective.GetLocation(),
				"using FluentAssertions");
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;

		// Check for .Should() calls which are characteristic of FluentAssertions
		if (memberAccess.Name.Identifier.ValueText == "Should")
		{
			// For test scenarios, assume .Should() refers to FluentAssertions
			var diagnostic = Diagnostic.Create(
				Rule,
				memberAccess.GetLocation(),
				".Should()");
			context.ReportDiagnostic(diagnostic);
		}

		// Check for other common FluentAssertions patterns
		var fluentAssertionsMethods = new[]
		{
			"Be", "BeEquivalentTo", "BeNull", "BeEmpty", "Contain",
			"HaveCount", "Match", "Satisfy", "BeOfType"
		};

		if (fluentAssertionsMethods.Contains(memberAccess.Name.Identifier.ValueText))
		{
			// Check if this is likely part of a FluentAssertions chain
			if (IsPartOfFluentAssertionsChain(memberAccess))
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					memberAccess.GetLocation(),
					$".{memberAccess.Name}");
				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	private static bool IsPartOfFluentAssertionsChain(MemberAccessExpressionSyntax memberAccess)
	{
		// Look for patterns like "something.Should().Be(...)"
		if (memberAccess.Expression is MemberAccessExpressionSyntax parentAccess)
		{
			return parentAccess.Name.Identifier.ValueText == "Should";
		}

		// Look for patterns where the parent expression contains "Should"
		var expressionText = memberAccess.Expression.ToString();
		return expressionText.Contains(".Should()");
	}

	private static string GetFullNamespace(INamespaceSymbol? namespaceSymbol)
	{
		if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
		{
			return string.Empty;
		}

		var parts = new List<string>();
		var current = namespaceSymbol;

		while (current != null && !current.IsGlobalNamespace)
		{
			parts.Insert(0, current.Name);
			current = current.ContainingNamespace;
		}

		return string.Join(".", parts);
	}
}
