using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Performance;

/// <summary>
/// Analyzer that enforces efficient LINQ operations to avoid multiple enumerations.
/// Supports the performance optimization principles.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseEfficientLinqAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use efficient LINQ operations";
	private static readonly LocalizableString MessageFormat = "Inefficient LINQ usage detected: '{0}' - multiple enumerations on the same collection";
	private static readonly LocalizableString Description = "Avoid multiple enumerations of the same LINQ query. Cache results in variables or use more efficient LINQ methods to improve performance.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseEfficientLinq,
		Title,
		MessageFormat,
		DiagnosticCategories.Performance,
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

		// TDD Green phase: Focus on method bodies where LINQ inefficiencies can occur
		context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzePropertyBody, SyntaxKind.PropertyDeclaration);
	}

	private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
	{
		var method = (MethodDeclarationSyntax)context.Node;
		if (method.Body != null)
		{
			AnalyzeBlockForLinqInefficiencies(context, method.Body);
		}
		else if (method.ExpressionBody != null)
		{
			AnalyzeExpressionForLinqInefficiencies(context, method.ExpressionBody.Expression);
		}
	}

	private static void AnalyzePropertyBody(SyntaxNodeAnalysisContext context)
	{
		var property = (PropertyDeclarationSyntax)context.Node;
		if (property.ExpressionBody != null)
		{
			AnalyzeExpressionForLinqInefficiencies(context, property.ExpressionBody.Expression);
		}
		else if (property.AccessorList != null)
		{
			foreach (var accessor in property.AccessorList.Accessors)
			{
				if (accessor.Body != null)
				{
					AnalyzeBlockForLinqInefficiencies(context, accessor.Body);
				}
				else if (accessor.ExpressionBody != null)
				{
					AnalyzeExpressionForLinqInefficiencies(context, accessor.ExpressionBody.Expression);
				}
			}
		}
	}

	private static void AnalyzeBlockForLinqInefficiencies(SyntaxNodeAnalysisContext context, BlockSyntax block)
	{
		// Look for patterns like: return data.Any() && data.First() > 0;
		foreach (var statement in block.Statements)
		{
			if (statement is ReturnStatementSyntax returnStatement && returnStatement.Expression != null)
			{
				AnalyzeExpressionForLinqInefficiencies(context, returnStatement.Expression);
			}
		}
	}

	private static void AnalyzeExpressionForLinqInefficiencies(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
	{
		// Detect pattern: data.Any() && data.First() > 0 (multiple enumerations)
		if (expression is BinaryExpressionSyntax binaryExpression &&
			binaryExpression.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken))
		{
			var leftSideCollection = ExtractCollectionFromLinqCall(binaryExpression.Left);
			var rightSideCollection = ExtractCollectionFromLinqCall(binaryExpression.Right);

			if (leftSideCollection != null && rightSideCollection != null &&
				leftSideCollection == rightSideCollection)
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					expression.GetLocation(),
					"Multiple enumerations on same collection");
				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	private static string? ExtractCollectionFromLinqCall(ExpressionSyntax expression)
	{
		// Extract collection name from expressions like "data.Any()" or "data.First()"
		if (expression is InvocationExpressionSyntax invocation &&
			invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			var methodName = memberAccess.Name.Identifier.ValueText;
			if (IsLinqMethod(methodName))
			{
				return memberAccess.Expression.ToString();
			}
		}

		// Handle cases with comparisons like "data.First() > 0"
		if (expression is BinaryExpressionSyntax binaryExpr)
		{
			return ExtractCollectionFromLinqCall(binaryExpr.Left) ?? ExtractCollectionFromLinqCall(binaryExpr.Right);
		}

		return null;
	}

	private static bool IsLinqMethod(string methodName)
	{
		var linqMethods = new[]
		{
			"Any", "First", "FirstOrDefault", "Last", "LastOrDefault", 
			"Single", "SingleOrDefault", "Count", "Where", "Select", 
			"OrderBy", "OrderByDescending", "Take", "Skip"
		};

		return linqMethods.Contains(methodName);
	}
}