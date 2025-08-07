import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { spawn } from 'child_process';

export function activate(context: vscode.ExtensionContext) {
    console.log('ExxerRules extension is now active!');

    // Register commands
    const analyzeWorkspaceCommand = vscode.commands.registerCommand('exxerRules.analyzeWorkspace', async () => {
        await analyzeWorkspace();
    });

    const showAnalyzerInfoCommand = vscode.commands.registerCommand('exxerRules.showAnalyzerInfo', () => {
        showAnalyzerInfo();
    });

    // Register configuration change handler
    const configChangeHandler = vscode.workspace.onDidChangeConfiguration(event => {
        if (event.affectsConfiguration('exxerRules')) {
            handleConfigurationChange();
        }
    });

    // Setup analyzer integration on activation
    setupAnalyzerIntegration(context);

    context.subscriptions.push(
        analyzeWorkspaceCommand,
        showAnalyzerInfoCommand,
        configChangeHandler
    );

    // Show welcome message on first activation
    showWelcomeMessage(context);
}

async function setupAnalyzerIntegration(context: vscode.ExtensionContext) {
    const config = vscode.workspace.getConfiguration('exxerRules');
    const enabled = config.get<boolean>('enabled', true);

    if (!enabled) {
        console.log('ExxerRules analyzers are disabled');
        return;
    }

    // Get analyzer DLL paths from extension
    const extensionPath = context.extensionPath;
    const analyzerPath = path.join(extensionPath, 'analyzers');
    
    if (!fs.existsSync(analyzerPath)) {
        vscode.window.showWarningMessage('ExxerRules analyzer files not found. Extension may not be properly installed.');
        return;
    }

    // Configure analyzers for all C# projects in workspace
    await configureAnalyzersForWorkspace(analyzerPath);
}

async function configureAnalyzersForWorkspace(analyzerPath: string) {
    const workspaceFolders = vscode.workspace.workspaceFolders;
    if (!workspaceFolders) {
        return;
    }

    for (const folder of workspaceFolders) {
        await configureAnalyzersForFolder(folder.uri.fsPath, analyzerPath);
    }
}

async function configureAnalyzersForFolder(folderPath: string, analyzerPath: string) {
    // Find all .csproj files
    const csprojFiles = await findCsprojFiles(folderPath);
    
    for (const csprojFile of csprojFiles) {
        await addAnalyzerToProject(csprojFile, analyzerPath);
    }
}

async function findCsprojFiles(folderPath: string): Promise<string[]> {
    const csprojFiles: string[] = [];
    
    const findFiles = async (dir: string) => {
        const entries = await fs.promises.readdir(dir, { withFileTypes: true });
        
        for (const entry of entries) {
            const fullPath = path.join(dir, entry.name);
            
            if (entry.isDirectory() && entry.name !== 'node_modules' && entry.name !== '.git') {
                await findFiles(fullPath);
            } else if (entry.isFile() && entry.name.endsWith('.csproj')) {
                csprojFiles.push(fullPath);
            }
        }
    };
    
    try {
        await findFiles(folderPath);
    } catch (error) {
        console.error('Error finding csproj files:', error);
    }
    
    return csprojFiles;
}

async function addAnalyzerToProject(csprojPath: string, analyzerPath: string) {
    try {
        const content = await fs.promises.readFile(csprojPath, 'utf-8');
        
        // Check if ExxerRules analyzer is already referenced
        if (content.includes('ExxerRules.Analyzers')) {
            return; // Already configured
        }

        // Add analyzer references to the project
        const analyzerDll = path.join(analyzerPath, 'ExxerRules.Analyzers.dll');
        const codeFixesDll = path.join(analyzerPath, 'ExxerRules.CodeFixes.dll');
        
        const analyzerItemGroup = `
  <ItemGroup>
    <Analyzer Include="${analyzerDll}" />
    <Analyzer Include="${codeFixesDll}" />
  </ItemGroup>`;

        // Insert before closing </Project> tag
        const updatedContent = content.replace('</Project>', `${analyzerItemGroup}
</Project>`);

        await fs.promises.writeFile(csprojPath, updatedContent);
        console.log(`Added ExxerRules analyzers to ${csprojPath}`);
        
    } catch (error) {
        console.error(`Error adding analyzers to ${csprojPath}:`, error);
    }
}

