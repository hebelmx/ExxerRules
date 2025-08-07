using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that prevents mocking of EF Core DbContext, enforcing InMemory provider usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotMockDbContextAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Do not mock DbContext";
	private static readonly LocalizableString MessageFormat = "Do not mock DbContext '{0}'. Use InMemory provider instead.";
	private static readonly LocalizableString Description = "EF Core DbContext should not be mocked. Use the InMemory database provider for testing instead.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.DoNotMockDbContext,
		Title,
		MessageFormat,
		DiagnosticCategories.Testing,
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

		context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
		context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
		context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
	}

	private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
	{
		var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
		var typeName = objectCreation.Type.ToString();

		// Check for Mock<DbContext> or Mock<CustomDbContext>
		if (typeName.StartsWith("Mock<") && IsDbContextType(typeName, context.SemanticModel, objectCreation))
		{
			var dbContextType = ExtractGenericTypeArgument(typeName);
			var diagnostic = Diagnostic.Create(
				Rule,
				objectCreation.GetLocation(),
				dbContextType);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		// Check for Mock.Of<DbContext>() calls
		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
			memberAccess.Name.Identifier.ValueText == "Of" &&
			memberAccess.Expression?.ToString() == "Mock")
		{
			if (invocation.Expression is GenericNameSyntax genericName)
			{
				var typeArgument = genericName.TypeArgumentList.Arguments.FirstOrDefault();
				if (typeArgument != null && IsDbContextType(typeArgument, context.SemanticModel))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						invocation.GetLocation(),
						typeArgument.ToString());

					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		// Check for Substitute.For<DbContext>() calls (this is also discouraged)
		if (invocation.Expression is MemberAccessExpressionSyntax substituteMemberAccess &&
			substituteMemberAccess.Name.Identifier.ValueText == "For" &&
			substituteMemberAccess.Expression?.ToString() == "Substitute")
		{
			if (invocation.Expression is GenericNameSyntax substituteGenericName)
			{
				var typeArgument = substituteGenericName.TypeArgumentList.Arguments.FirstOrDefault();
				if (typeArgument != null && IsDbContextType(typeArgument, context.SemanticModel))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						invocation.GetLocation(),
						typeArgument.ToString());

					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}

	private static void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
	{
		var genericName = (GenericNameSyntax)context.Node;

		// Check for Mock<DbContext> in variable declarations and method parameters
		if (genericName.Identifier.ValueText == "Mock")
		{
			var typeArgument = genericName.TypeArgumentList.Arguments.FirstOrDefault();
			if (typeArgument != null && IsDbContextType(typeArgument, context.SemanticModel))
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					genericName.GetLocation(),
					typeArgument.ToString());

				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	private static bool IsDbContextType(string typeName, SemanticModel semanticModel, SyntaxNode node)
	{
		// Extract the generic type argument from Mock<T>
		var genericArg = ExtractGenericTypeArgument(typeName);
		if (string.IsNullOrEmpty(genericArg))
			return false;

		// Check if it's a known DbContext type name
		if (genericArg == "DbContext" || genericArg.EndsWith("DbContext") || genericArg.EndsWith("Context"))
		{
			return true;
		}

		// Use semantic model to check if the type inherits from DbContext
		var typeInfo = semanticModel.GetTypeInfo(node);
		if (typeInfo.Type is INamedTypeSymbol namedType)
		{
			return InheritsFromDbContext(namedType);
		}

		return false;
	}

	private static bool IsDbContextType(TypeSyntax typeSyntax, SemanticModel semanticModel)
	{
		var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
		if (typeInfo.Type is INamedTypeSymbol namedType)
		{
			return InheritsFromDbContext(namedType);
		}

		// Fallback to name-based check
		var typeName = typeSyntax.ToString();
		return typeName == "DbContext" || typeName.EndsWith("DbContext") || typeName.EndsWith("Context");
	}

	private static bool InheritsFromDbContext(INamedTypeSymbol type)
	{
		var baseType = type.BaseType;
		while (baseType != null)
		{
			if (baseType.Name == "DbContext" && 
				baseType.ContainingNamespace?.ToDisplayString() == "Microsoft.EntityFrameworkCore")
			{
				return true;
			}
			baseType = baseType.BaseType;
		}

		return false;
	}

	private static string ExtractGenericTypeArgument(string typeName)
	{
		// Extract T from Mock<T>
		var startIndex = typeName.IndexOf('<');
		var endIndex = typeName.LastIndexOf('>');
		
		if (startIndex > 0 && endIndex > startIndex)
		{
			return typeName.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
		}

		return string.Empty;
	}
}