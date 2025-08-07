### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
EXXER200 | ExxerRules.NullSafety | Warning | Validate null parameters at method entry
EXXER300 | ExxerRules.Async | Info | Async methods should accept CancellationToken
EXXER400 | ExxerRules.Documentation | Info | Public members should have XML documentation
EXXER500 | ExxerRules.CodeQuality | Warning | Avoid magic numbers and strings
EXXER503 | ExxerRules.CodeQuality | Warning | Do not use regions for code organization
EXXER800 | ExxerRules.Logging | Warning | Use structured logging instead of string concatenation
EXXER801 | ExxerRules.Logging | Warning | Do not use Console.WriteLine in production code