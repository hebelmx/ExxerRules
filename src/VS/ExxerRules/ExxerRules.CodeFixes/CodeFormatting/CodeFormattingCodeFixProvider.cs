using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using ExxerRules.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ExxerRules.CodeFixes.CodeFormatting;

/// <summary>
/// Code fix provider that provides formatting actions for detected formatting issues.
/// SRP: Responsible only for providing code fix actions for formatting inconsistencies.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFormattingCodeFixProvider)), Shared]
public class CodeFormattingCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc/>
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(DiagnosticIds.CodeFormattingIssue);

	/// <inheritdoc/>
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return;
		}

		// Find the diagnostic for formatting issues
		var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.CodeFormattingIssue);
		if (diagnostic == null)
		{
			return;
		}

		// Get project information
		var project = context.Document.Project;
		var documentPath = context.Document.FilePath;
		var projectPath = GetProjectPath(project);

		// Register formatting actions for the current document and project
		RegisterDocumentFormattingActions(context, diagnostic, documentPath, context.Document.Name);
		RegisterProjectFormattingActions(context, diagnostic, projectPath, project.Name);
	}

	/// <summary>
	/// Registers formatting actions specific to the current document.
	/// </summary>
	/// <param name="context">The code fix context.</param>
	/// <param name="diagnostic">The diagnostic to fix.</param>
	/// <param name="documentPath">The path to the current document.</param>
	/// <param name="documentName">The name of the document.</param>
	private static void RegisterDocumentFormattingActions(CodeFixContext context, Diagnostic diagnostic, string? documentPath, string documentName)
	{
		if (string.IsNullOrEmpty(documentPath))
		{
			return;
		}

		// Format current file only
		var formatFileAction = CodeAction.Create(
			title: $"📄 Format Current File '{documentName}'",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, $"\"{documentPath}\"", "--verbosity d"),
			equivalenceKey: "FormatCurrentFile");

		context.RegisterCodeFix(formatFileAction, diagnostic);

		// Format current file with whitespace only
		var formatFileWhitespaceAction = CodeAction.Create(
			title: $"📝 Format Whitespace in '{documentName}'",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, $"\"{documentPath}\"", "whitespace --verbosity d"),
			equivalenceKey: "FormatFileWhitespace");

		context.RegisterCodeFix(formatFileWhitespaceAction, diagnostic);
	}

	/// <summary>
	/// Registers formatting actions for the entire project.
	/// </summary>
	/// <param name="context">The code fix context.</param>
	/// <param name="diagnostic">The diagnostic to fix.</param>
	/// <param name="projectPath">The path to the project file.</param>
	/// <param name="projectName">The name of the project.</param>
	private static void RegisterProjectFormattingActions(CodeFixContext context, Diagnostic diagnostic, string? projectPath, string projectName)
	{
		// Format entire project
		var formatProjectAction = CodeAction.Create(
			title: $"🏗️ Format Entire Project '{projectName}'",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, projectPath ?? ".", "--severity info --verbosity d"),
			equivalenceKey: "FormatEntireProject");

		context.RegisterCodeFix(formatProjectAction, diagnostic);

		// Quick fix: Format with style only
		var formatStyleAction = CodeAction.Create(
			title: $"⚡ Quick Style Fix '{projectName}'",
			createChangedDocument: _ => ExecuteFormattingCommand(context.Document, projectPath ?? ".", "style --severity suggestion --verbosity d"),
			equivalenceKey: "QuickStyleFix");

		context.RegisterCodeFix(formatStyleAction, diagnostic);
	}

	/// <summary>
	/// Executes the dotnet format command with specified target and arguments.
	/// </summary>
	/// <param name="document">The current document.</param>
	/// <param name="target">The target (file path or project path).</param>
	/// <param name="formatArguments">The format command arguments.</param>
	/// <returns>The original document (command executes externally).</returns>
	private static Task<Document> ExecuteFormattingCommand(Document document, string target, string formatArguments)
	{
		try
		{
			// Determine the working directory
			var workingDirectory = DetermineWorkingDirectory(document, target);

			// Prepare the process
			var processStartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"format {target} {formatArguments}",
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			// Execute the command asynchronously with better error handling
			Task.Run(async () =>
			{
				try
				{
					System.Diagnostics.Debug.WriteLine($"Executing: dotnet format {target} {formatArguments}");
					System.Diagnostics.Debug.WriteLine($"Working Directory: {workingDirectory}");

					using var process = Process.Start(processStartInfo);
					if (process != null)
					{
						// Set timeout to prevent hanging
						var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
						var processTask = Task.Run(() => process.WaitForExit());

						var completedTask = await Task.WhenAny(processTask, timeoutTask);

						if (completedTask == timeoutTask)
						{
							System.Diagnostics.Debug.WriteLine("dotnet format timed out after 2 minutes");
							process.Kill();
							return;
						}

						var output = await process.StandardOutput.ReadToEndAsync();
						var error = await process.StandardError.ReadToEndAsync();

						var success = process.ExitCode == 0;
						var statusIcon = success ? "✅" : "❌";

						System.Diagnostics.Debug.WriteLine($"{statusIcon} dotnet format completed with exit code: {process.ExitCode}");

						if (!string.IsNullOrEmpty(output))
						{
							System.Diagnostics.Debug.WriteLine($"📄 Output:\n{output}");
						}

						if (!string.IsNullOrEmpty(error) && !success)
						{
							System.Diagnostics.Debug.WriteLine($"🔴 Error:\n{error}");
						}

						if (success)
						{
							System.Diagnostics.Debug.WriteLine("🎉 Formatting completed successfully! Files should reload automatically in your IDE.");
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"❌ Error executing dotnet format: {ex.Message}");
					System.Diagnostics.Debug.WriteLine($"💡 Make sure 'dotnet' is available in PATH and the target path is correct.");
				}
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ Error setting up dotnet format: {ex.Message}");
		}

		// Return the original document since formatting happens externally
		// The IDE will reload the formatted files automatically
		return Task.FromResult(document);
	}

	/// <summary>
	/// Determines the appropriate working directory for the format command.
	/// </summary>
	/// <param name="document">The current document.</param>
	/// <param name="target">The target path.</param>
	/// <returns>The working directory path.</returns>
	private static string DetermineWorkingDirectory(Document document, string target)
	{
		// If target is a file path, use its directory
		if (File.Exists(target))
		{
			return Path.GetDirectoryName(target) ?? Environment.CurrentDirectory;
		}

		// If target is a project file, use its directory
		if (target.EndsWith(".csproj") || target.EndsWith(".vbproj") || target.EndsWith(".fsproj"))
		{
			var projectDir = Path.GetDirectoryName(target);
			if (!string.IsNullOrEmpty(projectDir) && Directory.Exists(projectDir))
			{
				return projectDir;
			}
		}

		// Try to use document's directory
		var documentPath = document.FilePath;
		if (!string.IsNullOrEmpty(documentPath))
		{
			var documentDir = Path.GetDirectoryName(documentPath);
			if (!string.IsNullOrEmpty(documentDir))
			{
				return documentDir;
			}
		}

		// Try to find solution directory
		var solutionDir = FindSolutionDirectory();
		if (!string.IsNullOrEmpty(solutionDir))
		{
			return solutionDir;
		}

		// Fallback to current directory
		return Environment.CurrentDirectory;
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

		// Fallback: try to construct from project name and common locations
		var possibleExtensions = new[] { ".csproj", ".vbproj", ".fsproj" };

		// Try in current directory
		foreach (var ext in possibleExtensions)
		{
			var possiblePath = Path.Combine(Environment.CurrentDirectory, $"{project.Name}{ext}");
			if (File.Exists(possiblePath))
			{
				return possiblePath;
			}
		}

		return null;
	}

	/// <summary>
	/// Finds the solution directory by walking up the directory tree.
	/// </summary>
	/// <returns>The solution directory path if found, otherwise current directory.</returns>
	private static string FindSolutionDirectory()
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

		return Environment.CurrentDirectory;
	}
}
