using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.Performance;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for performance analyzers.
/// SRP: Contains only test cases related to performance validation.
/// </summary>
public class PerformanceTests
{
	/// <summary>
	/// Tests that using efficient LINQ does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingEfficientLinq()
	{
		const string testCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUsers(List<User> users)
		{
			var activeUsers = users.Where(u => u.IsActive).ToList();
			var firstUser = users.FirstOrDefault(u => u.Id == 1);
			var userCount = users.Count(u => u.IsActive);
		}
	}

	public class User 
	{ 
		public int Id { get; set; }
		public bool IsActive { get; set; }
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseEfficientLinqAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using inefficient LINQ reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingInefficientLinq()
	{
		const string testCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUsers(List<User> users)
		{
			// Inefficient: multiple enumerations
			var activeUsers = users.Where(u => u.IsActive);
			var count = activeUsers.Count();
			var first = activeUsers.FirstOrDefault();
		}
	}

	public class User 
	{ 
		public int Id { get; set; }
		public bool IsActive { get; set; }
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseEfficientLinqAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseEfficientLinq).ShouldBeTrue();
	}
}
