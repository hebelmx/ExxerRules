using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.ModernCSharp;

/// <summary>
/// Analyzer that enforces using expression-bodied members where appropriate.
/// Supports the modern C# coding standards.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseExpressionBodiedMembersAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use expression-bodied members where appropriate";
	private static readonly LocalizableString MessageFormat = "Member '{0}' can be simplified to an expression-bodied member";
	private static readonly LocalizableString Description = "Expression-bodied members provide a more concise syntax for simple methods and properties, improving code readability and reducing boilerplate.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseExpressionBodiedMembers,
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

		// TDD Green phase: Focus on methods and properties that can be expression-bodied
		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var method = (MethodDeclarationSyntax)context.Node;

		// Skip if already expression-bodied
		if (method.ExpressionBody != null)
		{
			return;
		}

		// Check if method has a simple single-return body
		if (method.Body != null && CanBeExpressionBodied(method.Body))
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				method.Identifier.GetLocation(),
				method.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
	{
		var property = (PropertyDeclarationSyntax)context.Node;

		// Skip if already expression-bodied
		if (property.ExpressionBody != null)
		{
			return;
		}

		// Check getter accessors that can be expression-bodied
		if (property.AccessorList != null)
		{
			foreach (var accessor in property.AccessorList.Accessors)
			{
				if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration) &&
					accessor.Body != null &&
					CanBeExpressionBodied(accessor.Body))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						property.Identifier.GetLocation(),
						property.Identifier.ValueText);
					context.ReportDiagnostic(diagnostic);
					break; // Only report once per property
				}
			}
		}
	}

	private static bool CanBeExpressionBodied(BlockSyntax body)
	{
		// Check if body contains only a single return statement
		if (body.Statements.Count != 1)
		{
			return false;
		}

		// Must be a return statement
		if (body.Statements[0] is not ReturnStatementSyntax returnStatement)
		{
			return false;
		}

		// Must have an expression to return
		if (returnStatement.Expression == null)
		{
			return false;
		}

		// Simple heuristic: expression should be reasonably simple
		// Avoid very complex expressions that would hurt readability
		var expressionText = returnStatement.Expression.ToString();
		return expressionText.Length < 100; // Arbitrary threshold for simplicity
	}
}
