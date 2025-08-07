using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.ModernCSharp;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for modern C# analyzers.
/// SRP: Contains only test cases related to modern C# validation.
/// </summary>
public class ModernCSharpTests
{
	/// <summary>
	/// Tests that using modern pattern matching does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingModernPatternMatching()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public string ProcessUser(object user)
		{
			return user switch
			{
				User u => $""User: {u.Name}"",
				string s => $""String: {s}"",
				_ => ""Unknown""
			};
		}
	}

	public class User { public string Name { get; set; } = """"; }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseModernPatternMatchingAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using old pattern matching reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingOldPatternMatching()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public string ProcessUser(object user)
		{
			if (user is User)
			{
				var u = (User)user;
				return $""User: {u.Name}"";
			}
			else if (user is string)
			{
				var s = (string)user;
				return $""String: {s}"";
			}
			else
			{
				return ""Unknown"";
			}
		}
	}

	public class User { public string Name { get; set; } = """"; }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseModernPatternMatchingAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseModernPatternMatching).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that using expression-bodied members does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingExpressionBodiedMembers()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		private readonly string _name;

		public UserService(string name) => _name = name;

		public string GetName() => _name;

		public string FullName => $""User: {_name}"";

		public void Process() => Console.WriteLine(_name);
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that not using expression-bodied members reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_NotUsingExpressionBodiedMembers()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		private readonly string _name;

		public UserService(string name)
		{
			_name = name;
		}

		public string GetName()
		{
			return _name;
		}

		public string FullName
		{
			get
			{
				return $""User: {_name}"";
			}
		}

		public void Process()
		{
			Console.WriteLine(_name);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseExpressionBodiedMembers).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different pattern matching scenarios.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentPatternMatchingScenarios()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class User
	{
		public string Name { get; set; }
	}

	public class Admin
	{
		public string Name { get; set; }
	}

	public class UserService
	{
		public void ProcessUser(object user)
		{
			if (user is User)
			{
				var u = (User)user;
				Console.WriteLine(u.Name);
			}

			if (user is Admin)
			{
				var a = (Admin)user;
				Console.WriteLine(a.Name);
			}
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseModernPatternMatchingAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(2);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseModernPatternMatching).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Expression-bodied members in different contexts.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_ExpressionBodiedMembersInDifferentContexts()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		private string _name;

		public string GetName()
		{
			return _name;
		}

		public void SetName(string name)
		{
			_name = name;
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(_name);
		}

		public int GetLength()
		{
			return _name.Length;
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseExpressionBodiedMembersAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(3);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseExpressionBodiedMembers).ShouldBeTrue();
	}
}
