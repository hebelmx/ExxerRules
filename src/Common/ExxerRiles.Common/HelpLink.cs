namespace ExxeRules
{
    public static class HelpLink
    {
        public static string ForDiagnostic(DiagnosticId diagnosticId) =>
            $"https://ExxeRules.github.io/diagnostics/{diagnosticId.ToDiagnosticId()}.html";
    }
}
