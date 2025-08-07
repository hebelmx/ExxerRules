using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ExxerRules.Analyzers.Common;
using FluentResults;

namespace ExxerRules.Analyzers.CodeQuality;

/// <summary>
/// Analyzer that enforces not using regions for code organization.
/// Supports the "prefer subclasses over regions" principle.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotUseRegionsAnalyzer : DiagnosticAnalyzer
{
	private static readonly LocalizableString Title = "Do not use regions for code organization";
	private static readonly LocalizableString MessageFormat = "Region '{0}' should be avoided - prefer sub-classes or separate files for organization";
	private static readonly LocalizableString Description = "Regions should be avoided in favor of better code organization using sub-classes or separate files. Regions can hide poor design and make code harder to navigate.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticIds.DoNotUseRegions,
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

		context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
	}

	private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		var root = context.Tree.GetRoot(context.CancellationToken);

		// Find all region directives in the syntax tree
		var regionDirectives = root.DescendantTrivia()
			.Where(trivia => trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
			.ToList();

		foreach (var regionDirective in regionDirectives)
		{
			// Extract region name from the directive
			var regionName = GetRegionName(regionDirective);
			
			// Report diagnostic for each region
			var diagnostic = Diagnostic.Create(
				Rule,
				regionDirective.GetLocation(),
				regionName);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static string GetRegionName(SyntaxTrivia regionDirective)
	{
		// Get the text of the region directive and extract the name
		var text = regionDirective.ToString();
		
		// Remove "#region" prefix and trim whitespace
		var name = text.Replace("#region", "").Trim();
		
		// If no name was provided, use a default
		if (string.IsNullOrEmpty(name))
			return "unnamed region";
		
		return name;
	}
}