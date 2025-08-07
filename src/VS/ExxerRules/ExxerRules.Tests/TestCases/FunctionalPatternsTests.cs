using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.FunctionalPatterns;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for functional patterns analyzers.
/// SRP: Contains only test cases related to functional programming patterns.
/// </summary>
public class FunctionalPatternsTests
{
	/// <summary>
	/// Tests that throw statements report diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_ThrowStatementIsUsed()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class TestClass
	{
		public void TestMethod()
		{
			throw new Exception(""This should be flagged"");
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());
		diagnostics.Length.ShouldBe(1);
		diagnostics[0].Id.ShouldBe(DiagnosticIds.DoNotThrowExceptions);
	}

	/// <summary>
	/// Tests that returning Result&lt;T&gt; does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_ReturningResult()
	{
		const string testCode = @"
using FluentResults;

namespace TestProject
{
	public class TestClass
	{
		public Result<string> TestMethod()
		{
			return Result.Ok(""Success"");
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that throwing instead of returning Result&lt;T&gt; reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_ThrowingInsteadOfReturningResult()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class TestClass
	{
		public string ProcessData(string input)
		{
			if (string.IsNullOrEmpty(input))
				throw new ArgumentException(""Input cannot be null"");
			
			return input.ToUpper();
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());
		diagnostics.Length.ShouldBe(1);
		diagnostics[0].Id.ShouldBe(DiagnosticIds.DoNotThrowExceptions);
	}

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
	public class TestClass
	{
		public Result<string> ProcessData(string input)
		{
			if (string.IsNullOrEmpty(input))
				return Result.Fail(""Input cannot be null"");
			
			return Result.Ok(input.ToUpper());
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotThrowExceptionsAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}
}
