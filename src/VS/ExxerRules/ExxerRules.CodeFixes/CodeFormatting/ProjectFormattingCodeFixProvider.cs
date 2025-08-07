using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using ExxerRules.Analyzers;

namespace ExxerRules.CodeFixes.CodeFormatting;

/// <summary>
/// Code fix provider that executes 'dotnet format --severity info --verbosity d' on the current project.
/// SRP: Responsible only for providing code fix actions that trigger project formatting.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ProjectFormattingCodeFixProvider)), Shared]
public class ProjectFormattingCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc/>
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(DiagnosticIds.ProjectFormatting);

	/// <inheritdoc/>
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root == null) return;

		// Find the diagnostic for project formatting
		var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.ProjectFormatting);
		if (diagnostic == null) return;

		// Get project information
		var project = context.Document.Project;
		var projectPath = GetProjectPath(project);

		// Register different formatting actions
		RegisterFormattingActions(context, diagnostic, projectPath, project.Name);
	}

	/// <summary>
	/// Registers various formatting code actions.
	/// </summary>
	/// <param name="context">The code fix context.</param>
	/// <param name="diagnostic">The diagnostic to fix.</param>
	/// <param name="projectPath">The path to the project file.</param>
	/// <param name="projectName">The name of the project.</param>
	private static void RegisterFormattingActions(CodeFixContext context, Diagnostic diagnostic, string? projectPath, string projectName)
	{
		// Standard project formatting
		var formatAction = CodeAction.Create(
			title: $"🔧 Format Project '{projectName}' (dotnet format)",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, projectPath, "--severity info --verbosity d"),
			equivalenceKey: "FormatProject");

		context.RegisterCodeFix(formatAction, diagnostic);

		// Format with whitespace fixes
		var formatWhitespaceAction = CodeAction.Create(
			title: $"📝 Format Whitespace Only '{projectName}' (dotnet format whitespace)",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, projectPath, "whitespace --verbosity d"),
			equivalenceKey: "FormatWhitespace");

		context.RegisterCodeFix(formatWhitespaceAction, diagnostic);

		// Format with style fixes
		var formatStyleAction = CodeAction.Create(
			title: $"🎨 Format Style '{projectName}' (dotnet format style)",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, projectPath, "style --severity info --verbosity d"),
			equivalenceKey: "FormatStyle");

		context.RegisterCodeFix(formatStyleAction, diagnostic);

		// Format with analyzers
		var formatAnalyzersAction = CodeAction.Create(
			title: $"🔍 Format with Analyzers '{projectName}' (dotnet format analyzers)",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, projectPath, "analyzers --severity info --verbosity d"),
			equivalenceKey: "FormatAnalyzers");

		context.RegisterCodeFix(formatAnalyzersAction, diagnostic);
	}

	/// <summary>
	/// Executes the dotnet format command with specified arguments.
	/// </summary>
	/// <param name="document">The current document.</param>
	/// <param name="projectPath">The path to the project file.</param>
	/// <param name="formatArguments">The format command arguments.</param>
	/// <returns>The original document (command executes externally).</returns>
	private static Task<Document> ExecuteFormattingCommand(Document document, string? projectPath, string formatArguments)
	{
		try
		{
			// Determine the working directory
			string workingDirectory;
			string targetArgument;

			if (!string.IsNullOrEmpty(projectPath) && File.Exists(projectPath))
			{
				workingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
				targetArgument = $"\"{projectPath}\"";
			}
			else
			{
				// Fallback to solution directory or current directory
				workingDirectory = FindSolutionDirectory() ?? Environment.CurrentDirectory;
				targetArgument = "."; // Format entire solution/directory
			}

			// Prepare the process
			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"format {targetArgument} {formatArguments}",
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			// Execute the command asynchronously
			Task.Run(async () =>
			{
				try
				{
					using var process = Process.Start(processStartInfo);
					if (process != null)
					{
						await Task.Run(() => process.WaitForExit());
						
						// Log results to Output window (if available)
						var output = await process.StandardOutput.ReadToEndAsync();
						var error = await process.StandardError.ReadToEndAsync();
						
						System.Diagnostics.Debug.WriteLine($"dotnet format completed with exit code: {process.ExitCode}");
						if (!string.IsNullOrEmpty(output))
						{
							System.Diagnostics.Debug.WriteLine($"Output: {output}");
						}
						if (!string.IsNullOrEmpty(error))
						{
							System.Diagnostics.Debug.WriteLine($"Error: {error}");
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error executing dotnet format: {ex.Message}");
				}
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error setting up dotnet format: {ex.Message}");
		}

		// Return the original document since formatting happens externally
		// The IDE will reload the formatted files automatically
		return Task.FromResult(document);
	}

	/// <summary>
	/// Gets the project file path from the project instance.
	/// </summary>
	/// <param name="project">The project instance.</param>
	/// <returns>The project file path if available.</returns>
	private static string? GetProjectPath(Project project)
	{
		// Try to get the project file path
		var projectFilePath = project.FilePath;
		if (!string.IsNullOrEmpty(projectFilePath) && File.Exists(projectFilePath))
		{
			return projectFilePath;
		}

		// Fallback: try to construct from project name and output path
		var outputPath = project.OutputFilePath;
		if (!string.IsNullOrEmpty(outputPath))
		{
			var projectDirectory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(projectDirectory))
			{
				var possibleProjectFile = Path.Combine(projectDirectory, $"{project.Name}.csproj");
				if (File.Exists(possibleProjectFile))
				{
					return possibleProjectFile;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Finds the solution directory by walking up the directory tree.
	/// </summary>
	/// <returns>The solution directory path if found.</returns>
	private static string? FindSolutionDirectory()
	{
		var currentDirectory = Environment.CurrentDirectory;
		
		while (!string.IsNullOrEmpty(currentDirectory))
		{
			// Look for .sln files
			if (Directory.GetFiles(currentDirectory, "*.sln").Length > 0)
			{
				return currentDirectory;
			}

			// Look for .git directory (indicates repository root)
			if (Directory.Exists(Path.Combine(currentDirectory, ".git")))
			{
				return currentDirectory;
			}

			// Move up one directory
			var parentDirectory = Directory.GetParent(currentDirectory);
			if (parentDirectory == null || parentDirectory.FullName == currentDirectory)
			{
				break;
			}

			currentDirectory = parentDirectory.FullName;
		}

		return null;
	}
}