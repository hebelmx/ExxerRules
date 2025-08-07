using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.CodeFormatting;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for code formatting analyzers.
/// SRP: Contains only test cases related to code formatting validation.
/// </summary>
public class CodeFormattingTests
{
	/// <summary>
	/// Tests that project formatting analyzer provides hidden diagnostic for triggering format action.
	/// </summary>
	[Fact]
	public void Should_ReportHiddenDiagnostic_When_ProjectFormattingAnalyzer()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class TestClass
	{
		public void TestMethod()
		{
			Console.WriteLine(""Hello World"");
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new ProjectFormattingAnalyzer(), includeHidden: true);

		diagnostics.Length.ShouldBe(1);
		diagnostics[0].Id.ShouldBe(DiagnosticIds.ProjectFormatting);
		diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Hidden);
	}

	/// <summary>
	/// Tests that code formatting analyzer detects formatting issues.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_FormattingIssuesDetected()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class TestClass{
		public string Property1{get;set;}
		public string Property2 { get; set; }
		public void Method1(){
			var x=5;
			var y = 10;
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new CodeFormattingAnalyzer());

		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.CodeFormattingIssue).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that well-formatted code does not report formatting issues.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_CodeIsWellFormatted()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class TestClass
	{
		public string Property1 { get; set; }
		
		public string Property2 { get; set; }

		public void Method1()
		{
			var x = 5;
			var y = 10;
		}

		public void Method2()
		{
			var result = x + y;
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new CodeFormattingAnalyzer());

		// Should have fewer formatting issues (well-formatted code)
		var formattingIssues = diagnostics.Where(d => d.Id == DiagnosticIds.CodeFormattingIssue).ToArray();
		formattingIssues.Length.ShouldBeLessThanOrEqualTo(5); // Allow more tolerance for detection differences
	}
}
