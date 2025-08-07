using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Architecture;

/// <summary>
/// Analyzer that enforces Domain layer should not reference Infrastructure layer.
/// Supports Clean Architecture principles.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DomainShouldNotReferenceInfrastructureAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Domain layer should not reference Infrastructure layer";
	private static readonly LocalizableString MessageFormat = "Domain layer class '{0}' should not reference Infrastructure namespace '{1}' - violates Clean Architecture";
	private static readonly LocalizableString Description = "In Clean Architecture, the Domain layer should be independent and not reference the Infrastructure layer. Dependencies should flow inward, with Infrastructure depending on Domain, not the reverse.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.DomainShouldNotReferenceInfrastructure,
		Title,
		MessageFormat,
		DiagnosticCategories.Architecture,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// TDD Green phase: Focus on using directives in Domain namespace files
		context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
	}

	private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
	{
		var usingDirective = (UsingDirectiveSyntax)context.Node;
		
		// Check if we're in a Domain namespace
		if (!IsInDomainNamespace(context.Node))
			return;

		var namespaceName = usingDirective.Name?.ToString();
		if (namespaceName == null)
			return;

		// Check if the using directive references Infrastructure
		if (IsInfrastructureNamespace(namespaceName))
		{
			var containingClass = GetContainingClassName(context.Node);
			
			var diagnostic = Diagnostic.Create(
				Rule,
				usingDirective.GetLocation(),
				containingClass ?? "Domain class",
				namespaceName);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool IsInDomainNamespace(SyntaxNode node)
	{
		// Find the containing namespace declaration
		var namespaceDeclaration = node.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
		
		// If no namespace declaration found, check the compilation unit for namespace
		if (namespaceDeclaration == null)
		{
			// Look for namespace declarations in the file
			var root = node.SyntaxTree.GetRoot();
			var allNamespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>();
			namespaceDeclaration = allNamespaces.FirstOrDefault();
		}
		
		if (namespaceDeclaration == null)
			return false;

		var namespaceName = namespaceDeclaration.Name.ToString();
		
		// Check if namespace contains "Domain" (case-sensitive)
		return namespaceName.Contains(".Domain.") || 
			   namespaceName.StartsWith("Domain.") || 
			   namespaceName.EndsWith(".Domain") ||
			   namespaceName == "Domain";
	}

	private static bool IsInfrastructureNamespace(string namespaceName)
	{
		// Check if namespace contains "Infrastructure" (case-sensitive)
		if (namespaceName.Contains(".Infrastructure.") || 
			namespaceName.StartsWith("Infrastructure.") || 
			namespaceName.EndsWith(".Infrastructure") ||
			namespaceName == "Infrastructure")
		{
			return true;
		}

		// Check for Entity Framework Core (infrastructure concern)
		if (namespaceName.Contains("Microsoft.EntityFrameworkCore") ||
			namespaceName.StartsWith("Microsoft.EntityFrameworkCore") ||
			namespaceName.Contains("EntityFrameworkCore"))
		{
			return true;
		}

		// Check for other common infrastructure namespaces
		var infrastructureNamespaces = new[]
		{
			"Microsoft.EntityFrameworkCore",
			"System.Data.SqlClient",
			"System.Data.Odbc",
			"System.Data.OleDb",
			"Npgsql",
			"MySql.Data",
			"Oracle.ManagedDataAccess",
			"Microsoft.Data.SqlClient"
		};

		return infrastructureNamespaces.Any(ns => namespaceName.Contains(ns));
	}

	private static string? GetContainingClassName(SyntaxNode node)
	{
		// Find the containing class declaration
		var classDeclaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
		return classDeclaration?.Identifier.ValueText;
	}
}