[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$Command,

    [Parameter(Position = 1, ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ProjectPath {
    return (Resolve-Path "$PSScriptRoot/..").Path
}

function Get-ExpectedUnityVersion {
    $projectPath = Get-ProjectPath
    $versionFile = Join-Path $projectPath "ProjectSettings/ProjectVersion.txt"
    if (-not (Test-Path $versionFile)) {
        Write-Error "ProjectVersion.txt not found at $versionFile"
        exit 1
    }
    $content = Get-Content $versionFile -Raw
    if ($content -match 'm_EditorVersion:\s*([^\r\n]+)') {
        return $matches[1].Trim()
    }
    Write-Error "Could not parse m_EditorVersion from $versionFile"
    exit 1
}

function Resolve-UnityEditor {
    if ($env:UNITY_EDITOR -and (Test-Path $env:UNITY_EDITOR)) {
        return $env:UNITY_EDITOR
    }

    $version = Get-ExpectedUnityVersion
    $candidatePaths = @(
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "C:\Program Files\Unity\$version\Editor\Unity.exe",
        "D:\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "D:\Unity\$version\Editor\Unity.exe"
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    Write-Error @"
ERROR: Unity editor executable not found for version $version.
Looked in:
$($candidatePaths -join "`n")
Please set the UNITY_EDITOR environment variable to the exact Unity.exe path.
"@
    exit 1
}

function Ensure-ArtifactsDirectory {
    $projectPath = Get-ProjectPath
    $artifactsPath = Join-Path $projectPath ".artifacts"
    if (-not (Test-Path $artifactsPath)) {
        New-Item -ItemType Directory -Force -Path $artifactsPath | Out-Null
    }
    return $artifactsPath
}

function Invoke-UnityBatchmode {
    param(
        [string[]]$Arguments
    )
    $editor = Resolve-UnityEditor
    $projectPath = Get-ProjectPath

    $fullArgs = @("-batchmode", "-nographics", "-projectPath", $projectPath) + $Arguments

    $proc = Start-Process -FilePath $editor -ArgumentList $fullArgs -Wait -PassThru -NoNewWindow
    return $proc.ExitCode
}

function Run-Setup {
    $projectPath = Get-ProjectPath
    $editor = Resolve-UnityEditor
    $editorDir = Split-Path $editor -Parent
    $yamlMerge = Join-Path $editorDir "Data\Tools\UnityYAMLMerge.exe"

    Write-Host "Configuring Git LFS..."
    git lfs install --local
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to initialize Git LFS."
        exit $LASTEXITCODE
    }

    if (Test-Path $yamlMerge) {
        Write-Host "Configuring UnityYAMLMerge driver ($yamlMerge)..."
        $driverCommand = "'$($yamlMerge -replace '\\', '/')' merge -p %O %B %A %A"
        git config merge.unityyamlmerge.driver $driverCommand
        Write-Host "UnityYAMLMerge configured successfully."
    } else {
        Write-Warning "UnityYAMLMerge.exe not found at $yamlMerge. Skipping merge driver configuration."
    }
}

function Run-Compile {
    $artifactsDir = Ensure-ArtifactsDirectory
    $logPath = Join-Path $artifactsDir "compile.log"

    if (Test-Path $logPath) { Remove-Item $logPath -Force }

    Write-Host "[tools/unity compile] Compiling project scripts..."
    $exitCode = Invoke-UnityBatchmode @(
        "-quit",
        "-buildTarget", "Android",
        "-executeMethod", "Game.Editor.Cli.Compile",
        "-logFile", $logPath
    )

    $logContent = if (Test-Path $logPath) { Get-Content $logPath } else { @() }

    $errorLines = @()
    foreach ($line in $logContent) {
        if ($line -match ':\s*error\s*CS\d+:' -or $line -match 'Scripts have compiler errors') {
            $errorLines += $line
        }
    }

    $compileSuccess = $false
    foreach ($line in $logContent) {
        if ($line -match '\[Game\.Editor\.Cli\] Compile succeeded\.') {
            $compileSuccess = $true
            break
        }
    }

    if ($exitCode -ne 0 -or $errorLines.Count -gt 0 -or -not $compileSuccess) {
        Write-Host "`n[tools/unity compile] FAILED: Compilation errors detected." -ForegroundColor Red
        if ($errorLines.Count -gt 0) {
            foreach ($err in $errorLines) {
                Write-Host "  $err" -ForegroundColor Red
            }
        } elseif (-not $compileSuccess) {
            Write-Host "  Compilation did not finish successfully (exit code: $exitCode)." -ForegroundColor Red
        }
        exit 1
    }

    Write-Host "[tools/unity compile] SUCCESS: 0 errors." -ForegroundColor Green
    exit 0
}

function Run-Tests {
    param(
        [string]$Platform
    )
    $artifactsDir = Ensure-ArtifactsDirectory
    $resultsFile = Join-Path $artifactsDir "$($Platform.ToLower()).xml"
    $logFile = Join-Path $artifactsDir "test-$($Platform.ToLower()).log"

    if (Test-Path $resultsFile) { Remove-Item $resultsFile -Force }
    if (Test-Path $logFile) { Remove-Item $logFile -Force }

    Write-Host "[tools/unity test-$($Platform.ToLower())] Running $Platform tests..."
    $exitCode = Invoke-UnityBatchmode @(
        "-runTests",
        "-testPlatform", $Platform,
        "-testResults", $resultsFile,
        "-logFile", $logFile
    )

    if (-not (Test-Path $resultsFile)) {
        Write-Host "`n[tools/unity test-$($Platform.ToLower())] FAILED: No test results file generated (exit code: $exitCode)." -ForegroundColor Red
        exit 1
    }

    [xml]$xml = Get-Content $resultsFile
    $testRun = $xml.'test-run'
    $total = [int]$testRun.total
    $passed = [int]$testRun.passed
    $failed = [int]$testRun.failed
    $inconclusive = [int]$testRun.inconclusive
    $duration = $testRun.duration

    if ($failed -gt 0 -or $exitCode -ne 0) {
        Write-Host "`n[tools/unity test-$($Platform.ToLower())] FAILED: $failed/$total tests failed." -ForegroundColor Red
        $failedNodes = $xml.SelectNodes("//test-case[@result='Failed']")
        foreach ($node in $failedNodes) {
            Write-Host "  Failed test: $($node.fullname)" -ForegroundColor Red
            if ($node.failure.message) {
                Write-Host "    Message: $($node.failure.message.InnerText.Trim())" -ForegroundColor Red
            }
        }
        exit 1
    }

    Write-Host "[tools/unity test-$($Platform.ToLower())] SUCCESS: $passed/$total passed (duration: ${duration}s)." -ForegroundColor Green
    exit 0
}

switch ($Command.ToLower()) {
    "setup" {
        Run-Setup
    }
    "compile" {
        Run-Compile
    }
    "test-edit" {
        Run-Tests -Platform "EditMode"
    }
    "test-play" {
        Run-Tests -Platform "PlayMode"
    }
    "import-content" {
        $artifactsDir = Ensure-ArtifactsDirectory
        $logPath = Join-Path $artifactsDir "import-content.log"
        $code = Invoke-UnityBatchmode @("-quit", "-executeMethod", "Game.Editor.Cli.ImportContent", "-logFile", $logPath)
        exit $code
    }
    "validate" {
        $artifactsDir = Ensure-ArtifactsDirectory
        $logPath = Join-Path $artifactsDir "validate.log"
        $code = Invoke-UnityBatchmode @("-quit", "-executeMethod", "Game.Editor.Cli.ValidateContent", "-logFile", $logPath)
        exit $code
    }
    "simulate" {
        $artifactsDir = Ensure-ArtifactsDirectory
        $logPath = Join-Path $artifactsDir "simulate.log"
        $code = Invoke-UnityBatchmode @("-quit", "-executeMethod", "Game.Editor.Cli.SimulateLevel", "-logFile", $logPath)
        exit $code
    }
    "build-android" {
        $artifactsDir = Ensure-ArtifactsDirectory
        $logPath = Join-Path $artifactsDir "build-android.log"
        $code = Invoke-UnityBatchmode @("-quit", "-executeMethod", "Game.Editor.Cli.BuildAndroid", "-logFile", $logPath)
        exit $code
    }
    default {
        Write-Error "Unknown command: $Command. Available: setup, compile, test-edit, test-play, import-content, validate, simulate, build-android"
        exit 1
    }
}
