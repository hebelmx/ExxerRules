using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.ErrorHandling;

/// <summary>
/// Analyzer that detects methods throwing exceptions instead of returning Result&lt;T&gt;.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseResultPatternAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use Result<T> pattern instead of throwing exceptions";
	private static readonly LocalizableString MessageFormat = "Method '{0}' throws exceptions but should return Result<T>";
	private static readonly LocalizableString Description = "Methods should return Result<T> for error handling instead of throwing exceptions, following functional programming principles.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseResultPattern,
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

		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
		context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var methodDeclaration = (MethodDeclarationSyntax)context.Node;

		// Skip if method already returns Result<T>
		if (IsResultReturnType(methodDeclaration.ReturnType))
		{
			return;
		}

		// Skip if this is a method that should be exempted
		if (IsSkippableMethod(methodDeclaration))
		{
			return;
		}

		// Check for throw statements and expressions using functional approach
		var throwStatements = methodDeclaration.DescendantNodes().OfType<ThrowStatementSyntax>().ToList();
		var throwExpressions = methodDeclaration.DescendantNodes().OfType<ThrowExpressionSyntax>().ToList();

		if (throwStatements.Any() || throwExpressions.Any())
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				methodDeclaration.Identifier.GetLocation(),
				methodDeclaration.Identifier.Text);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool IsSkippableMethod(MethodDeclarationSyntax method)
	{
		// Skip constructors, destructors, and event handlers
		if (method.Identifier.Text.StartsWith("On") && method.Modifiers.Any(SyntaxKind.ProtectedKeyword))
		{
			return true;
		}

		// Skip Main method
		if (method.Identifier.Text == "Main")
		{
			return true;
		}

		return false;
	}

	private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
	{
		var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

		// Skip if property already returns Result<T>
		if (IsResultReturnType(propertyDeclaration.Type))
		{
			return;
		}

		// Check accessors for throw statements using functional approach
		var accessorThrows = CheckAccessorsForThrows(propertyDeclaration);
		var expressionThrows = CheckExpressionBodyForThrows(propertyDeclaration);

		if (accessorThrows || expressionThrows)
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				propertyDeclaration.Identifier.GetLocation(),
				propertyDeclaration.Identifier.Text);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool CheckAccessorsForThrows(PropertyDeclarationSyntax property)
	{
		if (property.AccessorList == null)
		{
			return false;
		}

		return property.AccessorList.Accessors.Any(accessor =>
			accessor.DescendantNodes().OfType<ThrowStatementSyntax>().Any() ||
			accessor.DescendantNodes().OfType<ThrowExpressionSyntax>().Any());
	}

	private static bool CheckExpressionBodyForThrows(PropertyDeclarationSyntax property) => property.ExpressionBody?.DescendantNodes().OfType<ThrowExpressionSyntax>().Any() == true;

	private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
	{
		var localFunction = (LocalFunctionStatementSyntax)context.Node;

		// Skip if function already returns Result<T>
		if (IsResultReturnType(localFunction.ReturnType))
		{
			return;
		}

		// Check for throw statements and expressions
		var throwStatements = localFunction.DescendantNodes().OfType<ThrowStatementSyntax>().ToList();
		var throwExpressions = localFunction.DescendantNodes().OfType<ThrowExpressionSyntax>().ToList();

		if (throwStatements.Any() || throwExpressions.Any())
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				localFunction.Identifier.GetLocation(),
				localFunction.Identifier.Text);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
	{
		LambdaExpressionSyntax? lambda = context.Node switch
		{
			SimpleLambdaExpressionSyntax simple => simple,
			ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized,
			_ => null
		};

		if (lambda == null)
		{
			return;
		}

		// Check for throw statements and expressions
		var throwStatements = lambda.DescendantNodes().OfType<ThrowStatementSyntax>().ToList();
		var throwExpressions = lambda.DescendantNodes().OfType<ThrowExpressionSyntax>().ToList();

		if (throwStatements.Any() || throwExpressions.Any())
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				lambda.GetLocation(),
				"Lambda expression");
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool IsResultReturnType(TypeSyntax? typeSyntax)
	{
		if (typeSyntax == null)
		{
			return false;
		}

		// Check for Result<T> or Task<Result<T>>
		var typeText = typeSyntax.ToString();
		return typeText.Contains("Result<") || typeText.Contains("Result ");
	}
}
