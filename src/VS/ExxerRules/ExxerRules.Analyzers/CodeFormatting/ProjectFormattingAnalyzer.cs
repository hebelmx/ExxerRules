using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.CodeFormatting;

/// <summary>
/// Analyzer that provides a mechanism to trigger project formatting on demand.
/// This analyzer will always report a hidden diagnostic that can be used to trigger formatting.
/// SRP: Responsible only for providing a trigger point for project-wide formatting actions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ProjectFormattingAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Format project with dotnet format";
	private static readonly LocalizableString MessageFormat = "Click to format the entire project using 'dotnet format --severity info --verbosity d'";
	private static readonly LocalizableString Description = "Provides an action to run 'dotnet format --severity info --verbosity d' on the current project. This action will format all files in the project according to EditorConfig settings and code style rules.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.ProjectFormatting,
		Title,
		MessageFormat,
		DiagnosticCategories.CodeQuality,
		DiagnosticSeverity.Hidden, // Hidden so it doesn't show as warning/error but can still trigger code actions
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// Register on compilation start to provide project-wide formatting option
		context.RegisterCompilationStartAction(OnCompilationStart);
	}

	private static void OnCompilationStart(CompilationStartAnalysisContext context) =>
		// Register syntax tree analysis to provide formatting action on every file
		context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

	private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		// Only report on the first line of the file to provide a consistent location
		var root = context.Tree.GetRoot(context.CancellationToken);
		if (root.HasLeadingTrivia || root.ChildNodes().Any())
		{
			var location = root.ChildNodes().FirstOrDefault()?.GetLocation() ??
						  Location.Create(context.Tree, new Microsoft.CodeAnalysis.Text.TextSpan(0, 0));

			// Report a hidden diagnostic that can be used to trigger formatting
			var diagnostic = Diagnostic.Create(
				Rule,
				location,
				context.Tree.FilePath ?? "Current Project");

			context.ReportDiagnostic(diagnostic);
		}
	}
}
