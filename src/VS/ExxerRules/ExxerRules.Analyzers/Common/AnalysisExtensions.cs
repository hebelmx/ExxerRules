using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExxerRules.Analyzers.Common;

/// <summary>
/// Extension methods for functional analysis operations using FluentResults.
/// </summary>
public static class AnalysisExtensions
{
	/// <summary>
	/// Maps a successful result to a new value using a transformation function.
	/// </summary>
	/// <typeparam name="TSource">The source result type.</typeparam>
	/// <typeparam name="TDestination">The destination result type.</typeparam>
	/// <param name="result">The source result.</param>
	/// <param name="mapper">The transformation function.</param>
	/// <returns>A new result with the transformed value, or the original error if the source failed.</returns>
	public static Result<TDestination> Map<TSource, TDestination>(
		this Result<TSource> result,
		Func<TSource, TDestination> mapper)
	{
		if (result.IsFailed)
		{
			return Result.Fail<TDestination>(result.Errors);
		}

		try
		{
			var mappedValue = mapper(result.Value);
			return AnalysisResult.Success(mappedValue);
		}
		catch (Exception ex)
		{
			return AnalysisResult.Failure<TDestination>(ex);
		}
	}

	/// <summary>
	/// Chains a result-returning function to the current result.
	/// </summary>
	/// <typeparam name="TSource">The source result type.</typeparam>
	/// <typeparam name="TDestination">The destination result type.</typeparam>
	/// <param name="result">The source result.</param>
	/// <param name="binder">The function that returns a new result.</param>
	/// <returns>The result of the binder function, or the original error if the source failed.</returns>
	public static Result<TDestination> Bind<TSource, TDestination>(
		this Result<TSource> result,
		Func<TSource, Result<TDestination>> binder)
	{
		if (result.IsFailed)
		{
			return Result.Fail<TDestination>(result.Errors);
		}

		try
		{
			return binder(result.Value);
		}
		catch (Exception ex)
		{
			return AnalysisResult.Failure<TDestination>(ex);
		}
	}

	/// <summary>
	/// Executes an action on the result value if successful, without changing the result.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The source result.</param>
	/// <param name="action">The action to execute on the value.</param>
	/// <returns>The original result unchanged.</returns>
	public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
	{
		if (result.IsSuccess)
		{
			try
			{
				action(result.Value);
			}
			catch
			{
				// Tap operations shouldn't affect the result
			}
		}
		return result;
	}

	/// <summary>
	/// Provides a default value if the result is failed.
	/// </summary>
	/// <typeparam name="T">The result type.</typeparam>
	/// <param name="result">The source result.</param>
	/// <param name="defaultValue">The default value to use if the result failed.</param>
	/// <returns>A successful result with either the original value or the default value.</returns>
	public static Result<T> IfFailed<T>(this Result<T> result, T defaultValue) => result.IsFailed ? AnalysisResult.Success(defaultValue) : result;

	/// <summary>
	/// Reports a diagnostic if the analysis result indicates a violation.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="result">The analysis result.</param>
	/// <param name="rule">The diagnostic rule to report.</param>
	/// <param name="location">The location to report the diagnostic.</param>
	/// <param name="messageArgs">Arguments for the diagnostic message.</param>
	public static void ReportDiagnosticIfFailed(
		this SyntaxNodeAnalysisContext context,
		Result result,
		DiagnosticDescriptor rule,
		Location location,
		params object?[]? messageArgs)
	{
		if (result.IsFailed)
		{
			var diagnostic = Diagnostic.Create(rule, location, messageArgs);
			context.ReportDiagnostic(diagnostic);
		}
	}

	/// <summary>
	/// Reports a diagnostic if the boolean result is false (indicating a violation).
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="result">The boolean analysis result.</param>
	/// <param name="rule">The diagnostic rule to report.</param>
	/// <param name="location">The location to report the diagnostic.</param>
	/// <param name="messageArgs">Arguments for the diagnostic message.</param>
	public static void ReportDiagnosticIfFalse(
		this SyntaxNodeAnalysisContext context,
		Result<bool> result,
		DiagnosticDescriptor rule,
		Location location,
		params object?[]? messageArgs)
	{
		if (result.IsSuccess && !result.Value)
		{
			var diagnostic = Diagnostic.Create(rule, location, messageArgs);
			context.ReportDiagnostic(diagnostic);
		}
	}

	/// <summary>
	/// Validates that a syntax node is not null.
	/// </summary>
	/// <typeparam name="T">The syntax node type.</typeparam>
	/// <param name="node">The syntax node to validate.</param>
	/// <param name="parameterName">The parameter name for error reporting.</param>
	/// <returns>A result containing the validated node or an error.</returns>
	public static Result<T> ValidateNotNull<T>(this T? node, string parameterName)
		where T : SyntaxNode => node != null
			? AnalysisResult.Success(node)
			: AnalysisResult.Failure<T>($"{parameterName} cannot be null");

	/// <summary>
	/// Safely gets a string representation of a syntax node.
	/// </summary>
	/// <param name="node">The syntax node.</param>
	/// <returns>A result containing the string representation or an error.</returns>
	public static Result<string> ToStringResult(this SyntaxNode? node) => node?.ToString() is string str
			? AnalysisResult.Success(str)
			: AnalysisResult.Failure<string>("Node is null or cannot be converted to string");
}
