using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.ErrorHandling;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for error handling analyzers.
/// SRP: Contains only test cases related to error handling validation.
/// </summary>
public class ErrorHandlingTests
{
	/// <summary>
	/// Tests that using Result pattern does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingResultPattern()
	{
		const string testCode = @"
using FluentResults;

namespace TestProject
{
	public class UserService
	{
		public Result<User> GetUser(int id)
		{
			if (id <= 0)
				return Result.Fail(""Invalid ID"");
			
			return Result.Ok(new User { Id = id });
		}
	}

	public class User { public int Id { get; set; } }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseResultPatternAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that throwing exceptions reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_ThrowingExceptions()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public User GetUser(int id)
		{
			if (id <= 0)
				throw new ArgumentException(""Invalid ID"");
			
			return new User { Id = id };
		}
	}

	public class User { public int Id { get; set; } }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseResultPatternAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseResultPattern).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that avoiding throw statements does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_AvoidingThrowStatements()
	{
		const string testCode = @"
using FluentResults;

namespace TestProject
{
	public class UserService
	{
		public Result<User> GetUser(int id)
		{
			if (id <= 0)
				return Result.Fail(""Invalid ID"");
			
			return Result.Ok(new User { Id = id });
		}
	}

	public class User { public int Id { get; set; } }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AvoidThrowingExceptionsAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using throw statements reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingThrowStatements()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public User GetUser(int id)
		{
			if (id <= 0)
				throw new ArgumentException(""Invalid ID"");
			
			return new User { Id = id };
		}
	}

	public class User { public int Id { get; set; } }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AvoidThrowingExceptionsAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.AvoidThrowingExceptions).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different exception throwing patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentExceptionThrowingPatterns()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(string userName)
		{
			if (string.IsNullOrEmpty(userName))
			{
				throw new ArgumentException(""User name cannot be null or empty"");
			}

			if (userName.Length < 3)
			{
				throw new InvalidOperationException(""User name too short"");
			}

			if (userName.Length > 50)
			{
				throw new ArgumentOutOfRangeException(""User name too long"");
			}
		}

		public void ValidateUser(int userId)
		{
			if (userId <= 0)
			{
				throw new ArgumentException(""User ID must be positive"");
			}

			if (userId > 1000000)
			{
				throw new ArgumentOutOfRangeException(""User ID too large"");
			}
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AvoidThrowingExceptionsAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(5);
		diagnostics.Any(d => d.Id == DiagnosticIds.AvoidThrowingExceptions).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different Result pattern usage.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_DifferentResultPatternUsage()
	{
		const string testCode = @"
using System;
using FluentResults;

namespace TestProject
{
	public class UserService
	{
		public Result ProcessUser(string userName)
		{
			if (string.IsNullOrEmpty(userName))
			{
				return Result.Failure(""User name cannot be null or empty"");
			}

			if (userName.Length < 3)
			{
				return Result.Failure(""User name too short"");
			}

			if (userName.Length > 50)
			{
				return Result.Failure(""User name too long"");
			}

			return Result.Success();
		}

		public Result<int> ValidateUser(int userId)
		{
			if (userId <= 0)
			{
				return Result.Failure<int>(""User ID must be positive"");
			}

			if (userId > 1000000)
			{
				return Result.Failure<int>(""User ID too large"");
			}

			return Result.Success(userId);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseResultPatternAnalyzer());
		// Should not report diagnostic when using Result pattern
		diagnostics.Length.ShouldBe(0);
	}
}
