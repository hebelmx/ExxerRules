using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that enforces using NSubstitute instead of Moq for mocking.
/// Supports the testing standards compliance principle.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotUseMoqAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use NSubstitute instead of Moq for mocking";
	private static readonly LocalizableString MessageFormat = "Moq usage detected: '{0}' - use NSubstitute for consistent testing patterns";
	private static readonly LocalizableString Description = "NSubstitute provides a cleaner, more readable mocking syntax than Moq. It's the preferred mocking framework for this project to ensure consistent testing patterns and better maintainability.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseNSubstitute,
		Title,
		MessageFormat,
		DiagnosticCategories.Testing,
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

		// TDD Green phase: Focus only on using directive detection for now
		context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
		// TODO: Add object creation and member access detection in refactor phase
	}



	private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
	{
		var usingDirective = (UsingDirectiveSyntax)context.Node;

		var nameString = usingDirective.Name?.ToString();

		// Exact match for Moq (not NSubstitute or other frameworks)
		if (nameString == "Moq")
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				usingDirective.GetLocation(),
				"using Moq");
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
	{
		var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

		// Check if creating Mock<T> instances (syntactic analysis first)
		if (objectCreation.Type is GenericNameSyntax genericName &&
			genericName.Identifier.ValueText == "Mock")
		{
			// For test scenarios, assume Mock<T> refers to Moq
			var diagnostic = Diagnostic.Create(
				Rule,
				objectCreation.GetLocation(),
				$"new {genericName}");
			context.ReportDiagnostic(diagnostic);
		}

		// Also check for non-generic Mock usage
		if (objectCreation.Type is IdentifierNameSyntax identifierName &&
			identifierName.Identifier.ValueText == "Mock")
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				objectCreation.GetLocation(),
				"new Mock");
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;

		// Check for Mock.* static method calls
		if (memberAccess.Expression is IdentifierNameSyntax identifier &&
			identifier.Identifier.ValueText == "Mock")
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				memberAccess.GetLocation(),
				$"Mock.{memberAccess.Name}");
			context.ReportDiagnostic(diagnostic);
		}
	}
}
