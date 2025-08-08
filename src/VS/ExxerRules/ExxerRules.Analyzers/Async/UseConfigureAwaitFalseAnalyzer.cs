using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Async;

/// <summary>
/// Analyzer that enforces using ConfigureAwait(false) in library code.
/// Supports the performance and async best practices principles.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseConfigureAwaitFalseAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use ConfigureAwait(false) in library code";
	private static readonly LocalizableString MessageFormat = "Await expression should use ConfigureAwait(false) to avoid deadlocks in library code";
	private static readonly LocalizableString Description = "In library code, await expressions should use ConfigureAwait(false) to prevent potential deadlocks when called from synchronous contexts. This improves performance and prevents threading issues.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseConfigureAwaitFalse,
		Title,
		MessageFormat,
		DiagnosticCategories.Async,
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

		// TDD Green phase: Focus on await expressions
		context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
	}

	private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
	{
		var awaitExpression = (AwaitExpressionSyntax)context.Node;
		var expression = awaitExpression.Expression;

		// Check if the await expression already has ConfigureAwait
		if (HasConfigureAwait(expression))
		{
			return;
		}

		// Report diagnostic for missing ConfigureAwait(false)
		var diagnostic = Diagnostic.Create(
			Rule,
			awaitExpression.GetLocation());
		context.ReportDiagnostic(diagnostic);
	}

	private static bool HasConfigureAwait(ExpressionSyntax expression)
	{
		// Check for ConfigureAwait method call
		if (expression is InvocationExpressionSyntax invocation &&
			invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
			memberAccess.Name.Identifier.ValueText == "ConfigureAwait")
		{
			return true;
		}

		// Check for chained method calls ending with ConfigureAwait
		var current = expression;
		while (current is InvocationExpressionSyntax currentInvocation &&
			   currentInvocation.Expression is MemberAccessExpressionSyntax currentMember)
		{
			if (currentMember.Name.Identifier.ValueText == "ConfigureAwait")
			{
				return true;
			}

			current = currentMember.Expression;
		}

		return false;
	}
}
