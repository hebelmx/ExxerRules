using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.Logging;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for logging analyzers.
/// SRP: Contains only test cases related to logging validation.
/// </summary>
public class LoggingTests
{
	/// <summary>
	/// Tests that using structured logging does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingStructuredLogging()
	{
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class UserService
	{
		private readonly ILogger<UserService> _logger;

		public UserService(ILogger<UserService> logger)
		{
			_logger = logger;
		}

		public void ProcessUser(int userId)
		{
			_logger.LogInformation(""Processing user with ID {UserId}"", userId);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseStructuredLoggingAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using string concatenation reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingStringConcatenation()
	{
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class UserService
	{
		private readonly ILogger<UserService> _logger;

		public UserService(ILogger<UserService> logger)
		{
			_logger = logger;
		}

		public void ProcessUser(int userId)
		{
			_logger.LogInformation(""Processing user with ID "" + userId);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseStructuredLoggingAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseStructuredLogging).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that not using Console.WriteLine does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_NotUsingConsoleWriteLine()
	{
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class UserService
	{
		private readonly ILogger<UserService> _logger;

		public UserService(ILogger<UserService> logger)
		{
			_logger = logger;
		}

		public void ProcessUser(int userId)
		{
			_logger.LogInformation(""Processing user with ID {UserId}"", userId);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseConsoleWriteLineAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using Console.WriteLine reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingConsoleWriteLine()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(int userId)
		{
			Console.WriteLine(""Processing user with ID "" + userId);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseConsoleWriteLineAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.DoNotUseConsoleWriteLine).ShouldBeTrue();
	}

	/// <summary>
	/// Debug test to understand syntax structure.
	/// </summary>
	[Fact]
	public void Debug_StringConcatenationSyntax()
	{
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class UserService
	{
		private readonly ILogger<UserService> _logger;

		public UserService(ILogger<UserService> logger)
		{
			_logger = logger;
		}

		public void ProcessUser(int userId)
		{
			_logger.LogInformation(""Processing user with ID "" + userId);
		}
	}
}";

		// Let's analyze the syntax tree manually
		var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(testCode);
		var root = tree.GetRoot();

		// Find all invocation expressions
		var invocations = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>();

		foreach (var invocation in invocations)
		{
			if (invocation.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccess)
			{
				if (memberAccess.Name.Identifier.ValueText == "LogInformation")
				{
					var arguments = invocation.ArgumentList.Arguments;
					if (arguments.Count > 0)
					{
						var firstArg = arguments[0].Expression;
						// This will help us understand the exact structure
						System.Diagnostics.Debug.WriteLine($"First argument type: {firstArg.GetType().Name}");
						System.Diagnostics.Debug.WriteLine($"First argument: {firstArg}");
					}
				}
			}
		}

		// This test should always pass, it's just for debugging
		true.ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different string concatenation patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentStringConcatenationPatterns()
	{
		const string testCode = @"
using Microsoft.Extensions.Logging;

namespace TestProject
{
	public class UserService
	{
		private readonly ILogger<UserService> _logger;

		public UserService(ILogger<UserService> logger)
		{
			_logger = logger;
		}

		public void ProcessUser(int userId, string userName)
		{
			_logger.LogInformation(""Processing user with ID "" + userId + "" and name "" + userName);
			_logger.LogWarning(""User "" + userName + "" not found"");
			_logger.LogError(""Error processing user: "" + userId);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseStructuredLoggingAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(3);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseStructuredLogging).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Console.WriteLine with different patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_ConsoleWriteLineWithDifferentPatterns()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(int userId, string userName)
		{
			Console.WriteLine(""Processing user with ID "" + userId);
			Console.WriteLine(""User "" + userName + "" processed successfully"");
			Console.WriteLine(""Error: "" + ""User not found"");
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseConsoleWriteLineAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(3);
		diagnostics.Any(d => d.Id == DiagnosticIds.DoNotUseConsoleWriteLine).ShouldBeTrue();
	}
}
