using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.ModernCSharp;

/// <summary>
/// Analyzer that enforces using modern pattern matching with declaration patterns.
/// Supports the modern C# coding standards.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseModernPatternMatchingAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use modern pattern matching with declaration patterns";
	private static readonly LocalizableString MessageFormat = "Use pattern matching with declaration instead of 'is' check followed by cast";
	private static readonly LocalizableString Description = "Modern pattern matching with declaration patterns (e.g., 'if (value is string str)') is more concise and safer than traditional 'is' checks followed by explicit casts.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseModernPatternMatching,
		Title,
		MessageFormat,
		DiagnosticCategories.CodeQuality,
		DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// TDD Green phase: Focus on if statements with 'is' expressions
		context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
	}

	private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
	{
		var ifStatement = (IfStatementSyntax)context.Node;

		// Check if condition is a simple 'is' expression without declaration pattern
		if (ifStatement.Condition is BinaryExpressionSyntax binaryExpression &&
			binaryExpression.IsKind(SyntaxKind.IsExpression) &&
			binaryExpression.Right is TypeSyntax)
		{
			// Check if the if block contains a cast of the same variable
			if (ContainsCastOfSameVariable(ifStatement.Statement, binaryExpression))
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					binaryExpression.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
		// Also check for else-if chains with the same pattern
		else if (ifStatement.Else?.Statement is IfStatementSyntax elseIfStatement)
		{
			// Recursively analyze the else-if statement
			AnalyzeIfStatementForPattern(elseIfStatement, context.ReportDiagnostic);
		}
	}

	private static void AnalyzeIfStatementForPattern(IfStatementSyntax ifStatement, Action<Diagnostic> reportDiagnostic)
	{
		// Check if condition is a simple 'is' expression without declaration pattern
		if (ifStatement.Condition is BinaryExpressionSyntax binaryExpression &&
			binaryExpression.IsKind(SyntaxKind.IsExpression) &&
			binaryExpression.Right is TypeSyntax)
		{
			// Check if the if block contains a cast of the same variable
			if (ContainsCastOfSameVariable(ifStatement.Statement, binaryExpression))
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					binaryExpression.GetLocation());
				reportDiagnostic(diagnostic);
			}
		}
	}

	private static bool ContainsCastOfSameVariable(StatementSyntax statement, BinaryExpressionSyntax isExpression)
	{
		// Get the variable being checked in the 'is' expression
		var checkedVariable = isExpression.Left.ToString();
		var targetType = isExpression.Right.ToString();

		// Look for casts in the statement block
		if (statement is BlockSyntax block)
		{
			return BlockContainsCast(block, checkedVariable, targetType);
		}
		else if (statement is ReturnStatementSyntax returnStatement)
		{
			return ExpressionContainsCast(returnStatement.Expression, checkedVariable, targetType);
		}

		return false;
	}

	private static bool BlockContainsCast(BlockSyntax block, string variableName, string targetType)
	{
		foreach (var statement in block.Statements)
		{
			if (statement is ReturnStatementSyntax returnStatement &&
				returnStatement.Expression != null)
			{
				if (ExpressionContainsCast(returnStatement.Expression, variableName, targetType))
					return true;
			}
			else if (statement is ExpressionStatementSyntax expressionStatement)
			{
				if (ExpressionContainsCast(expressionStatement.Expression, variableName, targetType))
					return true;
			}
			else if (statement is LocalDeclarationStatementSyntax localDeclaration)
			{
				// Check for patterns like "var u = (User)user"
				foreach (var variable in localDeclaration.Declaration.Variables)
				{
					if (variable.Initializer?.Value != null)
					{
						if (ExpressionContainsCast(variable.Initializer.Value, variableName, targetType))
							return true;
					}
				}
			}
		}

		return false;
	}

	private static bool ExpressionContainsCast(ExpressionSyntax? expression, string variableName, string targetType)
	{
		if (expression == null)
			return false;

		var expressionText = expression.ToString();

		// Look for cast patterns like ((string)value) or (string)value or ((int)value)
		var castPattern1 = $"(({targetType}){variableName})";
		var castPattern2 = $"({targetType}){variableName}";

		if (expressionText.Contains(castPattern1) || expressionText.Contains(castPattern2))
			return true;

		// Look for cast expressions like ((string)value) or (string)value
		if (expression is CastExpressionSyntax castExpression)
		{
			var castType = castExpression.Type.ToString();
			var castVariable = castExpression.Expression.ToString();

			return castType == targetType && castVariable == variableName;
		}

		// Look for casts in member access expressions like ((string)value).ToUpper()
		if (expression is MemberAccessExpressionSyntax memberAccess)
		{
			return ExpressionContainsCast(memberAccess.Expression, variableName, targetType);
		}

		// Look for casts in invocation expressions like ((string)value).ToUpper()
		if (expression is InvocationExpressionSyntax invocation)
		{
			return ExpressionContainsCast(invocation.Expression, variableName, targetType);
		}

		return false;
	}
}