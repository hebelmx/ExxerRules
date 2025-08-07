using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ExxerRules.Analyzers.Async;
using ExxerRules.Analyzers;
using ExxerRules.Tests.Testing;
using Xunit;
using Shouldly;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for async analyzers.
/// SRP: Contains only test cases related to async programming validation.
/// </summary>
public class AsyncTests
{
	/// <summary>
	/// Tests that using ConfigureAwait(false) does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingConfigureAwaitFalse()
	{
		const string testCode = @"
using System;
using System.Threading.Tasks;

namespace TestProject
{
	public class AsyncService
	{
		public async Task<string> GetDataAsync()
		{
			await Task.Delay(100).ConfigureAwait(false);
			return ""data"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseConfigureAwaitFalseAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that not using ConfigureAwait(false) reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_NotUsingConfigureAwaitFalse()
	{
		const string testCode = @"
using System;
using System.Threading.Tasks;

namespace TestProject
{
	public class AsyncService
	{
		public async Task<string> GetDataAsync()
		{
			await Task.Delay(100);
			return ""data"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseConfigureAwaitFalseAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseConfigureAwaitFalse).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that async methods accepting CancellationToken do not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_AsyncMethodAcceptsCancellationToken()
	{
		const string testCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
	public class AsyncService
	{
		public async Task<string> GetDataAsync(CancellationToken cancellationToken = default)
		{
			await Task.Delay(100, cancellationToken).ConfigureAwait(false);
			return ""data"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that async methods not accepting CancellationToken report diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_AsyncMethodDoesNotAcceptCancellationToken()
	{
		const string testCode = @"
using System;
using System.Threading.Tasks;

namespace TestProject
{
	public class AsyncService
	{
		public async Task<string> GetDataAsync()
		{
			await Task.Delay(100).ConfigureAwait(false);
			return ""data"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.AsyncMethodsShouldAcceptCancellationToken).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Async method with different naming patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_AsyncMethodWithDifferentNamingPatterns()
	{
		const string testCode = @"
using System.Threading.Tasks;

namespace TestProject
{
	public class UserService
	{
		public async Task ProcessUserAsync()
		{
			await Task.Delay(100);
		}

		public async Task GetUserDataAsync()
		{
			await Task.Delay(100);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(2);
		diagnostics.Any(d => d.Id == DiagnosticIds.AsyncMethodsShouldAcceptCancellationToken).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Async method with event handler naming.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_AsyncMethodWithEventHandlerNaming()
	{
		const string testCode = @"
using System.Threading.Tasks;

namespace TestProject
{
	public class UserService
	{
		public async Task OnUserChangedAsync()
		{
			await Task.Delay(100);
		}

		public async Task Button_ClickAsync()
		{
			await Task.Delay(100);
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new AsyncMethodsShouldAcceptCancellationTokenAnalyzer());
		// Event handlers should be exempted
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests edge case: ConfigureAwait usage in different contexts.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_ConfigureAwaitMissingInDifferentContexts()
	{
		const string testCode = @"
using System.Threading.Tasks;

namespace TestProject
{
	public class UserService
	{
		public async Task ProcessUserAsync()
		{
			var result = await GetUserDataAsync();
			var processed = await ProcessDataAsync(result);
		}

		private async Task<string> GetUserDataAsync()
		{
			await Task.Delay(100);
			return ""data"";
		}

		private async Task<string> ProcessDataAsync(string data)
		{
			await Task.Delay(100);
			return data + ""processed"";
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseConfigureAwaitFalseAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(3);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseConfigureAwaitFalse).ShouldBeTrue();
	}
}
