param()

# Pack the project and ignore the exit code
dotnet pack "F:\Dynamic\ExxerRules\ExxerRules\src\VS\ExxerRules\ExxerRules.NuGet\ExxerRules.NuGet.csproj" -c Release -p:NoWarn=NU5017

# Verify the package was created
$packagePath = "F:\Dynamic\ExxerRules\ExxerRules\src\VS\ExxerRules\ExxerRules.NuGet\bin\Release\ExxerRules.1.0.0.nupkg"
if (Test-Path $packagePath) {
    Write-Host "Package created successfully at $packagePath"
    exit 0
} else {
    Write-Host "Package not found!"
    exit 1
}
