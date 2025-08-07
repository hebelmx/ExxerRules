namespace ExxerRules.Analyzers;

/// <summary>
/// Defines diagnostic IDs for ExxerRules analyzers.
/// </summary>
public static class DiagnosticIds
{
	// Error Handling (EXXER001-EXXER099)
	/// <summary>
	/// Methods should return Result&lt;T&gt; instead of throwing exceptions.
	/// </summary>
	public const string UseResultPattern = "EXXER001";

	/// <summary>
	/// Exception throwing detected in method that should return Result&lt;T&gt;.
	/// </summary>
	public const string AvoidThrowingExceptions = "EXXER002";

	// Testing (EXXER100-EXXER199)
	/// <summary>
	/// Test methods should follow naming convention: Should_Action_When_Condition.
	/// </summary>
	public const string TestNamingConvention = "EXXER100";

	/// <summary>
	/// Use XUnit v3 for testing.
	/// </summary>
	public const string UseXUnitV3 = "EXXER101";

	/// <summary>
	/// Use Shouldly for assertions instead of FluentAssertions.
	/// </summary>
	public const string UseShouldly = "EXXER102";

	/// <summary>
	/// Use NSubstitute for mocking instead of Moq.
	/// </summary>
	public const string UseNSubstitute = "EXXER103";

	/// <summary>
	/// Do not mock EF Core DbContext, use InMemory provider.
	/// </summary>
	public const string DoNotMockDbContext = "EXXER104";

	// Null Safety (EXXER200-EXXER299)
	/// <summary>
	/// Validate null parameters at method entry.
	/// </summary>
	public const string ValidateNullParameters = "EXXER200";

	/// <summary>
	/// Use null safety patterns for Result&lt;T&gt;.
	/// </summary>
	public const string UseNullSafetyPatterns = "EXXER201";

	// Async (EXXER300-EXXER399)
	/// <summary>
	/// Async methods should accept CancellationToken.
	/// </summary>
	public const string AsyncMethodsShouldAcceptCancellationToken = "EXXER300";

	/// <summary>
	/// Use ConfigureAwait(false) in library code.
	/// </summary>
	public const string UseConfigureAwaitFalse = "EXXER301";

	/// <summary>
	/// Avoid async void methods except for event handlers.
	/// </summary>
	public const string AvoidAsyncVoid = "EXXER302";

	// Documentation (EXXER400-EXXER499)
	/// <summary>
	/// Public members should have XML documentation.
	/// </summary>
	public const string PublicMembersShouldHaveXmlDocumentation = "EXXER400";

	// Code Quality (EXXER500-EXXER599)
	/// <summary>
	/// Avoid magic numbers and strings.
	/// </summary>
	public const string AvoidMagicNumbersAndStrings = "EXXER500";

	/// <summary>
	/// Use expression-bodied members where appropriate.
	/// </summary>
	public const string UseExpressionBodiedMembers = "EXXER501";

	/// <summary>
	/// Private fields should use camelCase without underscore.
	/// </summary>
	public const string PrivateFieldNaming = "EXXER502";

	/// <summary>
	/// Do not use regions in code.
	/// </summary>
	public const string DoNotUseRegions = "EXXER503";

	// Architecture (EXXER600-EXXER699)
	/// <summary>
	/// Domain layer should not reference Infrastructure.
	/// </summary>
	public const string DomainShouldNotReferenceInfrastructure = "EXXER600";

	/// <summary>
	/// Use repository pattern with focused interfaces.
	/// </summary>
	public const string UseRepositoryPattern = "EXXER601";

	// Performance (EXXER700-EXXER799)
	/// <summary>
	/// Use efficient LINQ operations.
	/// </summary>
	public const string UseEfficientLinq = "EXXER700";

	/// <summary>
	/// Dispose resources properly with using statements.
	/// </summary>
	public const string DisposeResourcesProperly = "EXXER701";

	// Logging (EXXER800-EXXER899)
	/// <summary>
	/// Use structured logging instead of string concatenation.
	/// </summary>
	public const string UseStructuredLogging = "EXXER800";

	/// <summary>
	/// Do not use Console.WriteLine in production code.
	/// </summary>
	public const string DoNotUseConsoleWriteLine = "EXXER801";
}