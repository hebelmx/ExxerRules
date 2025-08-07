using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ExxerRules.Analyzers.Common;
using FluentResults;

namespace ExxerRules.Analyzers.CodeQuality;

/// <summary>
/// Analyzer that enforces use of named constants instead of magic numbers and strings.
/// Supports the "avoid globals and hardcoding" principle by promoting named constants.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AvoidMagicNumbersAndStringsAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Avoid magic numbers and strings";
	private static readonly LocalizableString MessageFormat = "Magic {0} '{1}' should be replaced with a named constant";
	private static readonly LocalizableString Description = "Magic numbers and strings should be replaced with named constants to improve code readability, maintainability, and reduce the risk of errors. Follow the principle of avoiding globals and hardcoding.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.AvoidMagicNumbersAndStrings,
		Title,
		MessageFormat,
		DiagnosticCategories.CodeQuality,
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

		context.RegisterSyntaxNodeAction(AnalyzeLiteralExpression, SyntaxKind.NumericLiteralExpression);
		context.RegisterSyntaxNodeAction(AnalyzeLiteralExpression, SyntaxKind.StringLiteralExpression);
	}

	private static void AnalyzeLiteralExpression(SyntaxNodeAnalysisContext context)
	{
		var literalExpression = (LiteralExpressionSyntax)context.Node;

		// Skip if this is a constant declaration
		if (IsInConstantDeclaration(literalExpression))
			return;

		// Skip if this is an attribute argument
		if (IsAttributeArgument(literalExpression))
			return;

		// Skip if this is in a switch expression or case label
		if (IsInSwitchOrCase(literalExpression))
			return;

		// Analyze based on literal type
		if (literalExpression.Token.IsKind(SyntaxKind.NumericLiteralToken))
		{
			AnalyzeNumericLiteral(context, literalExpression);
		}
		else if (literalExpression.Token.IsKind(SyntaxKind.StringLiteralToken))
		{
			AnalyzeStringLiteral(context, literalExpression);
		}
	}

	private static void AnalyzeNumericLiteral(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literal)
	{
		var value = literal.Token.ValueText;

		// Skip common numbers that are typically not considered magic
		if (IsCommonNumber(value))
			return;

		// Report diagnostic for magic number
		var diagnostic = Diagnostic.Create(
			Rule,
			literal.GetLocation(),
			"number",
			value);
		context.ReportDiagnostic(diagnostic);
	}

	private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literal)
	{
		var value = literal.Token.ValueText;

		// Skip empty strings and very short strings (often used for formatting)
		if (string.IsNullOrEmpty(value) || value.Length <= 1)
			return;

		// Skip strings that look like format strings or templates
		if (IsFormatString(value))
			return;

		// Skip very common strings
		if (IsCommonString(value))
			return;

		// Report diagnostic for magic string
		var diagnostic = Diagnostic.Create(
			Rule,
			literal.GetLocation(),
			"string",
			value);
		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsInConstantDeclaration(SyntaxNode node)
	{
		// Check if we're in a const field declaration
		var fieldDeclaration = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
		if (fieldDeclaration != null && fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
			return true;

		// Check if we're in a local const declaration
		var localDeclaration = node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
		if (localDeclaration != null && localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
			return true;

		return false;
	}

	private static bool IsAttributeArgument(SyntaxNode node)
	{
		return node.FirstAncestorOrSelf<AttributeArgumentSyntax>() != null ||
			   node.FirstAncestorOrSelf<AttributeSyntax>() != null;
	}

	private static bool IsInSwitchOrCase(SyntaxNode node)
	{
		return node.FirstAncestorOrSelf<SwitchExpressionArmSyntax>() != null ||
			   node.FirstAncestorOrSelf<CaseSwitchLabelSyntax>() != null ||
			   node.FirstAncestorOrSelf<SwitchExpressionSyntax>() != null;
	}

	private static bool IsCommonNumber(string value)
	{
		// Only very basic numbers that are typically acceptable in any context
		var commonNumbers = new[]
		{
			"0", "1", "-1", "2"
		};

		return commonNumbers.Contains(value);
	}

	private static bool IsFormatString(string value)
	{
		// Check for format strings like "{0}", "{name}", etc.
		return value.Contains('{') && value.Contains('}');
	}

	private static bool IsCommonString(string value)
	{
		// Very common strings that might be acceptable
		var commonStrings = new[]
		{
			" ", "\n", "\r\n", "\t", ",", ".", ":", ";", 
			"true", "false", "null", "undefined"
		};

		return commonStrings.Contains(value, System.StringComparer.OrdinalIgnoreCase);
	}
}