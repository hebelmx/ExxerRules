using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.Testing;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for testing standards analyzers.
/// SRP: Contains only test cases related to testing standards validation.
/// </summary>
public class TestingStandardsTests
{
	/// <summary>
	/// Tests that valid test naming convention does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_TestMethodFollowsNamingConvention()
	{
		const string testCode = @"
using Xunit;

namespace TestProject
{
	public class CalculatorTests
	{
		[Fact]
		public void Should_Add_When_TwoPositiveNumbers()
		{
			// Test implementation
		}

		[Theory]
		public void Should_Subtract_When_FirstNumberIsLarger()
		{
			// Test implementation
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new TestNamingConventionAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that invalid test naming convention reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_TestMethodDoesNotFollowNamingConvention()
	{
		const string testCode = @"
using Xunit;

namespace TestProject
{
	public class CalculatorTests
	{
		[Fact]
		public void AddTest()
		{
			// Test implementation
		}

		[Theory]
		public void TestSubtraction()
		{
			// Test implementation
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new TestNamingConventionAnalyzer());
		diagnostics.Length.ShouldBe(2);
		diagnostics.All(d => d.Id == DiagnosticIds.TestNamingConvention).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that using Moq reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingMoq()
	{
		const string testCode = @"
using Moq;
using System;

namespace TestProject
{
	public class TestClass
	{
		public void TestMethod()
		{
			// Test implementation
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseMoqAnalyzer());
		diagnostics.Length.ShouldBe(1);
		diagnostics[0].Id.ShouldBe(DiagnosticIds.UseNSubstitute);
	}

	/// <summary>
	/// Tests that using NSubstitute does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingNSubstitute()
	{
		const string testCode = @"
using NSubstitute;
using System;

namespace TestProject
{
	public class TestClass
	{
		public void TestMethod()
		{
			// Test implementation using NSubstitute
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseMoqAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using FluentAssertions reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingFluentAssertions()
	{
		const string testCode = @"
using FluentAssertions;
using System;

namespace TestProject
{
	public class TestClass
	{
		public void TestMethod()
		{
			// Test implementation
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseFluentAssertionsAnalyzer());
		diagnostics.Length.ShouldBe(1);
		diagnostics[0].Id.ShouldBe(DiagnosticIds.UseShouldly);
	}

	/// <summary>
	/// Tests that using Shouldly does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingShouldly()
	{
		const string testCode = @"
using Shouldly;
using System;

namespace TestProject
{
	public class TestClass
	{
		public void TestMethod()
		{
			// Test implementation using Shouldly
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseFluentAssertionsAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}
}
