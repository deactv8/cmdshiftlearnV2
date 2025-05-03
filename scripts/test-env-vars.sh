#!/bin/bash
# Test environment variables for CmdShiftLearn.Api
# This script sets environment variables for local testing

# Authentication settings
export Authentication__Google__ClientId="test-google-client-id"
export Authentication__Google__ClientSecret="test-google-client-secret"
export Authentication__GitHub__ClientId="test-github-client-id"
export Authentication__GitHub__ClientSecret="test-github-client-secret"
export Authentication__Jwt__Secret="test-jwt-secret-key-for-local-testing-only-do-not-use-in-production"
export Authentication__Jwt__Issuer="https://localhost:7071"
export Authentication__Jwt__Audience="cmdshiftlearn-api-test"
export Authentication__Jwt__ExpiryMinutes="60"

# Supabase settings
export Supabase__Url="test-supabase-url"
export Supabase__ApiKey="test-supabase-api-key"
export Supabase__JwtSecret="test-supabase-jwt-secret"

# GitHub settings
export GitHub__AccessToken="test-github-access-token"

# OpenAI settings
export OpenAI__ApiKey="test-openai-api-key"

# Print environment variables
echo "Environment variables set for testing:"
echo "Authentication:Google:ClientId = ${Authentication__Google__ClientId:0:3}..."
echo "Authentication:Google:ClientSecret = ${Authentication__Google__ClientSecret:0:3}..."
echo "Authentication:GitHub:ClientId = ${Authentication__GitHub__ClientId:0:3}..."
echo "Authentication:GitHub:ClientSecret = ${Authentication__GitHub__ClientSecret:0:3}..."
echo "Authentication:Jwt:Secret = ${Authentication__Jwt__Secret:0:3}..."
echo "Authentication:Jwt:Issuer = $Authentication__Jwt__Issuer"
echo "Authentication:Jwt:Audience = $Authentication__Jwt__Audience"
echo "Authentication:Jwt:ExpiryMinutes = $Authentication__Jwt__ExpiryMinutes"
echo "Supabase:Url = ${Supabase__Url:0:3}..."
echo "Supabase:ApiKey = ${Supabase__ApiKey:0:3}..."
echo "Supabase:JwtSecret = ${Supabase__JwtSecret:0:3}..."
echo "GitHub:AccessToken = ${GitHub__AccessToken:0:3}..."
echo "OpenAI:ApiKey = ${OpenAI__ApiKey:0:3}..."

echo -e "\nNow run the API with these environment variables:"
echo "cd CmdShiftLearn.Api"
echo "dotnet run"
echo -e "\nThen navigate to https://localhost:7071/api/debug/config to verify the configuration"