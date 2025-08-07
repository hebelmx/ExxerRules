using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.CodeFormatting;

/// <summary>
/// Analyzer that detects common formatting issues and suggests running dotnet format.
/// SRP: Responsible for detecting formatting inconsistencies and suggesting automated fixes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CodeFormattingAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Code formatting inconsistency detected";
	private static readonly LocalizableString MessageFormat = "Formatting issue detected: {0}. Consider running 'dotnet format' to fix automatically.";
	private static readonly LocalizableString Description = "Detects common code formatting issues that can be automatically fixed with 'dotnet format' command.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.CodeFormattingIssue,
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

		// Register various syntax node analyzers for formatting issues
		context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
	}

	private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		
		// Check for inconsistent brace placement
		if (HasInconsistentBraces(classDeclaration))
		{
			ReportFormattingIssue(context, classDeclaration, "Inconsistent brace placement");
		}

		// Check for missing blank lines between members
		if (HasMissingBlankLinesBetweenMembers(classDeclaration))
		{
			ReportFormattingIssue(context, classDeclaration, "Missing blank lines between class members");
		}
	}

	private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
	{
		var methodDeclaration = (MethodDeclarationSyntax)context.Node;
		
		// Check for inconsistent parameter formatting
		if (HasInconsistentParameterFormatting(methodDeclaration))
		{
			ReportFormattingIssue(context, methodDeclaration, "Inconsistent parameter formatting");
		}

		// Check for missing spaces around operators in method body
		if (HasMissingOperatorSpacing(methodDeclaration))
		{
			ReportFormattingIssue(context, methodDeclaration, "Missing spaces around operators");
		}
	}

	private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
	{
		var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
		
		// Check for inconsistent accessor formatting
		if (HasInconsistentAccessorFormatting(propertyDeclaration))
		{
			ReportFormattingIssue(context, propertyDeclaration, "Inconsistent property accessor formatting");
		}
	}

	private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
	{
		var variableDeclaration = (VariableDeclarationSyntax)context.Node;
		
		// Check for inconsistent variable initialization formatting
		if (HasInconsistentVariableFormatting(variableDeclaration))
		{
			ReportFormattingIssue(context, variableDeclaration, "Inconsistent variable declaration formatting");
		}
	}

	private static void ReportFormattingIssue(SyntaxNodeAnalysisContext context, SyntaxNode node, string issueDescription)
	{
		var diagnostic = Diagnostic.Create(
			Rule,
			node.GetLocation(),
			issueDescription);

		context.ReportDiagnostic(diagnostic);
	}

	// Formatting detection methods
	private static bool HasInconsistentBraces(ClassDeclarationSyntax classDeclaration)
	{
		// Simple check: if opening brace is not on the same line or next line consistently
		var openBrace = classDeclaration.OpenBraceToken;
		var identifier = classDeclaration.Identifier;
		
		if (!openBrace.IsKind(SyntaxKind.None))
		{
			var identifierLine = identifier.GetLocation().GetLineSpan().StartLinePosition.Line;
			var braceLine = openBrace.GetLocation().GetLineSpan().StartLinePosition.Line;
			
			// Check if there are inconsistent line breaks (more sophisticated logic could be added)
			return Math.Abs(braceLine - identifierLine) > 2;
		}
		
		return false;
	}

	private static bool HasMissingBlankLinesBetweenMembers(ClassDeclarationSyntax classDeclaration)
	{
		var members = classDeclaration.Members;
		
		for (int i = 1; i < members.Count; i++)
		{
			var previousMember = members[i - 1];
			var currentMember = members[i];
			
			var previousLine = previousMember.GetLocation().GetLineSpan().EndLinePosition.Line;
			var currentLine = currentMember.GetLocation().GetLineSpan().StartLinePosition.Line;
			
			// If members are on consecutive lines, might need formatting
			if (currentLine - previousLine == 1)
			{
				// Check if both members are substantial (not just fields)
				if (IsSubstantialMember(previousMember) && IsSubstantialMember(currentMember))
				{
					return true;
				}
			}
		}
		
		return false;
	}

	private static bool HasInconsistentParameterFormatting(MethodDeclarationSyntax methodDeclaration)
	{
		var parameterList = methodDeclaration.ParameterList;
		if (parameterList?.Parameters.Count > 2)
		{
			// Check if parameters are formatted consistently (all on one line vs. each on separate line)
			var firstParam = parameterList.Parameters[0];
			var lastParam = parameterList.Parameters.Last();
			
			var firstLine = firstParam.GetLocation().GetLineSpan().StartLinePosition.Line;
			var lastLine = lastParam.GetLocation().GetLineSpan().StartLinePosition.Line;
			
			// If we have many parameters spanning multiple lines inconsistently
			return lastLine - firstLine > 0 && parameterList.Parameters.Count > 3;
		}
		
		return false;
	}

	private static bool HasMissingOperatorSpacing(MethodDeclarationSyntax methodDeclaration)
	{
		// This is a simplified check - in practice, you'd want more sophisticated analysis
		var methodText = methodDeclaration.ToString();
		
		// Look for common spacing issues (simplified patterns)
		return methodText.Contains("=") && (methodText.Contains(" =") || methodText.Contains("= ")) &&
			   (methodText.Contains("if(") || methodText.Contains("for(") || methodText.Contains("while("));
	}

	private static bool HasInconsistentAccessorFormatting(PropertyDeclarationSyntax propertyDeclaration)
	{
		var accessorList = propertyDeclaration.AccessorList;
		if (accessorList?.Accessors.Count >= 2)
		{
			var getAccessor = accessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
			var setAccessor = accessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.SetKeyword));
			
			if (getAccessor != null && setAccessor != null)
			{
				// Check if they're formatted differently (one has body, other doesn't, etc.)
				var getHasBody = getAccessor.Body != null;
				var setHasBody = setAccessor.Body != null;
				
				// If one has a body and the other doesn't, might indicate formatting inconsistency
				return getHasBody != setHasBody && (getAccessor.ExpressionBody != null || setAccessor.ExpressionBody != null);
			}
		}
		
		return false;
	}

	private static bool HasInconsistentVariableFormatting(VariableDeclarationSyntax variableDeclaration)
	{
		// Check for inconsistent spacing around assignment operators
		var declarationText = variableDeclaration.ToString();
		
		// Look for patterns like "var x=5" vs "var y = 10" in the same context
		return declarationText.Contains("=") && 
			   (!declarationText.Contains(" = ") || declarationText.Contains(" =") || declarationText.Contains("= "));
	}

	private static bool IsSubstantialMember(MemberDeclarationSyntax member)
	{
		// Consider methods, properties, classes as substantial (not just fields)
		return member is MethodDeclarationSyntax ||
			   member is PropertyDeclarationSyntax ||
			   member is ClassDeclarationSyntax ||
			   member is ConstructorDeclarationSyntax;
	}
}