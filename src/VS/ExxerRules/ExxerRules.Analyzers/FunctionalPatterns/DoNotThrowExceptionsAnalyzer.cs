using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.FunctionalPatterns;

/// <summary>
/// Analyzer that enforces Result&lt;T&gt; pattern instead of throwing exceptions.
/// Supports the core architectural principle of functional error handling.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotThrowExceptionsAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Do not throw exceptions - use Result<T> pattern instead";
	private static readonly LocalizableString MessageFormat = "Method throws exception '{0}' - use Result<T> pattern for functional error handling instead";
	private static readonly LocalizableString Description = "Exceptions should not be thrown in business logic. Use Result<T> pattern to represent success/failure states functionally. This improves composability, testability, and makes error paths explicit.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.DoNotThrowExceptions,
		Title,
		MessageFormat,
		DiagnosticCategories.FunctionalPatterns,
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

		context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
	}

	private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
	{
		var throwStatement = (ThrowStatementSyntax)context.Node;

		// Skip rethrow statements (throw; without expression)
		if (throwStatement.Expression == null)
		{
			return;
		}

		// Skip if we're in a catch block doing a proper rethrow with transformation
		if (IsInCatchBlockRethrow(throwStatement))
		{
			return;
		}

		// Get exception type name for reporting
		var exceptionType = GetExceptionTypeName(throwStatement.Expression);

		// Report diagnostic for exception throwing
		var diagnostic = Diagnostic.Create(
			Rule,
			throwStatement.GetLocation(),
			exceptionType);
		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsInCatchBlockRethrow(ThrowStatementSyntax throwStatement)
	{
		// Check if this throw is inside a catch block
		var catchClause = throwStatement.FirstAncestorOrSelf<CatchClauseSyntax>();
		if (catchClause == null)
		{
			return false;
		}

		// Allow rethrowing with transformation or wrapping
		// This is a simplified check - in practice you might want more sophisticated logic
		return false; // For now, flag all throws as we want Result<T> pattern
	}

	private static string GetExceptionTypeName(ExpressionSyntax expression) => expression switch
	{
		ObjectCreationExpressionSyntax objectCreation when objectCreation.Type != null =>
			objectCreation.Type.ToString(),
		ThrowExpressionSyntax throwExpression when throwExpression.Expression is ObjectCreationExpressionSyntax obj =>
			obj.Type?.ToString() ?? "Exception",
		_ => "Exception"
	};
}
