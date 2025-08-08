using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.Documentation;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for documentation analyzers.
/// SRP: Contains only test cases related to documentation validation.
/// </summary>
public class DocumentationTests
{
	/// <summary>
	/// Tests that public members with XML documentation do not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_PublicMembersHaveXmlDocumentation()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	/// <summary>
	/// Represents a user service for managing user operations.
	/// </summary>
	public class UserService
	{
		/// <summary>
		/// Gets or sets the user identifier.
		/// </summary>
		public int UserId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref=""UserService""/> class.
		/// </summary>
		/// <param name=""userId"">The user identifier.</param>
		public UserService(int userId)
		{
			UserId = userId;
		}

		/// <summary>
		/// Processes the user with the specified identifier.
		/// </summary>
		/// <param name=""userId"">The user identifier to process.</param>
		/// <returns>The processed user result.</returns>
		public string ProcessUser(int userId)
		{
			return $""Processed user {userId}"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that public members without XML documentation report diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_PublicMembersDoNotHaveXmlDocumentation()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public int UserId { get; set; }

		public UserService(int userId)
		{
			UserId = userId;
		}

		public string ProcessUser(int userId)
		{
			return $""Processed user {userId}"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.PublicMembersShouldHaveXmlDocumentation).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different public member patterns without documentation.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentPublicMemberPatternsWithoutDocumentation()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public string UserName { get; set; }

		public int UserId { get; set; }

		public void ProcessUser()
		{
			// Implementation
		}

		public bool IsValid()
		{
			return true;
		}

		public string GetUserData()
		{
			return ""data"";
		}
	}

	public interface IUserRepository
	{
		void GetUser();
		bool IsValid();
		string GetData();
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(10);
		diagnostics.Any(d => d.Id == DiagnosticIds.PublicMembersShouldHaveXmlDocumentation).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Public members with different documentation patterns.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_PublicMembersWithDifferentDocumentationPatterns()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	/// <summary>
	/// User service for processing user data.
	/// </summary>
	public class UserService
	{
		/// <summary>
		/// Gets or sets the user name.
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Gets or sets the user ID.
		/// </summary>
		public int UserId { get; set; }

		/// <summary>
		/// Processes the user data.
		/// </summary>
		public void ProcessUser()
		{
			// Implementation
		}

		/// <summary>
		/// Validates the user data.
		/// </summary>
		/// <returns>True if valid, false otherwise.</returns>
		public bool IsValid()
		{
			return true;
		}

		/// <summary>
		/// Gets the user data.
		/// </summary>
		/// <returns>The user data string.</returns>
		public string GetUserData()
		{
			return ""data"";
		}
	}

	/// <summary>
	/// User repository interface.
	/// </summary>
	public interface IUserRepository
	{
		/// <summary>
		/// Gets the user.
		/// </summary>
		void GetUser();

		/// <summary>
		/// Validates the user.
		/// </summary>
		/// <returns>True if valid, false otherwise.</returns>
		bool IsValid();

		/// <summary>
		/// Gets the data.
		/// </summary>
		/// <returns>The data string.</returns>
		string GetData();
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new PublicMembersShouldHaveXmlDocumentationAnalyzer());
		// Should not report diagnostic when documentation is present
		diagnostics.Length.ShouldBe(0);
	}
}
