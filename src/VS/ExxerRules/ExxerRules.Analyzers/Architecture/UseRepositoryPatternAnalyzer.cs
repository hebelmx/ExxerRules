using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Architecture;

/// <summary>
/// Analyzer that enforces using Repository pattern with focused interfaces.
/// Supports Clean Architecture and dependency inversion principles.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseRepositoryPatternAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use Repository pattern with focused interfaces";
	private static readonly LocalizableString MessageFormat = "Class '{0}' should use Repository pattern instead of direct data access - {1}";
	private static readonly LocalizableString Description = "Repository pattern provides abstraction over data access, making code more testable and maintainable. Use focused repository interfaces instead of direct DbContext or data access dependencies.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseRepositoryPattern,
		Title,
		MessageFormat,
		DiagnosticCategories.Architecture,
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

		// TDD Green phase: Focus on class declarations and their dependencies
		context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
	}

	private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		var className = classDeclaration.Identifier.ValueText;

		// Skip if this is already a repository interface or implementation
		if (IsRepositoryClass(className))
		{
			// Check if repository implementation has corresponding interface
			if (className.EndsWith("Repository") && !className.StartsWith("I"))
			{
				CheckRepositoryHasInterface(context, classDeclaration);
			}
			return;
		}

		// Check for direct DbContext usage in non-repository classes
		CheckForDirectDataAccessUsage(context, classDeclaration);
	}

	private static void CheckRepositoryHasInterface(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
	{
		var className = classDeclaration.Identifier.ValueText;
		
		// Check if class implements an interface
		if (classDeclaration.BaseList?.Types.Count > 0)
		{
			var implementsInterface = classDeclaration.BaseList.Types
				.Any(t => t.Type.ToString().StartsWith("I") && t.Type.ToString().Contains("Repository"));
			
			if (implementsInterface)
				return; // Has interface, good!
		}

		// Repository class without interface
		var diagnostic = Diagnostic.Create(
			Rule,
			classDeclaration.Identifier.GetLocation(),
			className,
			"Repository implementation should implement a focused interface");
		context.ReportDiagnostic(diagnostic);
	}

	private static void CheckForDirectDataAccessUsage(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
	{
		var className = classDeclaration.Identifier.ValueText;
		bool foundDirectAccess = false;

		// Look for DbContext fields/properties
		foreach (var member in classDeclaration.Members)
		{
			if (member is FieldDeclarationSyntax field)
			{
				var fieldType = field.Declaration.Type?.ToString() ?? "";
				if (IsDirectDataAccessType(fieldType))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						field.Declaration.GetLocation(),
						className,
						$"Use repository pattern instead of direct {fieldType} field");
					context.ReportDiagnostic(diagnostic);
					foundDirectAccess = true;
				}
			}

			if (member is PropertyDeclarationSyntax property)
			{
				var propertyType = property.Type.ToString();
				if (IsDirectDataAccessType(propertyType))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						property.Identifier.GetLocation(),
						className,
						$"Use repository pattern instead of direct {propertyType} property");
					context.ReportDiagnostic(diagnostic);
					foundDirectAccess = true;
				}
			}
		}

		// Look for DbContext constructor parameters (only if no field found)
		if (!foundDirectAccess)
		{
			foreach (var constructor in classDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
			{
				if (constructor.ParameterList?.Parameters != null)
				{
					foreach (var parameter in constructor.ParameterList.Parameters)
					{
						var paramType = parameter.Type?.ToString() ?? "";
						if (IsDirectDataAccessType(paramType))
						{
							var diagnostic = Diagnostic.Create(
								Rule,
								parameter.GetLocation(),
								className,
								$"Use repository pattern instead of direct {paramType} parameter");
							context.ReportDiagnostic(diagnostic);
							return;
						}
					}
				}
			}
		}
	}

	private static bool IsRepositoryClass(string className)
	{
		return className.Contains("Repository") || 
			   className.Contains("DataAccess") ||
			   className.Contains("Dal");
	}

	private static bool IsInInfrastructureLayer(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
	{
		// Find the containing namespace declaration
		var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
		
		if (namespaceDeclaration == null)
		{
			// Look for namespace declarations in the file
			var root = classDeclaration.SyntaxTree.GetRoot();
			var allNamespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>();
			namespaceDeclaration = allNamespaces.FirstOrDefault();
		}
		
		if (namespaceDeclaration == null)
			return false;

		var namespaceName = namespaceDeclaration.Name.ToString();
		
		// Check if namespace contains "Infrastructure" 
		return namespaceName.Contains(".Infrastructure.") || 
			   namespaceName.StartsWith("Infrastructure.") || 
			   namespaceName.EndsWith(".Infrastructure") ||
			   namespaceName == "Infrastructure";
	}

	private static bool IsDirectDataAccessType(string typeName)
	{
		var dataAccessTypes = new[]
		{
			"DbContext",
			"IDbContext", 
			"DataContext",
			"ObjectContext",
			"SqlConnection",
			"IDbConnection",
			"SqlCommand",
			"IDbCommand"
		};

		return dataAccessTypes.Any(t => typeName.Contains(t));
	}
}