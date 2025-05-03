# Verify static files are included in the published output

# Clean and publish the project
Write-Host "Cleaning and publishing the project..." -ForegroundColor Cyan
dotnet clean CmdShiftLearn.Api/CmdShiftLearn.Api.csproj
dotnet publish CmdShiftLearn.Api/CmdShiftLearn.Api.csproj -c Release -o publish

# Check if the wwwroot directory exists in the published output
Write-Host "Checking if the wwwroot directory exists in the published output..." -ForegroundColor Cyan
$wwwrootPath = "publish/wwwroot"
if (Test-Path $wwwrootPath) {
    Write-Host "✅ wwwroot directory exists in the published output" -ForegroundColor Green
    
    # List the files in the wwwroot directory
    Write-Host "Files in the wwwroot directory:" -ForegroundColor Cyan
    Get-ChildItem -Path $wwwrootPath -Recurse -File | ForEach-Object {
        Write-Host "  $($_.FullName.Replace((Get-Location).Path + '\', ''))" -ForegroundColor Gray
    }
    
    # Check if auth-related HTML files exist
    $authFiles = @("auth-test.html", "auth-success.html", "auth-error.html")
    foreach ($file in $authFiles) {
        $filePath = "$wwwrootPath/$file"
        if (Test-Path $filePath) {
            Write-Host "✅ $file exists in the published output" -ForegroundColor Green
        } else {
            Write-Host "❌ $file does not exist in the published output" -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ wwwroot directory does not exist in the published output" -ForegroundColor Red
}

# Check if the web.config file exists in the published output
$webConfigPath = "publish/web.config"
if (Test-Path $webConfigPath) {
    Write-Host "✅ web.config exists in the published output" -ForegroundColor Green
    
    # Check if the web.config file contains the staticContent section
    $webConfig = Get-Content $webConfigPath -Raw
    if ($webConfig -match "<staticContent>") {
        Write-Host "✅ web.config contains the staticContent section" -ForegroundColor Green
    } else {
        Write-Host "❌ web.config does not contain the staticContent section" -ForegroundColor Red
    }
} else {
    Write-Host "❌ web.config does not exist in the published output" -ForegroundColor Red
}

Write-Host "Done!" -ForegroundColor Cyan