using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ExxerRules.Analyzers.NullSafety;
using ExxerRules.Analyzers;
using ExxerRules.Tests.Testing;
using Xunit;
using Shouldly;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for null safety analyzers.
/// SRP: Contains only test cases related to null safety validation.
/// </summary>
public class NullSafetyTests
{
	/// <summary>
	/// Tests that validating null parameters does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_ValidatingNullParameters()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(string userName)
		{
			if (userName == null)
				throw new ArgumentNullException(nameof(userName));

			// Process user
		}

		public string GetUserInfo(string userId)
		{
			ArgumentNullException.ThrowIfNull(userId);

			return $""User: {userId}"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that not validating null parameters reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_NotValidatingNullParameters()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(string userName)
		{
			// No null validation
			var length = userName.Length;
		}

		public string GetUserInfo(string userId)
		{
			// No null validation
			return $""User: {userId}"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.ValidateNullParameters).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different null parameter patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentNullParameterPatterns()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(string userName, int userId, object userData)
		{
			// Missing null checks
			Console.WriteLine(userName.Length);
			Console.WriteLine(userId.ToString());
			Console.WriteLine(userData.ToString());
		}

		public void ValidateUser(string name, string email, string phone)
		{
			// Missing null checks
			var isValid = name.Length > 0 && email.Contains(""@"") && phone.Length == 10;
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(5);
		diagnostics.Any(d => d.Id == DiagnosticIds.ValidateNullParameters).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Null parameter validation in different contexts.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_NullParameterValidationInDifferentContexts()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(string userName, int userId, object userData)
		{
			if (userName == null) throw new ArgumentNullException(nameof(userName));
			if (userData == null) throw new ArgumentNullException(nameof(userData));

			Console.WriteLine(userName.Length);
			Console.WriteLine(userId.ToString());
			Console.WriteLine(userData.ToString());
		}

		public void ValidateUser(string name, string email, string phone)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (email == null) throw new ArgumentNullException(nameof(email));
			if (phone == null) throw new ArgumentNullException(nameof(phone));

			var isValid = name.Length > 0 && email.Contains(""@"") && phone.Length == 10;
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new ValidateNullParametersAnalyzer());
		// Should not report diagnostic when null checks are present
		diagnostics.Length.ShouldBe(0);
	}
}
