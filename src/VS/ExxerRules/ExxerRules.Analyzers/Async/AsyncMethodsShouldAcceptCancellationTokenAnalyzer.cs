using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Async;

/// <summary>
/// Analyzer that enforces CancellationToken parameters in async methods.
/// Supports graceful cancellation and fail-safe defaults principles.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncMethodsShouldAcceptCancellationTokenAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Async methods should accept CancellationToken";
	private static readonly LocalizableString MessageFormat = "Async method '{0}' should accept a CancellationToken parameter to support graceful cancellation";
	private static readonly LocalizableString Description = "Async methods should accept a CancellationToken parameter to enable graceful cancellation and prevent unresponsive applications, following fail-safe defaults principles.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.AsyncMethodsShouldAcceptCancellationToken,
		Title,
		MessageFormat,
		DiagnosticCategories.Async,
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

		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var methodDeclaration = (MethodDeclarationSyntax)context.Node;

		// Only analyze async methods
		if (!IsAsyncMethod(methodDeclaration))
		{
			return;
		}

		// Skip async void methods (typically event handlers)
		if (IsAsyncVoidMethod(methodDeclaration))
		{
			return;
		}

		// Skip if this is a method that should be exempted
		if (IsSkippableMethod(methodDeclaration))
		{
			return;
		}

		// Check if method already has CancellationToken parameter
		if (HasCancellationTokenParameter(methodDeclaration, context.SemanticModel))
		{
			return;
		}

		// Report diagnostic for missing CancellationToken
		var diagnostic = Diagnostic.Create(
			Rule,
			methodDeclaration.Identifier.GetLocation(),
			methodDeclaration.Identifier.Text);
		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsAsyncMethod(MethodDeclarationSyntax method) =>
		// Check if method has async modifier
		method.Modifiers.Any(SyntaxKind.AsyncKeyword);

	private static bool IsAsyncVoidMethod(MethodDeclarationSyntax method) =>
		// Check if return type is void (async void methods are typically event handlers)
		method.ReturnType is PredefinedTypeSyntax predefined &&
			   predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);

	private static bool IsSkippableMethod(MethodDeclarationSyntax method)
	{
		// Skip Main method
		if (method.Identifier.Text == "Main")
		{
			return true;
		}

		// Skip interface methods (they don't have bodies to implement cancellation)
		if (method.Body == null && method.ExpressionBody == null)
		{
			return true;
		}

		// Skip methods that look like event handlers by naming convention
		var methodName = method.Identifier.Text;
		if (methodName.Contains("_Click") ||
			methodName.Contains("_Changed") ||
			methodName.Contains("_Load") ||
			methodName.StartsWith("On"))
		{
			return true;
		}

		return false;
	}

	private static bool HasCancellationTokenParameter(MethodDeclarationSyntax method, SemanticModel semanticModel)
	{
		foreach (var parameter in method.ParameterList.Parameters)
		{
			var parameterType = semanticModel.GetTypeInfo(parameter.Type!).Type;

			// Check if parameter type is CancellationToken
			if (parameterType != null &&
				IsCancellationTokenType(parameterType))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsCancellationTokenType(ITypeSymbol typeSymbol)
	{
		// Check if type is System.Threading.CancellationToken
		if (typeSymbol.ContainingNamespace?.Name == "Threading" &&
			typeSymbol.ContainingNamespace.ContainingNamespace?.Name == "System" &&
			typeSymbol.Name == "CancellationToken")
		{
			return true;
		}

		// Check for fully qualified name
		var fullName = GetFullTypeName(typeSymbol);
		return fullName is "System.Threading.CancellationToken" or
			   "CancellationToken";
	}

	private static string GetFullTypeName(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
		{
			return string.Empty;
		}

		var parts = new List<string>();
		var current = typeSymbol;

		while (current != null)
		{
			parts.Insert(0, current.Name);
			current = current.ContainingType;
		}

		// Add namespace
		var namespaceParts = new List<string>();
		var namespaceSymbol = typeSymbol.ContainingNamespace;
		while (namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace)
		{
			namespaceParts.Insert(0, namespaceSymbol.Name);
			namespaceSymbol = namespaceSymbol.ContainingNamespace;
		}

		if (namespaceParts.Any())
		{
			return string.Join(".", namespaceParts.Concat(parts));
		}

		return string.Join(".", parts);
	}
}
