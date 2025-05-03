# Test environment variables for CmdShiftLearn.Api
# This script sets environment variables for local testing

# Authentication settings
$env:Authentication__Google__ClientId = "test-google-client-id"
$env:Authentication__Google__ClientSecret = "test-google-client-secret"
$env:Authentication__GitHub__ClientId = "test-github-client-id"
$env:Authentication__GitHub__ClientSecret = "test-github-client-secret"
$env:Authentication__Jwt__Secret = "test-jwt-secret-key-for-local-testing-only-do-not-use-in-production"
$env:Authentication__Jwt__Issuer = "https://localhost:7071"
$env:Authentication__Jwt__Audience = "cmdshiftlearn-api-test"
$env:Authentication__Jwt__ExpiryMinutes = "60"

# Supabase settings
$env:Supabase__Url = "test-supabase-url"
$env:Supabase__ApiKey = "test-supabase-api-key"
$env:Supabase__JwtSecret = "test-supabase-jwt-secret"

# GitHub settings
$env:GitHub__AccessToken = "test-github-access-token"

# OpenAI settings
$env:OpenAI__ApiKey = "test-openai-api-key"

# Print environment variables
Write-Host "Environment variables set for testing:"
Write-Host "Authentication:Google:ClientId = $($env:Authentication__Google__ClientId.Substring(0, 3))..."
Write-Host "Authentication:Google:ClientSecret = $($env:Authentication__Google__ClientSecret.Substring(0, 3))..."
Write-Host "Authentication:GitHub:ClientId = $($env:Authentication__GitHub__ClientId.Substring(0, 3))..."
Write-Host "Authentication:GitHub:ClientSecret = $($env:Authentication__GitHub__ClientSecret.Substring(0, 3))..."
Write-Host "Authentication:Jwt:Secret = $($env:Authentication__Jwt__Secret.Substring(0, 3))..."
Write-Host "Authentication:Jwt:Issuer = $($env:Authentication__Jwt__Issuer)"
Write-Host "Authentication:Jwt:Audience = $($env:Authentication__Jwt__Audience)"
Write-Host "Authentication:Jwt:ExpiryMinutes = $($env:Authentication__Jwt__ExpiryMinutes)"
Write-Host "Supabase:Url = $($env:Supabase__Url.Substring(0, 3))..."
Write-Host "Supabase:ApiKey = $($env:Supabase__ApiKey.Substring(0, 3))..."
Write-Host "Supabase:JwtSecret = $($env:Supabase__JwtSecret.Substring(0, 3))..."
Write-Host "GitHub:AccessToken = $($env:GitHub__AccessToken.Substring(0, 3))..."
Write-Host "OpenAI:ApiKey = $($env:OpenAI__ApiKey.Substring(0, 3))..."

Write-Host "`nNow run the API with these environment variables:"
Write-Host "cd CmdShiftLearn.Api"
Write-Host "dotnet run"
Write-Host "`nThen navigate to https://localhost:7071/api/debug/config to verify the configuration"