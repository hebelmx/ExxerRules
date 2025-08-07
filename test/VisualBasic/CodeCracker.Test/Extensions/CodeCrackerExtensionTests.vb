Imports Xunit

Namespace Extensions
    Public Class ExxeRulesExtensionTests
        <Fact>
        Public Sub CanFormatDiagnosticIdAsEnum()
            Const id = DiagnosticId.ArgumentException
            Assert.Equal("CC0002", id.ToDiagnosticId())
        End Sub
    End Class
End Namespace