async function analyzeWorkspace() {
    const workspaceFolders = vscode.workspace.workspaceFolders;
    if (!workspaceFolders) {
        vscode.window.showErrorMessage('No workspace folder is open');
        return;
    }

    vscode.window.showInformationMessage('Running ExxerRules analysis on workspace...');
    
    // Trigger dotnet build to run analyzers
    for (const folder of workspaceFolders) {
        await runDotnetBuild(folder.uri.fsPath);
    }
}

async function runDotnetBuild(folderPath: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const process = spawn('dotnet', ['build', '--verbosity', 'normal'], {
            cwd: folderPath,
            shell: true
        });

        let output = '';
        let errorOutput = '';

        process.stdout?.on('data', (data) => {
            output += data.toString();
        });

        process.stderr?.on('data', (data) => {
            errorOutput += data.toString();
        });

        process.on('close', (code) => {
            if (code === 0) {
                vscode.window.showInformationMessage('ExxerRules analysis completed successfully');
                console.log('Build output:', output);
            } else {
                vscode.window.showWarningMessage(`Build completed with warnings/errors (exit code: ${code})`);
                console.error('Build error output:', errorOutput);
            }
            resolve();
        });

        process.on('error', (error) => {
            vscode.window.showErrorMessage(`Failed to run dotnet build: ${error.message}`);
            reject(error);
        });
    });
}

function showAnalyzerInfo() {
    const panel = vscode.window.createWebviewPanel(
        'exxerRulesInfo',
        'ExxerRules - Analyzer Information',
        vscode.ViewColumn.One,
        {
            enableScripts: true
        }
    );

    panel.webview.html = getAnalyzerInfoHtml();
}

function getAnalyzerInfoHtml(): string {
    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ExxerRules - Modern Development Conventions</title>
    <style>
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
            padding: 20px; 
            line-height: 1.6;
            color: var(--vscode-editor-foreground);
            background-color: var(--vscode-editor-background);
        }
        .category { 
            margin: 20px 0; 
            padding: 15px; 
            border-left: 4px solid #007ACC; 
            background-color: var(--vscode-editorWidget-background);
        }
        .category h3 { 
            margin-top: 0; 
            color: #007ACC; 
        }
        .rule { 
            margin: 10px 0; 
            padding: 10px; 
            background-color: var(--vscode-editor-background);
            border-radius: 4px;
        }
        .rule-id { 
            font-weight: bold; 
            color: #FFA500; 
        }
        .header {
            text-align: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 2px solid #007ACC;
        }
        .stats {
            display: flex;
            justify-content: space-around;
            margin: 20px 0;
            padding: 15px;
            background-color: var(--vscode-editorWidget-background);
            border-radius: 8px;
        }
        .stat {
            text-align: center;
        }
        .stat-number {
            font-size: 2em;
            font-weight: bold;
            color: #007ACC;
        }
    </style>
