using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ExxerRules.Analyzers.Common;
using FluentResults;

namespace ExxerRules.Analyzers.NullSafety;

/// <summary>
/// Analyzer that enforces null parameter validation at method entry points.
/// Supports the fail-safe defaults and defensive programming principles.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ValidateNullParametersAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Validate null parameters at method entry";
	private static readonly LocalizableString MessageFormat = "Method '{0}' should validate null parameters at method entry for parameter(s): {1}";
	private static readonly LocalizableString Description = "Methods should validate reference type parameters for null values at the method entry point, following fail-safe defaults and defensive programming principles.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.ValidateNullParameters,
		Title,
		MessageFormat,
		DiagnosticCategories.NullSafety,
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

		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var methodDeclaration = (MethodDeclarationSyntax)context.Node;

		// Skip if this is a method that should be exempted
		if (IsSkippableMethod(methodDeclaration))
			return;

		// Get reference type parameters that need validation
		var referenceParameters = GetReferenceTypeParameters(methodDeclaration, context.SemanticModel);
		if (!referenceParameters.Any())
			return;

		// Check if method has null validation for each reference parameter
		var unvalidatedParameters = GetUnvalidatedReferenceParameters(methodDeclaration, referenceParameters);
		
		if (unvalidatedParameters.Any())
		{
			var parameterNames = string.Join(", ", unvalidatedParameters);
			var diagnostic = Diagnostic.Create(
				Rule,
				methodDeclaration.Identifier.GetLocation(),
				methodDeclaration.Identifier.Text,
				parameterNames);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool IsSkippableMethod(MethodDeclarationSyntax method)
	{
		// Skip constructors, destructors, and event handlers
		if (method.Identifier.Text.StartsWith("On") && method.Modifiers.Any(SyntaxKind.ProtectedKeyword))
			return true;

		// Skip Main method
		if (method.Identifier.Text == "Main")
			return true;

		// Skip interface methods (they don't have bodies)
		if (method.Body == null && method.ExpressionBody == null)
			return true;

		return false;
	}

	private static List<string> GetReferenceTypeParameters(MethodDeclarationSyntax method, SemanticModel semanticModel)
	{
		var referenceParams = new List<string>();

		foreach (var parameter in method.ParameterList.Parameters)
		{
			var parameterType = semanticModel.GetTypeInfo(parameter.Type!).Type;
			
			// Check if it's a reference type (not value type and not nullable value type)
			// Also check for string, object, and other common reference types
			if (parameterType != null && 
				(parameterType.IsReferenceType || 
				 parameterType.ToString() == "string" ||
				 parameterType.ToString() == "object" ||
				 parameterType.ToString().Contains("String") ||
				 parameterType.ToString().Contains("Object")))
			{
				referenceParams.Add(parameter.Identifier.ValueText);
			}
		}

		return referenceParams;
	}

	private static List<string> GetUnvalidatedReferenceParameters(MethodDeclarationSyntax method, List<string> referenceParameters)
	{
		var unvalidated = new List<string>(referenceParameters);

		// Get method body statements
		var statements = GetMethodStatements(method);
		if (!statements.Any())
			return unvalidated;

		// Look for null validation patterns in all statements
		var firstStatements = statements;

		foreach (var statement in firstStatements)
		{
			var validatedParameter = FindValidatedParameter(statement, referenceParameters);
			if (!string.IsNullOrEmpty(validatedParameter))
			{
				unvalidated.Remove(validatedParameter!);
			}
		}

		return unvalidated;
	}

	private static IEnumerable<StatementSyntax> GetMethodStatements(MethodDeclarationSyntax method)
	{
		if (method.Body != null)
		{
			return method.Body.Statements;
		}

		// For expression-bodied methods, we can't easily validate null parameters
		// So we consider them as not having validation
		return Enumerable.Empty<StatementSyntax>();
	}

	private static string? FindValidatedParameter(StatementSyntax statement, List<string> referenceParameters)
	{
		// Look for patterns like:
		// if (parameter == null) return Result.Fail(...);
		// if (parameter is null) return Result.Fail(...);
		// ArgumentNullException.ThrowIfNull(parameter);

		if (statement is IfStatementSyntax ifStatement)
		{
			var condition = ifStatement.Condition;
			
			// Handle binary expressions like "parameter == null" or "parameter is null"
			if (condition is BinaryExpressionSyntax binaryExpr)
			{
				var leftIdentifier = GetIdentifierFromExpression(binaryExpr.Left);
				var rightIdentifier = GetIdentifierFromExpression(binaryExpr.Right);

				// Check if one side is a parameter and the other is null
				if (IsNullLiteral(binaryExpr.Right) && referenceParameters.Contains(leftIdentifier))
				{
					// Check if the if statement body contains appropriate validation
					if (HasAppropriateValidation(ifStatement))
					{
						return leftIdentifier;
					}
				}
				if (IsNullLiteral(binaryExpr.Left) && referenceParameters.Contains(rightIdentifier))
				{
					// Check if the if statement body contains appropriate validation
					if (HasAppropriateValidation(ifStatement))
					{
						return rightIdentifier;
					}
				}
			}

			// Handle "is" patterns like "parameter is null"
			if (condition is IsPatternExpressionSyntax isPattern)
			{
				var identifier = GetIdentifierFromExpression(isPattern.Expression);
				if (referenceParameters.Contains(identifier) && IsNullPattern(isPattern.Pattern))
				{
					// Check if the if statement body contains appropriate validation
					if (HasAppropriateValidation(ifStatement))
					{
						return identifier;
					}
				}
			}
		}

		// Look for expression statements like ArgumentNullException.ThrowIfNull(parameter)
		if (statement is ExpressionStatementSyntax exprStatement &&
			exprStatement.Expression is InvocationExpressionSyntax invocation)
		{
			// Check for ThrowIfNull patterns
			var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
			if (memberAccess?.Name.Identifier.ValueText == "ThrowIfNull" &&
				invocation.ArgumentList.Arguments.Count > 0)
			{
				var argument = invocation.ArgumentList.Arguments[0].Expression;
				var identifier = GetIdentifierFromExpression(argument);
				if (referenceParameters.Contains(identifier))
				{
					return identifier;
				}
			}
		}

		return null;
	}

	private static bool HasAppropriateValidation(IfStatementSyntax ifStatement)
	{
		// Check if the if statement body contains appropriate validation patterns
		// Look for: throw new ArgumentNullException(nameof(parameter))
		// or: return Result.Fail(...)
		// or: ArgumentNullException.ThrowIfNull(parameter)
		
		if (ifStatement.Statement is BlockSyntax block)
		{
			foreach (var statement in block.Statements)
			{
				if (IsValidValidationStatement(statement))
					return true;
			}
		}
		else if (IsValidValidationStatement(ifStatement.Statement))
		{
			return true;
		}
		
		return false;
	}

	private static bool IsValidValidationStatement(StatementSyntax statement)
	{
		// Check for throw new ArgumentNullException(nameof(parameter))
		if (statement is ThrowStatementSyntax throwStatement &&
			throwStatement.Expression is ObjectCreationExpressionSyntax objectCreation &&
			objectCreation.Type.ToString().Contains("ArgumentNullException"))
		{
			return true;
		}
		
		// Check for return Result.Fail(...)
		if (statement is ReturnStatementSyntax returnStatement &&
			returnStatement.Expression is InvocationExpressionSyntax invocation &&
			invocation.Expression.ToString().Contains("Result.Fail"))
		{
			return true;
		}
		
		// Check for ArgumentNullException.ThrowIfNull(parameter)
		if (statement is ExpressionStatementSyntax exprStatement &&
			exprStatement.Expression is InvocationExpressionSyntax invocationExpr &&
			invocationExpr.Expression.ToString().Contains("ArgumentNullException.ThrowIfNull"))
		{
			return true;
		}
		
		// Check for any throw statement (more permissive)
		if (statement is ThrowStatementSyntax)
		{
			return true;
		}
		
		return false;
	}

	private static string GetIdentifierFromExpression(ExpressionSyntax expression)
	{
		return expression switch
		{
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			_ => string.Empty
		};
	}

	private static bool IsNullLiteral(ExpressionSyntax expression)
	{
		return expression is LiteralExpressionSyntax literal &&
			   literal.Token.IsKind(SyntaxKind.NullKeyword);
	}

	private static bool IsNullPattern(PatternSyntax pattern)
	{
		return pattern is ConstantPatternSyntax constantPattern &&
			   constantPattern.Expression is LiteralExpressionSyntax literal &&
			   literal.Token.IsKind(SyntaxKind.NullKeyword);
	}
}