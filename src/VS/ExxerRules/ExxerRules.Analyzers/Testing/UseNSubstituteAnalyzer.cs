using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Testing;

/// <summary>
/// Analyzer that enforces NSubstitute usage instead of Moq for mocking.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseNSubstituteAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Use NSubstitute for mocking";
	private static readonly LocalizableString MessageFormat = "Use NSubstitute instead of '{0}' for mocking";
	private static readonly LocalizableString Description = "NSubstitute should be used for mocking instead of Moq or other mocking frameworks.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.UseNSubstitute,
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

		context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
		context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
		context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
		context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
	}

	private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
	{
		var usingDirective = (UsingDirectiveSyntax)context.Node;
		var namespaceName = usingDirective.Name?.ToString();

		if (namespaceName == null)
			return;

		// Check for forbidden mocking framework namespaces
		var forbiddenNamespaces = new[]
		{
			"Moq",
			"Moq.Protected",
			"Moq.Language",
			"Moq.Language.Flow",
			"FakeItEasy",
			"Rhino.Mocks"
		};

		foreach (var forbidden in forbiddenNamespaces)
		{
			if (namespaceName == forbidden || namespaceName.StartsWith(forbidden + "."))
			{
				var frameworkName = GetFrameworkNameFromNamespace(forbidden);
				var diagnostic = Diagnostic.Create(
					Rule,
					usingDirective.GetLocation(),
					frameworkName);

				context.ReportDiagnostic(diagnostic);
				break;
			}
		}
	}

	private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
	{
		var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
		var typeName = objectCreation.Type.ToString();

		// Check for Mock<T> object creation
		if (typeName.StartsWith("Mock<") || typeName == "Mock")
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				objectCreation.GetLocation(),
				"Moq");

			context.ReportDiagnostic(diagnostic);
		}

		// Check for other mocking framework object creation
		var forbiddenTypes = new[]
		{
			"MockRepository",
			"A.Fake",
			"Fake"
		};

		foreach (var forbidden in forbiddenTypes)
		{
			if (typeName.StartsWith(forbidden))
			{
				var framework = GetFrameworkNameFromType(forbidden);
				var diagnostic = Diagnostic.Create(
					Rule,
					objectCreation.GetLocation(),
					framework);

				context.ReportDiagnostic(diagnostic);
				break;
			}
		}
	}

	private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;
		var invocationText = invocation.ToString();

		// Check for Moq method calls
		var moqMethods = new[]
		{
			"Mock.Of<",
			".Setup(",
			".SetupGet(",
			".SetupSet(",
			".Verify(",
			".VerifyGet(",
			".VerifySet(",
			".VerifyAll(",
			".VerifyNoOtherCalls(",
			".Returns(",
			".Throws(",
			".Callback("
		};

		foreach (var moqMethod in moqMethods)
		{
			if (invocationText.Contains(moqMethod))
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					invocation.GetLocation(),
					"Moq");

				context.ReportDiagnostic(diagnostic);
				break;
			}
		}

		// Check for FakeItEasy method calls
		if (invocationText.StartsWith("A.CallTo(") || 
			invocationText.StartsWith("A.Fake<") ||
			invocationText.Contains(".MustHaveHappened"))
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				invocation.GetLocation(),
				"FakeItEasy");

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;
		var memberName = memberAccess.Name.Identifier.ValueText;

		// Check for Moq-specific properties and methods
		var moqMembers = new[]
		{
			"Object", // Mock<T>.Object
			"Verify",
			"VerifyAll",
			"Setup",
			"SetupGet",
			"SetupSet",
			"Returns",
			"Throws",
			"Callback"
		};

		if (moqMembers.Contains(memberName))
		{
			// Check if this is on a Mock<T> object
			var expressionText = memberAccess.Expression?.ToString() ?? "";
			if (expressionText.Contains("Mock") || 
				(memberAccess.Expression != null && context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type?.Name.Contains("Mock") == true))
			{
				var diagnostic = Diagnostic.Create(
					Rule,
					memberAccess.GetLocation(),
					"Moq");

				context.ReportDiagnostic(diagnostic);
			}
		}

		// Check for FakeItEasy-specific members
		if (memberName == "MustHaveHappened" || memberName == "MustNotHaveHappened")
		{
			var diagnostic = Diagnostic.Create(
				Rule,
				memberAccess.GetLocation(),
				"FakeItEasy");

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static string GetFrameworkNameFromNamespace(string namespaceName)
	{
		return namespaceName switch
		{
			var ns when ns.StartsWith("Moq") => "Moq",
			var ns when ns.StartsWith("FakeItEasy") => "FakeItEasy",
			var ns when ns.StartsWith("Rhino.Mocks") => "Rhino Mocks",
			_ => "unknown mocking framework"
		};
	}

	private static string GetFrameworkNameFromType(string typeName)
	{
		return typeName switch
		{
			var t when t.StartsWith("Mock") => "Moq",
			var t when t.StartsWith("A.Fake") || t == "Fake" => "FakeItEasy",
			_ => "unknown mocking framework"
		};
	}
}