using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.ErrorHandling;

/// <summary>
/// Analyzer that detects direct exception throwing in code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AvoidThrowingExceptionsAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Avoid throwing exceptions";
	private static readonly LocalizableString MessageFormat = "Throwing '{0}' detected. Use Result<T> pattern instead.";
	private static readonly LocalizableString Description = "Exceptions should be avoided in favor of the Result<T> pattern for better functional programming and error handling.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.AvoidThrowingExceptions,
		Title,
		MessageFormat,
		DiagnosticCategories.ErrorHandling,
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
		context.RegisterSyntaxNodeAction(AnalyzeThrowExpression, SyntaxKind.ThrowExpression);
	}

	private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
	{
		var throwStatement = (ThrowStatementSyntax)context.Node;

		// Skip if in catch block and just rethrowing
		if (throwStatement.Expression == null && throwStatement.Parent is BlockSyntax block && 
			block.Parent is CatchClauseSyntax)
		{
			return;
		}

		// Skip if in a method that's specifically for exception handling (e.g., ThrowHelper methods)
		var containingMethod = throwStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
		if (containingMethod != null && 
			(containingMethod.Identifier.Text.Contains("Throw") || 
			 containingMethod.Identifier.Text.Contains("Exception")))
		{
			return;
		}

		var exceptionType = GetExceptionType(throwStatement.Expression, context.SemanticModel);
		var diagnostic = Diagnostic.Create(
			Rule,
			throwStatement.GetLocation(),
			exceptionType);

		context.ReportDiagnostic(diagnostic);
	}

	private static void AnalyzeThrowExpression(SyntaxNodeAnalysisContext context)
	{
		var throwExpression = (ThrowExpressionSyntax)context.Node;

		// Skip if in a method that's specifically for exception handling
		var containingMethod = throwExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
		if (containingMethod != null && 
			(containingMethod.Identifier.Text.Contains("Throw") || 
			 containingMethod.Identifier.Text.Contains("Exception")))
		{
			return;
		}

		var exceptionType = GetExceptionType(throwExpression.Expression, context.SemanticModel);
		var diagnostic = Diagnostic.Create(
			Rule,
			throwExpression.GetLocation(),
			exceptionType);

		context.ReportDiagnostic(diagnostic);
	}

	private static string GetExceptionType(ExpressionSyntax? expression, SemanticModel semanticModel)
	{
		if (expression == null)
			return "exception";

		var typeInfo = semanticModel.GetTypeInfo(expression);
		if (typeInfo.Type != null)
		{
			return typeInfo.Type.Name;
		}

		// Try to get type from object creation
		if (expression is ObjectCreationExpressionSyntax objectCreation)
		{
			return objectCreation.Type.ToString();
		}

		return "exception";
	}
}