</head>
<body>
    <div class="header">
        <h1>🎯 ExxerRules - Modern Development Conventions</h1>
        <p>Comprehensive Roslyn analyzer suite with 20 production-ready analyzers</p>
        
        <div class="stats">
            <div class="stat">
                <div class="stat-number">20</div>
                <div>Analyzers</div>
            </div>
            <div class="stat">
                <div class="stat-number">7</div>
                <div>Categories</div>
            </div>
            <div class="stat">
                <div class="stat-number">51/51</div>
                <div>Tests Passing</div>
            </div>
        </div>
    </div>

    <div class="category">
        <h3>🏗️ Clean Architecture</h3>
        <div class="rule">
            <span class="rule-id">EXXER001:</span> Domain layer should not reference Infrastructure
        </div>
        <div class="rule">
            <span class="rule-id">EXXER002:</span> Use Repository Pattern for data access
        </div>
    </div>

    <div class="category">
        <h3>⚡ Async & Performance</h3>
        <div class="rule">
            <span class="rule-id">EXXER003:</span> Use ConfigureAwait(false) in libraries
        </div>
        <div class="rule">
            <span class="rule-id">EXXER004:</span> Async methods should accept CancellationToken
        </div>
        <div class="rule">
            <span class="rule-id">EXXER005:</span> Use efficient LINQ operations
        </div>
    </div>

    <div class="category">
        <h3>🎯 Error Handling & Result Pattern</h3>
        <div class="rule">
            <span class="rule-id">EXXER006:</span> Use Result&lt;T&gt; pattern instead of exceptions
        </div>
        <div class="rule">
            <span class="rule-id">EXXER007:</span> Avoid throwing exceptions in business logic
        </div>
    </div>

    <div class="category">
        <h3>🧪 Modern Testing Standards</h3>
        <div class="rule">
            <span class="rule-id">EXXER008:</span> Use XUnit v3 for testing
        </div>
        <div class="rule">
            <span class="rule-id">EXXER009:</span> Use Shouldly for assertions (avoid FluentAssertions)
        </div>
        <div class="rule">
            <span class="rule-id">EXXER010:</span> Use NSubstitute for mocking (avoid Moq)
        </div>
        <div class="rule">
            <span class="rule-id">EXXER011:</span> Follow proper test naming conventions
        </div>
        <div class="rule">
            <span class="rule-id">EXXER012:</span> Do not mock DbContext directly
        </div>
    </div>

    <div class="category">
        <h3>🚀 Modern C# Patterns</h3>
        <div class="rule">
            <span class="rule-id">EXXER013:</span> Use expression-bodied members where appropriate
        </div>
        <div class="rule">
            <span class="rule-id">EXXER014:</span> Use modern pattern matching
        </div>
    </div>

    <div class="category">
        <h3>📝 Code Quality & Documentation</h3>
        <div class="rule">
            <span class="rule-id">EXXER015:</span> Public members should have XML documentation
        </div>
        <div class="rule">
            <span class="rule-id">EXXER016:</span> Avoid magic numbers and strings
        </div>
        <div class="rule">
            <span class="rule-id">EXXER017:</span> Do not use #regions
        </div>
        <div class="rule">
            <span class="rule-id">EXXER018:</span> Validate null parameters
        </div>
    </div>

    <div class="category">
        <h3>📋 Logging & Formatting</h3>
        <div class="rule">
            <span class="rule-id">EXXER019:</span> Use structured logging instead of Console.WriteLine
        </div>
        <div class="rule">
            <span class="rule-id">EXXER020:</span> Follow consistent code formatting standards
        </div>
    </div>

    <div style="margin-top: 30px; padding: 20px; background-color: var(--vscode-editorWidget-background); border-radius: 8px;">
        <h3>🎉 Built with Modern Development Conventions</h3>
        <ul>
            <li>✅ Test-Driven Development (TDD)</li>
            <li>✅ Clean Architecture principles</li>
            <li>✅ Result&lt;T&gt; pattern for error handling</li>
            <li>✅ Modern C# 12 features</li>
            <li>✅ Comprehensive test coverage</li>
            <li>✅ Open source (MIT License)</li>
        </ul>
    </div>
</body>
</html>`;
}

function handleConfigurationChange() {
    const config = vscode.workspace.getConfiguration('exxerRules');
    const enabled = config.get<boolean>('enabled', true);
    
    if (enabled) {
        vscode.window.showInformationMessage('ExxerRules analyzers enabled');
    } else {
        vscode.window.showInformationMessage('ExxerRules analyzers disabled');
    }
}

function showWelcomeMessage(context: vscode.ExtensionContext) {
    const key = 'exxerRules.welcomeShown';
    const welcomeShown = context.globalState.get<boolean>(key, false);
    
    if (!welcomeShown) {
        vscode.window.showInformationMessage(
            'Welcome to ExxerRules! 🎯 Your C# code will now be analyzed with 20 modern development convention rules.',
            'Show Analyzer Info',
            'Open Settings'
        ).then(selection => {
            if (selection === 'Show Analyzer Info') {
                vscode.commands.executeCommand('exxerRules.showAnalyzerInfo');
            } else if (selection === 'Open Settings') {
                vscode.commands.executeCommand('workbench.action.openSettings', 'exxerRules');
            }
        });
        
        context.globalState.update(key, true);
    }
}

export function deactivate() {
    console.log('ExxerRules extension is now deactivated');
}
