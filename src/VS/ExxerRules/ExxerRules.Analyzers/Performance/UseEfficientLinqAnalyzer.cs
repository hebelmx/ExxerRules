using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

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
			else if (statement is ExpressionStatementSyntax expressionStatement)
			{
				AnalyzeExpressionForLinqInefficiencies(context, expressionStatement.Expression);
			}
			else if (statement is LocalDeclarationStatementSyntax localDeclaration)
			{
				// Check for patterns like: var activeUsers = users.Where(u => u.IsActive);
				foreach (var variable in localDeclaration.Declaration.Variables)
				{
					if (variable.Initializer?.Value != null)
					{
						AnalyzeExpressionForLinqInefficiencies(context, variable.Initializer.Value);
					}
				}
			}
		}

		// Check for multiple LINQ operations on the same collection
		CheckForMultipleLinqOperations(context, block);
	}

	private static void CheckForMultipleLinqOperations(SyntaxNodeAnalysisContext context, BlockSyntax block)
	{
		// Look for patterns like: var activeUsers = users.Where(u => u.IsActive); var count = activeUsers.Count();
		var variableDeclarations = new List<(string VariableName, string Collection, Location Location)>();
		
		// Collect variable declarations that might be LINQ queries
		foreach (var statement in block.Statements)
		{
			if (statement is LocalDeclarationStatementSyntax localDeclaration)
			{
				foreach (var variable in localDeclaration.Declaration.Variables)
				{
					if (variable.Initializer?.Value != null)
					{
						var collection = ExtractCollectionFromExpression(variable.Initializer.Value);
						if (!string.IsNullOrEmpty(collection))
						{
							variableDeclarations.Add((variable.Identifier.ValueText, collection!, variable.GetLocation()));
						}
					}
				}
			}
		}

		// Check for usage of these variables in subsequent operations
		foreach (var declaration in variableDeclarations)
		{
			var variableName = declaration.VariableName;
			var collection = declaration.Collection;
			
			// Look for subsequent operations on this variable
			var operations = new List<string>();
			foreach (var statement in block.Statements)
			{
				CollectOperationsOnVariable(statement, variableName, operations);
			}
			
			// If we have multiple operations on the same variable, it might be inefficient
			if (operations.Count > 1)
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					declaration.Location,
					$"Multiple operations on LINQ query '{variableName}'");
				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	private static string? ExtractCollectionFromExpression(ExpressionSyntax expression)
	{
		if (expression is InvocationExpressionSyntax invocation &&
			invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			var methodName = memberAccess.Name.Identifier.ValueText;
			if (IsLinqMethod(methodName))
			{
				return memberAccess.Expression.ToString();
			}
		}
		return null;
	}

	private static void CollectOperationsOnVariable(StatementSyntax statement, string variableName, List<string> operations)
	{
		if (statement is ExpressionStatementSyntax expressionStatement)
		{
			if (expressionStatement.Expression is InvocationExpressionSyntax invocation &&
				invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				if (memberAccess.Expression.ToString() == variableName)
				{
					operations.Add(memberAccess.Name.Identifier.ValueText);
				}
			}
		}
		else if (statement is LocalDeclarationStatementSyntax localDeclaration)
		{
			foreach (var variable in localDeclaration.Declaration.Variables)
			{
				if (variable.Initializer?.Value is InvocationExpressionSyntax invocation &&
					invocation.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					if (memberAccess.Expression.ToString() == variableName)
					{
						operations.Add(memberAccess.Name.Identifier.ValueText);
					}
				}
			}
		}
	}

	private static bool HasMultipleEnumerationsOfSameQuery(List<(string Collection, string Method, Location Location)> operations)
	{
		// This is a simplified check - in a real implementation, you'd need to analyze the query structure
		// For now, we'll flag if we have multiple operations that could cause multiple enumerations
		var enumerationMethods = new[] { "Count", "Any", "First", "FirstOrDefault", "Last", "LastOrDefault" };
		
		var hasEnumerationMethods = operations.Any(op => enumerationMethods.Contains(op.Method));
		var hasMultipleOperations = operations.Count > 1;
		
		return hasEnumerationMethods && hasMultipleOperations;
	}

	private static void CollectLinqOperations(StatementSyntax statement, List<(string, string, Location)> operations)
	{
		if (statement is ExpressionStatementSyntax expressionStatement)
		{
			CollectLinqOperationsFromExpression(expressionStatement.Expression, operations);
		}
		else if (statement is LocalDeclarationStatementSyntax localDeclaration)
		{
			foreach (var variable in localDeclaration.Declaration.Variables)
			{
				if (variable.Initializer?.Value != null)
				{
					CollectLinqOperationsFromExpression(variable.Initializer.Value, operations);
				}
			}
		}
	}

	private static void CollectLinqOperationsFromExpression(ExpressionSyntax expression, List<(string, string, Location)> operations)
	{
		if (expression is InvocationExpressionSyntax invocation &&
			invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			var methodName = memberAccess.Name.Identifier.ValueText;
			if (IsLinqMethod(methodName))
			{
				var collection = memberAccess.Expression.ToString();
				operations.Add((collection, methodName, expression.GetLocation()));
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

	private static bool IsMaterializedOperation(string methodName)
	{
		// Operations that materialize the collection
		var materializedMethods = new[]
		{
			"ToList", "ToArray", "ToDictionary", "ToLookup", "ToHashSet"
		};

		return materializedMethods.Contains(methodName);
	}
}