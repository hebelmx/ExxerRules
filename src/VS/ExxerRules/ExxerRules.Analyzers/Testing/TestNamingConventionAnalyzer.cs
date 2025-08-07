using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ExxerRules.Analyzers.Common;
using FluentResults;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that enforces test naming convention: Should_Action_When_Condition.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestNamingConventionAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Test methods should follow naming convention";
	private static readonly LocalizableString MessageFormat = "Test method '{0}' should follow naming convention: Should_Action_When_Condition";
	private static readonly LocalizableString Description = "Test methods should use descriptive names following the pattern Should_Action_When_Condition for better readability and maintainability.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.TestNamingConvention,
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

		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var methodDeclaration = (MethodDeclarationSyntax)context.Node;

		// Check if this is a test method using FluentResults pattern
		var testAttributeResult = PatternDetector.DetectTestAttributes(methodDeclaration, context.SemanticModel);
		if (testAttributeResult.IsFailed || !testAttributeResult.Value.HasTestAttributes)
			return;

		var methodName = methodDeclaration.Identifier.ValueText;

		// Validate naming convention using FluentResults pattern
		var namingValidationResult = PatternDetector.ValidateMethodNaming(
			methodName, 
			@"^Should_[A-Z][a-zA-Z0-9]*(_When_[A-Z][a-zA-Z0-9]*)?$");

		// Use the extension method to report diagnostic if validation failed
		context.ReportDiagnosticIfFalse(
			namingValidationResult,
			Rule,
			methodDeclaration.Identifier.GetLocation(),
			methodName);
	}

}