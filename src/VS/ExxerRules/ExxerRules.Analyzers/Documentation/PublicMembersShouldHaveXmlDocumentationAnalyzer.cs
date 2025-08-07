using System.Collections.Immutable;
using System.Linq;
using ExxerRules.Analyzers.Common;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Documentation;

/// <summary>
/// Analyzer that enforces XML documentation on public members.
/// Supports the "documentation is the bridge from intent to understanding" principle.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PublicMembersShouldHaveXmlDocumentationAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Public members should have XML documentation";
	private static readonly LocalizableString MessageFormat = "Public {0} '{1}' should have XML documentation";
	private static readonly LocalizableString Description = "Public members should have XML documentation to support IntelliSense, tooling integration, and developer understanding. Documentation is the bridge from intent to understanding.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.PublicMembersShouldHaveXmlDocumentation,
		Title,
		MessageFormat,
		DiagnosticCategories.Documentation,
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

		context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeStruct, SyntaxKind.StructDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeEnum, SyntaxKind.EnumDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeEvent, SyntaxKind.EventDeclaration);
	}

	private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		AnalyzeMember(context, classDeclaration, classDeclaration.Modifiers, classDeclaration.Identifier, "class");
	}

	private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
	{
		var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
		AnalyzeMember(context, interfaceDeclaration, interfaceDeclaration.Modifiers, interfaceDeclaration.Identifier, "interface");
	}

	private static void AnalyzeStruct(SyntaxNodeAnalysisContext context)
	{
		var structDeclaration = (StructDeclarationSyntax)context.Node;
		AnalyzeMember(context, structDeclaration, structDeclaration.Modifiers, structDeclaration.Identifier, "struct");
	}

	private static void AnalyzeEnum(SyntaxNodeAnalysisContext context)
	{
		var enumDeclaration = (EnumDeclarationSyntax)context.Node;
		AnalyzeMember(context, enumDeclaration, enumDeclaration.Modifiers, enumDeclaration.Identifier, "enum");
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var methodDeclaration = (MethodDeclarationSyntax)context.Node;

		// Skip special methods
		if (IsSkippableMethod(methodDeclaration))
		{
			return;
		}

		AnalyzeMember(context, methodDeclaration, methodDeclaration.Modifiers, methodDeclaration.Identifier, "method");
	}

	private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
	{
		var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
		AnalyzeMember(context, propertyDeclaration, propertyDeclaration.Modifiers, propertyDeclaration.Identifier, "property");
	}

	private static void AnalyzeField(SyntaxNodeAnalysisContext context)
	{
		var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

		// Skip const fields (they're often obvious)
		if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
		{
			return;
		}

		foreach (var variable in fieldDeclaration.Declaration.Variables)
		{
			AnalyzeMember(context, fieldDeclaration, fieldDeclaration.Modifiers, variable.Identifier, "field");
		}
	}

	private static void AnalyzeEvent(SyntaxNodeAnalysisContext context)
	{
		var eventDeclaration = (EventDeclarationSyntax)context.Node;
		AnalyzeMember(context, eventDeclaration, eventDeclaration.Modifiers, eventDeclaration.Identifier, "event");
	}

	private static void AnalyzeMember(
		SyntaxNodeAnalysisContext context,
		SyntaxNode node,
		SyntaxTokenList modifiers,
		SyntaxToken identifier,
		string memberType)
	{
		// Only analyze public members or interface members
		if (!IsPublicMember(modifiers) && !IsInterfaceMember(node))
		{
			return;
		}

		// Check if member has XML documentation
		if (!HasXmlDocumentation(node))
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				identifier.GetLocation(),
				memberType,
				identifier.Text);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool IsPublicMember(SyntaxTokenList modifiers) =>
		// Member is public if it explicitly has public modifier
		// OR if it's in an interface (interface members are implicitly public)
		modifiers.Any(SyntaxKind.PublicKeyword);

	private static bool IsInterfaceMember(SyntaxNode node)
	{
		// Check if the member is inside an interface
		var parent = node.Parent;
		while (parent != null)
		{
			if (parent is InterfaceDeclarationSyntax)
			{
				return true;
			}

			parent = parent.Parent;
		}
		return false;
	}

	private static bool HasXmlDocumentation(SyntaxNode node)
	{
		// Check for XML documentation comments (///)
		var leadingTrivia = node.GetLeadingTrivia();

		return leadingTrivia.Any(trivia =>
			trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
			trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
	}

	private static bool IsSkippableMethod(MethodDeclarationSyntax method)
	{
		var methodName = method.Identifier.Text;

		// Skip Main method
		if (methodName == "Main")
		{
			return true;
		}

		// Skip override methods (they inherit documentation)
		if (method.Modifiers.Any(SyntaxKind.OverrideKeyword))
		{
			return true;
		}

		// Skip interface implementations (they should be documented on the interface)
		if (method.ExplicitInterfaceSpecifier != null)
		{
			return true;
		}

		// Skip constructors and destructors (often self-explanatory)
		if (method.Modifiers.Any(SyntaxKind.StaticKeyword) && methodName.EndsWith("Constructor"))
		{
			return true;
		}

		return false;
	}
}
