using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ExxerRules.Analyzers.Common;
using FluentResults;

namespace ExxerRules.Analyzers.Logging;

/// <summary>
/// Analyzer that enforces structured logging instead of string concatenation.
/// Supports the "use structured logging" principle for better observability.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseStructuredLoggingAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use structured logging instead of string concatenation";
	private static readonly LocalizableString MessageFormat = "Use structured logging with named parameters instead of {0}";
	private static readonly LocalizableString Description = "Structured logging improves observability, searchability, and performance. Use named parameters like logger.LogInformation(\"User {UserId} logged in\", userId) instead of string concatenation or interpolation.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseStructuredLogging,
		Title,
		MessageFormat,
		DiagnosticCategories.Logging,
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

		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		// Check if this is a logging method call
		if (!IsLoggingMethodCall(invocation, context.SemanticModel))
			return;

		// Get the first argument (the message template)
		var arguments = invocation.ArgumentList.Arguments;
		if (arguments.Count == 0)
			return;

		var messageArgument = arguments[0].Expression;

		// Check if the message uses string concatenation or interpolation
		if (messageArgument is BinaryExpressionSyntax binaryExpr && IsStringConcatenation(binaryExpr))
		{
			ReportDiagnostic(context, messageArgument, "string concatenation");
		}
		else if (messageArgument is InterpolatedStringExpressionSyntax)
		{
			ReportDiagnostic(context, messageArgument, "string interpolation");
		}
	}

	private static bool IsLoggingMethodCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		// Check if the method is called on ILogger<T> or ILogger
		var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
		if (memberAccess == null)
			return false;

		var methodName = memberAccess.Name.Identifier.ValueText;
		
		// Check if it's a logging method
		if (!IsLoggingMethodName(methodName))
			return false;

		// Check if the receiver is an ILogger
		var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
		if (receiverType == null)
			return false;

		return IsLoggerType(receiverType);
	}

	private static bool IsLoggingMethodName(string methodName)
	{
		// Common logging method names
		var loggingMethods = new[]
		{
			"LogTrace", "LogDebug", "LogInformation", "LogWarning", 
			"LogError", "LogCritical", "Log"
		};

		return loggingMethods.Contains(methodName);
	}

	private static bool IsLoggerType(ITypeSymbol typeSymbol)
	{
		// Check if it's ILogger or ILogger<T>
		if (typeSymbol.Name == "ILogger")
		{
			var containingNamespace = GetFullNamespace(typeSymbol.ContainingNamespace);
			return containingNamespace == "Microsoft.Extensions.Logging";
		}

		return false;
	}

	private static string GetFullNamespace(INamespaceSymbol? namespaceSymbol)
	{
		if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
			return string.Empty;

		var parts = new List<string>();
		var current = namespaceSymbol;

		while (current != null && !current.IsGlobalNamespace)
		{
			parts.Insert(0, current.Name);
			current = current.ContainingNamespace;
		}

		return string.Join(".", parts);
	}

	private static bool IsStringConcatenation(BinaryExpressionSyntax binaryExpression)
	{
		// Check if it's a + operator with string operands
		if (!binaryExpression.OperatorToken.IsKind(SyntaxKind.PlusToken))
			return false;

		// Recursively check for string concatenation patterns
		return ContainsStringConcatenation(binaryExpression);
	}

	private static bool ContainsStringConcatenation(ExpressionSyntax expression)
	{
		return expression switch
		{
			BinaryExpressionSyntax binaryExpr when binaryExpr.OperatorToken.IsKind(SyntaxKind.PlusToken) => true,
			LiteralExpressionSyntax literal when literal.Token.IsKind(SyntaxKind.StringLiteralToken) => false,
			_ => false
		};
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode node, string violationType)
	{
		var diagnostic = Diagnostic.Create(
			Rule,
			node.GetLocation(),
			violationType);
		context.ReportDiagnostic(diagnostic);
	}
}