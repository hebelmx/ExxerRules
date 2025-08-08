using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExxerRules.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace ExxerRules.CodeFixes.ErrorHandling;

/// <summary>
/// Code fix provider that converts exception throwing methods to Result&lt;T&gt; pattern.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseResultPatternCodeFixProvider)), Shared]
public class UseResultPatternCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc/>
	public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.UseResultPattern);

	/// <inheritdoc/>
	public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return;
		}

		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		var node = root.FindNode(diagnosticSpan);
		if (node == null)
		{
			return;
		}
		// Find the containing method
		var methodDeclaration = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
		if (methodDeclaration != null)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: "Convert to Result<T> pattern",
					createChangedDocument: c => ConvertMethodToResultPatternAsync(context.Document, methodDeclaration, c),
					equivalenceKey: "ConvertToResultPattern"),
				diagnostic);
		}
	}

	private async Task<Document> ConvertMethodToResultPatternAsync(
		Document document,
		MethodDeclarationSyntax methodDeclaration,
		CancellationToken cancellationToken)
	{
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		var generator = editor.Generator;

		// Determine the new return type
		var currentReturnType = methodDeclaration.ReturnType;
		TypeSyntax newReturnType;

		if (IsTaskType(currentReturnType))
		{
			// Extract the inner type from Task<T> or just use Task
			var innerType = GetTaskInnerType(currentReturnType);
			if (innerType != null)
			{
				newReturnType = SyntaxFactory.ParseTypeName($"Task<Result<{innerType}>>");
			}
			else
			{
				newReturnType = SyntaxFactory.ParseTypeName("Task<Result>");
			}
		}
		else if (currentReturnType.ToString() == "void")
		{
			newReturnType = SyntaxFactory.ParseTypeName("Result");
		}
		else
		{
			newReturnType = SyntaxFactory.ParseTypeName($"Result<{currentReturnType}>");
		}

		// Create new method with Result return type
		var newMethod = methodDeclaration.WithReturnType(newReturnType);

		// Convert throw statements to Result.WithFailure
		var rewriter = new ThrowToResultRewriter();
		newMethod = (MethodDeclarationSyntax)rewriter.Visit(newMethod);

		// Replace the method
		editor.ReplaceNode(methodDeclaration, newMethod);

		// Add using statement if needed
		var root = await editor.GetChangedDocument().GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is CompilationUnitSyntax compilationUnit)
		{
			var hasResultUsing = compilationUnit.Usings.Any(u => u.Name?.ToString().Contains("Result") == true);
			if (!hasResultUsing)
			{
				var firstNode = compilationUnit.Usings.FirstOrDefault() as SyntaxNode ?? compilationUnit.Members.FirstOrDefault();
				if (firstNode != null)
				{
					editor.InsertBefore(firstNode,
						SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("FluentResults")));
				}
			}
		}

		return editor.GetChangedDocument();
	}

	private static bool IsTaskType(TypeSyntax type)
	{
		var typeString = type.ToString();
		return typeString.StartsWith("Task<") || typeString == "Task" ||
			   typeString.StartsWith("ValueTask<") || typeString == "ValueTask";
	}

	private static string? GetTaskInnerType(TypeSyntax type)
	{
		if (type is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
		{
			return genericName.TypeArgumentList.Arguments[0].ToString();
		}
		return null;
	}

	private class ThrowToResultRewriter : CSharpSyntaxRewriter
	{
		public override SyntaxNode? VisitThrowStatement(ThrowStatementSyntax node)
		{
			if (node.Expression == null)
			{
				// Rethrow - keep as is for now
				return base.VisitThrowStatement(node);
			}

			// Extract error message from exception
			var errorMessage = ExtractErrorMessage(node.Expression);

			// Create Result.Fail statement (FluentResults syntax)
			var resultStatement = SyntaxFactory.ParseStatement($"return Result.Fail({errorMessage});")
				.WithLeadingTrivia(node.GetLeadingTrivia())
				.WithTrailingTrivia(node.GetTrailingTrivia())
				.WithAdditionalAnnotations(Formatter.Annotation);

			return resultStatement;
		}

		public override SyntaxNode? VisitThrowExpression(ThrowExpressionSyntax node)
		{
			// Extract error message from exception
			var errorMessage = ExtractErrorMessage(node.Expression);

			// Create Result.Fail expression (FluentResults syntax)
			var resultExpression = SyntaxFactory.ParseExpression($"Result.Fail({errorMessage})")
				.WithAdditionalAnnotations(Formatter.Annotation);

			return resultExpression;
		}

		private static string ExtractErrorMessage(ExpressionSyntax expression)
		{
			// Try to extract the message from exception constructor
			if (expression is ObjectCreationExpressionSyntax objectCreation &&
				objectCreation.ArgumentList?.Arguments.Count > 0)
			{
				var firstArg = objectCreation.ArgumentList.Arguments[0].Expression;
				return firstArg.ToString();
			}

			// Default message based on exception type
			return "\"Operation failed\"";
		}
	}
}
