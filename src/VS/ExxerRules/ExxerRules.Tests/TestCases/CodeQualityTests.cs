using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ExxerRules.Analyzers.CodeQuality;
using ExxerRules.Analyzers;
using ExxerRules.Tests.Testing;
using Xunit;
using Shouldly;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for code quality analyzers.
/// SRP: Contains only test cases related to code quality validation.
/// </summary>
public class CodeQualityTests
{
	/// <summary>
	/// Tests that not using regions does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_NotUsingRegions()
	{
		const string testCode = @"
using System;

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

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseRegionsAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using regions reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingRegions()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		#region Fields
		private readonly ILogger<UserService> _logger;
		#endregion

		#region Constructor
		public UserService(ILogger<UserService> logger)
		{
			_logger = logger;
		}
		#endregion

		#region Methods
		public void ProcessUser(int userId)
		{
			_logger.LogInformation(""Processing user with ID {UserId}"", userId);
		}
		#endregion
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseRegionsAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.DoNotUseRegions).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that avoiding magic numbers and strings does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_AvoidingMagicNumbersAndStrings()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		private const int MaxRetryAttempts = 3;
		private const string DefaultErrorMessage = ""An error occurred"";

		public void ProcessUser(int userId)
		{
			for (int i = 0; i < MaxRetryAttempts; i++)
			{
				try
				{
					// Process user
					break;
				}
				catch (Exception ex)
				{
					if (i == MaxRetryAttempts - 1)
						throw new Exception(DefaultErrorMessage, ex);
				}
			}
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that using magic numbers and strings reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingMagicNumbersAndStrings()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser(int userId)
		{
			for (int i = 0; i < 3; i++)
			{
				try
				{
					// Process user
					break;
				}
				catch (Exception ex)
				{
					if (i == 2)
						throw new Exception(""An error occurred"", ex);
				}
			}
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.AvoidMagicNumbersAndStrings).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different region patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentRegionPatterns()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		#region Properties
		public string UserName { get; set; }
		public int UserId { get; set; }
		#endregion

		#region Methods
		public void ProcessUser()
		{
			// Implementation
		}

		public bool IsValid()
		{
			return true;
		}
		#endregion

		#region Private Methods
		private void ValidateUser()
		{
			// Implementation
		}
		#endregion
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DoNotUseRegionsAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(3);
		diagnostics.Any(d => d.Id == DiagnosticIds.DoNotUseRegions).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Different magic number and string patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DifferentMagicNumberAndStringPatterns()
	{
		const string testCode = @"
using System;

namespace TestProject
{
	public class UserService
	{
		public void ProcessUser()
		{
			var maxRetries = 3;
			var timeout = 5000;
			var status = ""active"";
			var role = ""admin"";
			var priority = 1;

			if (maxRetries > 3)
			{
				// Logic
			}

			if (timeout < 5000)
			{
				// Logic
			}

			if (status == ""active"")
			{
				// Logic
			}

			if (role == ""admin"")
			{
				// Logic
			}

			if (priority == 1)
			{
				// Logic
			}
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AvoidMagicNumbersAndStringsAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(5);
		diagnostics.Any(d => d.Id == DiagnosticIds.AvoidMagicNumbersAndStrings).ShouldBeTrue();
	}
}
