using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
		{
			return;
		}

		// Get reference type parameters that need validation
		var referenceParameters = GetReferenceTypeParameters(methodDeclaration, context.SemanticModel);
		if (!referenceParameters.Any())
		{
			return;
		}

		// Check if method has null validation for each reference parameter
		var unvalidatedParameters = GetUnvalidatedReferenceParameters(methodDeclaration, referenceParameters);

		// Report one diagnostic per unvalidated parameter
		foreach (var unvalidatedParameter in unvalidatedParameters)
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				methodDeclaration.Identifier.GetLocation(),
				methodDeclaration.Identifier.Text,
				unvalidatedParameter);
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

		// Skip interface methods (they don't have bodies)
		if (method.Body == null && method.ExpressionBody == null)
		{
			return true;
		}

		return false;
	}

	private static List<string> GetReferenceTypeParameters(MethodDeclarationSyntax method, SemanticModel semanticModel)
	{
		var referenceParams = new List<string>();

		foreach (var parameter in method.ParameterList.Parameters)
		{
			if (parameter.Type == null)
			{
				continue;
			}

			var typeName = parameter.Type.ToString();

			// Explicitly check for reference types by name
			if (typeName == "string" || typeName == "object" ||
				typeName.Contains("String") || typeName.Contains("Object") ||
				typeName.Contains("Exception") || typeName.Contains("Collection") ||
				typeName.Contains("List") || typeName.Contains("Dictionary") ||
				typeName.Contains("Array") || typeName.Contains("Enumerable"))
			{
				referenceParams.Add(parameter.Identifier.ValueText);
			}
			// Explicitly exclude common value types
			else if (typeName is "int" or "long" or "short" or
					 "byte" or "uint" or "ulong" or
					 "ushort" or "sbyte" or "float" or
					 "double" or "decimal" or "bool" or
					 "char" or "DateTime" or "Guid")
			{
				// Skip value types
				continue;
			}
			// For other types, try semantic model as fallback
			else
			{
				var parameterType = semanticModel.GetTypeInfo(parameter.Type).Type;
				if (parameterType != null && parameterType.IsReferenceType)
				{
					referenceParams.Add(parameter.Identifier.ValueText);
				}
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
		{
			return unvalidated;
		}

		// Look for null validation patterns in all statements
		foreach (var statement in statements)
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
		return [];
	}

	private static string? FindValidatedParameter(StatementSyntax statement, List<string> referenceParameters)
	{
		// Look for patterns like:
		// if (parameter == null) throw new ArgumentNullException(nameof(parameter));
		// if (parameter is null) throw new ArgumentNullException(nameof(parameter));
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
				{
					return true;
				}
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

	private static string GetIdentifierFromExpression(ExpressionSyntax expression) => expression switch
	{
		IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
		_ => string.Empty
	};

	private static bool IsNullLiteral(ExpressionSyntax expression) => expression is LiteralExpressionSyntax literal &&
			   literal.Token.IsKind(SyntaxKind.NullKeyword);

	private static bool IsNullPattern(PatternSyntax pattern) => pattern is ConstantPatternSyntax constantPattern &&
			   constantPattern.Expression is LiteralExpressionSyntax literal &&
			   literal.Token.IsKind(SyntaxKind.NullKeyword);
}
