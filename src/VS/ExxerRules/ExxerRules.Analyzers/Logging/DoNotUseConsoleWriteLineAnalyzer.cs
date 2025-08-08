using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Logging;

/// <summary>
/// Analyzer that enforces not using Console.WriteLine in production code.
/// Supports the "use structured logging" principle by preventing console output.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotUseConsoleWriteLineAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Do not use Console.WriteLine in production code";
	private static readonly LocalizableString MessageFormat = "Do not use {0} in production code - use structured logging instead";
	private static readonly LocalizableString Description = "Console.WriteLine and Console.Write should not be used in production code. Use structured logging with ILogger instead for better observability, configuration, and performance.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.DoNotUseConsoleWriteLine,
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

		// Check if this is a Console.WriteLine or Console.Write call
		if (!IsConsoleWriteCall(invocation, context.SemanticModel))
		{
			return;
		}

		// Get the method name for reporting
		var methodName = GetConsoleMethodName(invocation);

		// Report diagnostic for Console write usage
		var diagnostic = Diagnostic.Create(
			Rule,
			invocation.GetLocation(),
			methodName);
		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsConsoleWriteCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		// Check if it's a member access on Console
		if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
		{
			return false;
		}

		// Check if the method is WriteLine, Write, or similar
		var methodName = memberAccess.Name.Identifier.ValueText;
		if (!IsConsoleWriteMethodName(methodName))
		{
			return false;
		}

		// Check if the receiver is Console (syntactic check first)
		if (memberAccess.Expression is not IdentifierNameSyntax identifierName)
		{
			return false;
		}

		if (identifierName.Identifier.ValueText != "Console")
		{
			return false;
		}

		// Try to verify it's the System.Console type through semantic model
		// If semantic model fails, we'll rely on the syntactic check
		try
		{
			var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
			if (symbolInfo.Symbol is INamedTypeSymbol namedTypeSymbol)
			{
				return IsSystemConsole(namedTypeSymbol);
			}
		}
		catch
		{
			// Semantic model failed, fall back to syntactic check
		}

		// If semantic analysis fails, assume it's System.Console based on name
		// This is less precise but works when semantic model is incomplete
		return true;
	}

	private static bool IsConsoleWriteMethodName(string methodName)
	{
		// Console methods that should be avoided in production code
		var consoleMethods = new[]
		{
			"WriteLine", "Write", "Error", "Out"
		};

		return consoleMethods.Contains(methodName);
	}

	private static bool IsSystemConsole(INamedTypeSymbol typeSymbol) =>
		// Check if it's System.Console
		typeSymbol.Name == "Console" &&
			   GetFullNamespace(typeSymbol.ContainingNamespace) == "System";

	private static string GetFullNamespace(INamespaceSymbol? namespaceSymbol)
	{
		if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
		{
			return string.Empty;
		}

		var parts = new List<string>();
		var current = namespaceSymbol;

		while (current != null && !current.IsGlobalNamespace)
		{
			parts.Insert(0, current.Name);
			current = current.ContainingNamespace;
		}

		return string.Join(".", parts);
	}

	private static string GetConsoleMethodName(InvocationExpressionSyntax invocation)
	{
		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			return $"Console.{memberAccess.Name.Identifier.ValueText}";
		}

		return "Console method";
	}
}
