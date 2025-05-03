#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Clean build script for CmdShiftLearn project
.DESCRIPTION
    This script cleans bin/obj folders, restores dependencies, and builds the solution
.NOTES
    Run with ./build.ps1
#>

# Set strict mode to catch common errors
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Define projects to clean
$projectPaths = @(
    ".",                # CmdShiftLearn.Api (root project)
    "./CmdAgent"        # CmdAgent project
)

Write-Host "🧹 Cleaning bin/obj folders..." -ForegroundColor Cyan

foreach ($projectPath in $projectPaths) {
    $binPath = Join-Path -Path $projectPath -ChildPath "bin"
    $objPath = Join-Path -Path $projectPath -ChildPath "obj"
    
    if (Test-Path $binPath) {
        Write-Host "  Removing $binPath" -ForegroundColor Gray
        Remove-Item -Path $binPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    if (Test-Path $objPath) {
        Write-Host "  Removing $objPath" -ForegroundColor Gray
        Remove-Item -Path $objPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "📦 Restoring dependencies..." -ForegroundColor Cyan
dotnet restore
if (-not $?) {
    Write-Host "❌ Dependency restoration failed!" -ForegroundColor Red
    exit 1
}

# Check if dotnet-format is installed, if not install it
Write-Host "🔍 Checking for dotnet-format tool..." -ForegroundColor Cyan
$formatInstalled = dotnet tool list --global | Select-String -Pattern "dotnet-format" -Quiet
if (-not $formatInstalled) {
    Write-Host "  Installing dotnet-format tool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-format
    if (-not $?) {
        Write-Host "⚠️ Could not install dotnet-format, continuing without formatting" -ForegroundColor Yellow
    }
}

# Run dotnet format to fix code style and project structure issues
Write-Host "✨ Formatting code and fixing project structure..." -ForegroundColor Cyan
dotnet format --verbosity normal
if (-not $?) {
    Write-Host "⚠️ Formatting encountered issues, continuing with build" -ForegroundColor Yellow
} else {
    Write-Host "  Formatting complete" -ForegroundColor Green
}

Write-Host "🔨 Building solution..." -ForegroundColor Cyan
dotnet build --no-restore
if (-not $?) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build completed successfully!" -ForegroundColor Green