# 🔧 ExxerRules Formatting Actions - User Guide

## 🎯 **Overview**

ExxerRules now includes powerful **code formatting actions** that integrate `dotnet format` directly into your IDE workflow. These actions allow you to format your code on-demand with various options and scopes.

## ⚡ **Features**

### **1. Project Formatting Trigger (EXXER900)**
- **Type**: Hidden diagnostic (always available)
- **Purpose**: Provides formatting actions in the IDE
- **Location**: Available on every file
- **Actions Available**:
  - 🔧 **Format Project** - Full project formatting
  - 📝 **Format Whitespace Only** - Whitespace and indentation
  - 🎨 **Format Style** - Code style rules only  
  - 🔍 **Format with Analyzers** - All analyzer rules

### **2. Formatting Issue Detection (EXXER901)**
- **Type**: Info diagnostic
- **Purpose**: Detects common formatting inconsistencies
- **Triggers**: When formatting issues are detected
- **Actions Available**:
  - 📄 **Format Current File** - Format just the active file
  - 📝 **Format File Whitespace** - Whitespace in current file only
  - 🏗️ **Format Entire Project** - Full project formatting
  - ⚡ **Quick Style Fix** - Fast style-only formatting

## 🚀 **How to Use**

### **Method 1: Light Bulb Actions**
1. Open any C# file in your project
2. Look for the light bulb icon (💡) or press `Ctrl+.` (Windows) / `Cmd+.` (Mac)
3. Select from available formatting actions:
   ```
   🔧 Format Project 'MyProject' (dotnet format)
   📝 Format Whitespace Only 'MyProject' 
   🎨 Format Style 'MyProject'
   🔍 Format with Analyzers 'MyProject'
   ```

### **Method 2: Quick Fix Menu**
1. Right-click in any C# file
2. Select "Quick Actions and Refactorings..."
3. Choose your preferred formatting action

### **Method 3: Formatting Issue Detection**
1. Write code with formatting inconsistencies
2. ExxerRules will detect issues and show info diagnostics
3. Click the light bulb to see contextual formatting actions

## 📋 **Available Commands**

| Action | Command Executed | Scope | Description |
|--------|-----------------|-------|-------------|
| **Format Project** | `dotnet format [project] --severity info --verbosity d` | Entire Project | Complete formatting with all rules |
| **Format Whitespace** | `dotnet format [target] whitespace --verbosity d` | Project/File | Whitespace and indentation only |
| **Format Style** | `dotnet format [target] style --severity info --verbosity d` | Project/File | Code style rules only |
| **Format Analyzers** | `dotnet format [target] analyzers --severity info --verbosity d` | Project/File | All analyzer-based rules |
| **Format Current File** | `dotnet format "[file.cs]" --verbosity d` | Single File | Format specific file only |
| **Quick Style Fix** | `dotnet format [project] style --severity suggestion --verbosity d` | Project | Fast style fixes only |

## 🛠️ **Configuration**

### **EditorConfig Integration**
All formatting actions respect your `.editorconfig` settings:

```ini
# Example .editorconfig for ExxerRules formatting
root = true

[*.cs]
# Indentation
indent_style = tab
indent_size = 4

# Code style rules
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true

# ExxerRules formatting
dotnet_analyzer_diagnostic.EXXER900.severity = hidden    # Always available
dotnet_analyzer_diagnostic.EXXER901.severity = info      # Show formatting issues
```

### **MSBuild Configuration**
Customize formatting behavior in your project file:

```xml
<PropertyGroup>
  <!-- Enable ExxerRules formatting -->
  <EnableExxerFormattingActions>true</EnableExxerFormattingActions>
  
  <!-- Configure formatting scope -->
  <ExxerFormattingDefaultScope>project</ExxerFormattingDefaultScope>
  
  <!-- Set default verbosity -->
  <ExxerFormattingVerbosity>diagnostic</ExxerFormattingVerbosity>
</PropertyGroup>
```

## 🎨 **Example Workflow**

### **Scenario**: Cleaning up messy code

**1. Before Formatting:**
```csharp
using System;
namespace MyProject{
public class BadlyFormatted{
public string Property1{get;set;}
public void Method1(){
var x=5;var y=10;
if(x>y){
Console.WriteLine("Bad formatting");
}
}
}
}
```

**2. Use ExxerRules Formatting:**
- Press `Ctrl+.` in the file
- Select "🔧 Format Project 'MyProject'"
- ExxerRules executes: `dotnet format MyProject.csproj --severity info --verbosity d`

**3. After Formatting:**
```csharp
using System;

namespace MyProject
{
	public class BadlyFormatted
	{
		public string Property1 { get; set; }

		public void Method1()
		{
			var x = 5;
			var y = 10;
			
			if (x > y)
			{
				Console.WriteLine("Bad formatting");
			}
		}
	}
}
```

## 🔍 **Troubleshooting**

### **Common Issues**

**❌ "dotnet command not found"**
- Ensure .NET SDK is installed and in your PATH
- Verify with: `dotnet --version`

**❌ "No changes made"**
- Your code might already be properly formatted
- Check your `.editorconfig` settings
- Try a more specific formatting action

**❌ "Timeout after 2 minutes"**
- Large projects may take time to format
- Consider using file-specific actions for faster feedback
- Check for any infinite loops in custom analyzers

### **Debug Information**
Formatting commands output debug information to the Output window:
```
✅ dotnet format completed with exit code: 0
📄 Output:
Formatting code files in workspace 'MyProject.csproj'.
Fixed 5 files in 1.2 seconds.
🎉 Formatting completed successfully!
```

## ⚙️ **Advanced Usage**

### **Custom Formatting Rules**
Create custom formatting rules by extending the analyzers:

```csharp
// Custom formatting analyzer
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CustomFormattingAnalyzer : DiagnosticAnalyzer
{
    // Implement your custom formatting logic
}
```

### **CI/CD Integration**
Use the same commands in your build pipeline:

```yaml
- name: Format Code
  run: dotnet format --verify-no-changes --severity info --verbosity diagnostic
```

### **Team Guidelines**
Establish team formatting standards:

```markdown
# Team Formatting Standards
1. Use ExxerRules formatting actions before committing
2. Always run "Format Project" for consistency
3. Configure your IDE to show EXXER901 as warnings
4. Set up pre-commit hooks with dotnet format
```

## 🎉 **Benefits**

### **For Developers**
- ✅ **One-click formatting** directly in IDE
- ✅ **Multiple formatting scopes** (file, project, solution)
- ✅ **Consistent code style** across team
- ✅ **No need to memorize commands**

### **For Teams**
- ✅ **Automatic code consistency**
- ✅ **Reduced code review time**
- ✅ **Integrated with existing tools**
- ✅ **Configurable per project**

### **For Projects**
- ✅ **Maintainable codebase**
- ✅ **Professional appearance**
- ✅ **Better readability**
- ✅ **Faster onboarding**

---

**Made with ❤️ by the ExxerAI team using integrated `dotnet format` workflow**

*"Well-formatted code is a sign of professionalism and attention to detail."*