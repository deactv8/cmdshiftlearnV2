using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using CmdShiftLearn.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"Bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
    
    // Enable XML comments for Swagger documentation
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Load Supabase settings
var supabaseSettings = builder.Configuration.GetSection("Supabase").Get<SupabaseSettings>();
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));

// ✅ Add secure JWT authentication using Supabase token validation
// Try to get JWT secret from multiple sources with fallback logic
string jwtSecret;
string jwtSecretSource;

// First try to get from configuration
var configSecret = builder.Configuration["Supabase:JwtSecret"];
if (!string.IsNullOrWhiteSpace(configSecret))
{
    jwtSecret = configSecret;
    jwtSecretSource = "appsettings configuration";
    Console.WriteLine("✅ JWT Secret loaded from appsettings configuration");
}
else
{
    // Fall back to environment variable
    var envSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
    if (!string.IsNullOrWhiteSpace(envSecret))
    {
        jwtSecret = envSecret;
        jwtSecretSource = "SUPABASE_JWT_SECRET environment variable";
        Console.WriteLine("✅ JWT Secret loaded from SUPABASE_JWT_SECRET environment variable");
    }
    else
    {
        // No valid secret found
        jwtSecret = string.Empty;
        jwtSecretSource = "none - SECRET NOT FOUND";
        Console.WriteLine("⚠️ WARNING: JWT Secret not found in configuration or environment variables!");
        Console.WriteLine("Authentication will fail until a valid JWT secret is provided.");
        Console.WriteLine("Please set the Supabase:JwtSecret in appsettings.json or the SUPABASE_JWT_SECRET environment variable.");
    }
}

// Log information about the secret (masked for security)
if (!string.IsNullOrEmpty(jwtSecret))
{
    Console.WriteLine($"JWT Secret source: {jwtSecretSource}");
    Console.WriteLine($"JWT Secret preview: {jwtSecret[..Math.Min(3, jwtSecret.Length)]}...{(jwtSecret.Length > 3 ? jwtSecret[^Math.Min(3, jwtSecret.Length)..] : "")}");
    Console.WriteLine($"JWT Secret length: {jwtSecret.Length} characters");
}
else
{
    Console.WriteLine("JWT Secret: EMPTY");
}

// Check if the secret is Base64 encoded (Supabase JWT secrets are typically Base64 encoded)
// This is just for informational purposes - we'll use the raw string regardless
bool isBase64 = false;
try {
    var decodedBytes = Convert.FromBase64String(jwtSecret);
    isBase64 = true;
    Console.WriteLine($"JWT Secret is valid Base64, decoded length: {decodedBytes.Length} bytes");
    Console.WriteLine("Note: We'll use the raw Base64 string as-is for JWT validation");
} catch {
    if (!string.IsNullOrEmpty(jwtSecret))
    {
        Console.WriteLine("⚠️ WARNING: JWT Secret is NOT valid Base64 - this might be a problem as Supabase typically uses Base64 encoded secrets");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        if (string.IsNullOrEmpty(jwtSecret))
        {
            Console.WriteLine("⚠️ WARNING: No JWT secret available for token validation");
            Console.WriteLine("Authentication will fail for all requests requiring authorization");
            
            // Even with no secret, we need to provide something to avoid startup errors
            // This will cause all tokens to fail validation, which is the desired behavior
            // when no secret is configured
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("invalid-key")),
                ValidateIssuer = true,
                ValidIssuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }
        else
        {
            // For Supabase JWT validation, we should use the raw JWT secret string
            // The secret is already Base64 encoded and should be used as-is
            byte[] keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
            Console.WriteLine($"Using JWT secret from {jwtSecretSource} (as UTF-8 bytes without decoding Base64 first)");
        
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.Zero,
                // Explicitly set the algorithm to HS256 which is what Supabase uses
                ValidAlgorithms = new[] { "HS256" },
                // ✅ Map the "sub" claim to the user's identity
                NameClaimType = "sub"
            };
        }
        
        // Add detailed event logging for token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                Console.WriteLine("[JWT] Token validated successfully!");
                Console.WriteLine($"[JWT] User: {context.Principal?.Identity?.Name}");
                Console.WriteLine($"[JWT] Claims: {string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] Authentication failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"[JWT] Inner exception: {context.Exception.InnerException.GetType().Name}: {context.Exception.InnerException.Message}");
                }
                
                // If it's a SecurityTokenSignatureKeyNotFoundException, it means the key is wrong
                if (context.Exception is Microsoft.IdentityModel.Tokens.SecurityTokenSignatureKeyNotFoundException)
                {
                    Console.WriteLine("[JWT] This suggests the JWT secret doesn't match what Supabase is using to sign tokens.");
                    Console.WriteLine("[JWT] Please check that the Supabase:JwtSecret in appsettings.json is correct.");
                }
                
                // If it's a SecurityTokenExpiredException, the token has expired
                if (context.Exception is Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException)
                {
                    Console.WriteLine("[JWT] The token has expired. Please get a new token.");
                }
                
                // If it's a SecurityTokenInvalidSignatureException, the signature is invalid
                if (context.Exception is Microsoft.IdentityModel.Tokens.SecurityTokenInvalidSignatureException)
                {
                    Console.WriteLine("[JWT] The token signature is invalid. This could be due to an incorrect JWT secret.");
                }
                
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"[JWT] OnChallenge: {context.Error}, {context.ErrorDescription}");
                
                // Add a custom response for unauthorized requests
                if (context.AuthenticateFailure != null)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var errorMessage = "Authentication failed. ";
                    
                    if (context.AuthenticateFailure is Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException)
                    {
                        errorMessage += "Your token has expired. Please log in again.";
                    }
                    else if (context.AuthenticateFailure is Microsoft.IdentityModel.Tokens.SecurityTokenInvalidSignatureException)
                    {
                        errorMessage += "Invalid token signature. Please log in again.";
                    }
                    else if (context.AuthenticateFailure is Microsoft.IdentityModel.Tokens.SecurityTokenSignatureKeyNotFoundException)
                    {
                        errorMessage += "Token signature key not found. Please contact support.";
                    }
                    else
                    {
                        errorMessage += "Please log in again.";
                    }
                    
                    var result = System.Text.Json.JsonSerializer.Serialize(new { message = errorMessage });
                    context.Response.WriteAsync(result);
                }
                
                context.HandleResponse(); // Suppress the default response
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrEmpty(context.Token))
                {
                    Console.WriteLine($"[JWT] Token received: {context.Token.Substring(0, Math.Min(10, context.Token.Length))}...");
                    
                    try
                    {
                        // Try to decode the token without validation to see its structure
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadJwtToken(context.Token);
                        
                        Console.WriteLine($"[JWT] Token issuer: {jsonToken.Issuer}");
                        Console.WriteLine($"[JWT] Token algorithm: {jsonToken.Header.Alg}");
                        Console.WriteLine($"[JWT] Token issued at: {jsonToken.IssuedAt}");
                        Console.WriteLine($"[JWT] Token expires at: {jsonToken.ValidTo}");
                        
                        // Check if the token is expired
                        if (jsonToken.ValidTo < DateTime.UtcNow)
                        {
                            Console.WriteLine("[JWT] WARNING: Token is expired!");
                        }
                        
                        // Check if the issuer matches our expected issuer
                        if (jsonToken.Issuer != "https://fqceiphubiqnorytayiu.supabase.co/auth/v1")
                        {
                            Console.WriteLine($"[JWT] WARNING: Token issuer '{jsonToken.Issuer}' doesn't match expected issuer 'https://fqceiphubiqnorytayiu.supabase.co/auth/v1'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[JWT] Error decoding token: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("[JWT] No token received in request");
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
builder.Services.AddSingleton<IEventLogger, EventLoggerService>();

// Register tutorial services based on configuration
var tutorialSource = builder.Configuration["ContentSources:Tutorials:Source"] ?? "File";
switch (tutorialSource.ToLower())
{
    case "github":
        builder.Services.AddSingleton<ITutorialLoader, GitHubTutorialLoader>();
        builder.Services.AddSingleton<ITutorialService, TutorialService>();
        Console.WriteLine("Using GitHub tutorial loader");
        break;
    case "supabase":
        builder.Services.AddSingleton<ITutorialLoader, SupabaseTutorialLoader>();
        builder.Services.AddSingleton<ITutorialService, TutorialService>();
        Console.WriteLine("Using Supabase tutorial loader");
        break;
    case "file":
    default:
        builder.Services.AddSingleton<ITutorialLoader, FileTutorialLoader>();
        builder.Services.AddSingleton<ITutorialService, TutorialService>();
        Console.WriteLine("Using File tutorial loader");
        break;
}

// Configure OpenAI settings with secure API key loading
var openAISettings = new OpenAISettings();
builder.Configuration.GetSection("OpenAI").Bind(openAISettings);

// Try to get OpenAI API key from environment variable first
string openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
string apiKeySource = "environment variable";

// If not found in environment, try user secrets (for development)
if (string.IsNullOrEmpty(openAIApiKey) && builder.Environment.IsDevelopment())
{
    openAIApiKey = builder.Configuration["OpenAI:ApiKey"] ?? "";
    apiKeySource = "user secrets or appsettings";
}

// Set the API key in the settings
openAISettings.ApiKey = openAIApiKey;

// Log the API key source (but not the key itself)
if (!string.IsNullOrEmpty(openAIApiKey))
{
    Console.WriteLine($"✅ OpenAI API key loaded from {apiKeySource}");
    // Log a masked version of the key for debugging
    var maskedKey = openAIApiKey.Length > 8 
        ? $"{openAIApiKey[..4]}...{openAIApiKey[^4..]}" 
        : "[too short to display safely]";
    Console.WriteLine($"API Key preview: {maskedKey}");
}
else
{
    Console.WriteLine("⚠️ WARNING: OpenAI API key not found in environment variables or configuration!");
    Console.WriteLine("Shello AI assistant will not function correctly until a valid API key is provided.");
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable or add it to user secrets.");
}

// Register OpenAI settings
builder.Services.Configure<OpenAISettings>(options => 
{
    options.ApiKey = openAISettings.ApiKey;
    options.Model = openAISettings.Model;
    options.Temperature = openAISettings.Temperature;
    options.MaxRetries = openAISettings.MaxRetries;
    options.RetryDelayMs = openAISettings.RetryDelayMs;
});

// Register Shello service for AI-powered responses
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IShelloService, ShelloService>();
Console.WriteLine($"Registered Shello AI assistant service with model: {openAISettings.Model}, temperature: {openAISettings.Temperature}");

// Register challenge services based on configuration
var challengeSource = builder.Configuration["ContentSources:Challenges:Source"] ?? "File";
switch (challengeSource.ToLower())
{
    case "github":
        builder.Services.AddSingleton<IChallengeLoader, GitHubChallengeLoader>();
        Console.WriteLine("Using GitHub challenge loader");
        break;
    case "supabase":
        builder.Services.AddSingleton<IChallengeLoader, SupabaseChallengeLoader>();
        Console.WriteLine("Using Supabase challenge loader");
        break;
    case "file":
    default:
        builder.Services.AddSingleton<IChallengeLoader, FileChallengeLoader>();
        Console.WriteLine("Using File challenge loader");
        break;
}
builder.Services.AddSingleton<ChallengeService>();

// Configure static files for serving the debug HTML
builder.Services.AddDirectoryBrowser();

var app = builder.Build();
app.MapGet("/", () => "CmdShiftLearn API is working!");

// Enable static files for the debug HTML
app.UseStaticFiles();

// Swagger and dev tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Add diagnostic endpoint for JWT configuration (development only)
    app.MapGet("/debug/jwt-config", () => {
        // Check configuration source
        var configSecret = builder.Configuration["Supabase:JwtSecret"];
        var envSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
        
        var configSecretAvailable = !string.IsNullOrWhiteSpace(configSecret);
        var envSecretAvailable = !string.IsNullOrWhiteSpace(envSecret);
        
        // Determine which secret is being used (following the same logic as in startup)
        string secretSource;
        string secret;
        
        if (configSecretAvailable)
        {
            secretSource = "appsettings configuration";
            secret = configSecret!;
        }
        else if (envSecretAvailable)
        {
            secretSource = "SUPABASE_JWT_SECRET environment variable";
            secret = envSecret!;
        }
        else
        {
            secretSource = "none - SECRET NOT FOUND";
            secret = string.Empty;
        }
        
        // Check if the secret is Base64 encoded
        bool isValidBase64 = false;
        int decodedLength = 0;
        try {
            var bytes = Convert.FromBase64String(secret);
            isValidBase64 = true;
            decodedLength = bytes.Length;
        } catch {
            // Not valid Base64
        }
        
        return new {
            SecretSource = secretSource,
            ConfigHasSecret = configSecretAvailable,
            EnvVarHasSecret = envSecretAvailable,
            SecretFirstChars = string.IsNullOrEmpty(secret) ? "EMPTY" : secret.Length > 6 ? $"{secret[..3]}..." : "TOO SHORT",
            SecretLastChars = string.IsNullOrEmpty(secret) ? "EMPTY" : secret.Length > 6 ? $"...{secret[^3..]}" : "TOO SHORT",
            SecretLength = secret.Length,
            HasValidSecret = !string.IsNullOrEmpty(secret) && secret.Length > 32,
            IsValidBase64 = isValidBase64,
            DecodedLength = decodedLength,
            Issuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = "TimeSpan.Zero"
        };
    });
    
    // Add endpoint to check request headers
    app.MapGet("/debug/headers", (HttpContext context) => {
        var headers = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            var value = header.Key.ToLower() == "authorization" 
                ? $"{header.Value.ToString()[0..Math.Min(15, header.Value.ToString().Length)]}..." 
                : header.Value.ToString();
            headers[header.Key] = value;
        }
        return headers;
    });
    
    // Add endpoint to check GitHub configuration
    app.MapGet("/debug/github-config", () => {
        return new {
            Owner = builder.Configuration["GitHub:Owner"] ?? "Not configured",
            Repo = builder.Configuration["GitHub:Repo"] ?? "Not configured",
            Branch = builder.Configuration["GitHub:Branch"] ?? "Not configured",
            TutorialsPath = builder.Configuration["GitHub:TutorialsPath"] ?? "Not configured",
            HasAccessToken = !string.IsNullOrEmpty(builder.Configuration["GitHub:AccessToken"]),
            ContentSource = builder.Configuration["ContentSources:Tutorials:Source"] ?? "File"
        };
    });
    
    // Add endpoint to check JWT secret details
    app.MapGet("/debug/jwt-secret", () => {
        // Check all possible sources
        var configSecret = builder.Configuration["Supabase:JwtSecret"];
        var envSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
        
        var configSecretAvailable = !string.IsNullOrWhiteSpace(configSecret);
        var envSecretAvailable = !string.IsNullOrWhiteSpace(envSecret);
        
        // Determine which secret is being used (following the same logic as in startup)
        string secretSource;
        string secret;
        
        if (configSecretAvailable)
        {
            secretSource = "appsettings configuration";
            secret = configSecret!;
        }
        else if (envSecretAvailable)
        {
            secretSource = "SUPABASE_JWT_SECRET environment variable";
            secret = envSecret!;
        }
        else
        {
            secretSource = "none - SECRET NOT FOUND";
            secret = string.Empty;
        }
        
        // Check if the secret is Base64 encoded
        bool isValidBase64 = false;
        int decodedLength = 0;
        
        try {
            var bytes = Convert.FromBase64String(secret);
            isValidBase64 = true;
            decodedLength = bytes.Length;
        } catch {
            // Not valid Base64
        }
        
        return new {
            SecretSource = secretSource,
            ConfigHasSecret = configSecretAvailable,
            EnvVarHasSecret = envSecretAvailable,
            SecretPresent = !string.IsNullOrEmpty(secret),
            SecretLength = secret.Length,
            IsBase64Encoded = isValidBase64,
            DecodedLength = decodedLength,
            FirstFewChars = !string.IsNullOrEmpty(secret) && secret.Length > 3 ? secret.Substring(0, 3) + "..." : "N/A",
            LastFewChars = !string.IsNullOrEmpty(secret) && secret.Length > 3 ? "..." + secret.Substring(secret.Length - 3) : "N/A"
        };
    });
}

app.UseHttpsRedirection();

// Health check
app.MapGet("/health", () => new { status = "OK" });

// Add a test endpoint to manually validate tokens (development only)
if (app.Environment.IsDevelopment())
{
    // Add an endpoint to test authentication
    app.MapGet("/debug/auth-test", [Authorize] (HttpContext context) => {
        var userId = context.User.FindFirstValue("sub");
        var claims = context.User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        
        return new { 
            Message = "Authentication successful!",
            UserId = userId,
            Claims = claims,
            IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false
        };
    });
    
    app.MapPost("/debug/validate-token", async (HttpContext context) => {
        try {
            // Read the token from the request body
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var token = requestBody.Trim();
            
            if (string.IsNullOrEmpty(token)) {
                return Results.BadRequest("No token provided");
            }
            
            // Get the JWT secret using the same logic as in startup
            string secret;
            string secretSource;
            
            // First try configuration
            var configSecret = builder.Configuration["Supabase:JwtSecret"];
            if (!string.IsNullOrWhiteSpace(configSecret))
            {
                secret = configSecret;
                secretSource = "appsettings configuration";
            }
            else
            {
                // Fall back to environment variable
                var envSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
                if (!string.IsNullOrWhiteSpace(envSecret))
                {
                    secret = envSecret;
                    secretSource = "SUPABASE_JWT_SECRET environment variable";
                }
                else
                {
                    return Results.Problem("JWT secret not configured in either appsettings.json or SUPABASE_JWT_SECRET environment variable");
                }
            }
            
            // First, decode the token without validation to see its structure
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            
            try {
                var jsonToken = handler.ReadJwtToken(token);
                var tokenInfo = new {
                    Header = jsonToken.Header,
                    Issuer = jsonToken.Issuer,
                    Audience = jsonToken.Audiences,
                    ValidFrom = jsonToken.ValidFrom,
                    ValidTo = jsonToken.ValidTo,
                    Claims = jsonToken.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
                };
                
                // Check if the token is expired
                bool isExpired = jsonToken.ValidTo < DateTime.UtcNow;
                
                // Create token validation parameters
                // For Supabase, we should use the raw JWT secret string with UTF8 encoding
                byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
                bool isBase64 = false;
                
                try {
                    // Just check if it's valid Base64 for informational purposes
                    var _ = Convert.FromBase64String(secret);
                    isBase64 = true;
                } catch {
                    // Not valid Base64, but we still use the raw string
                }
                
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.Zero,
                    ValidAlgorithms = new[] { "HS256" }
                };
                
                try {
                    // Validate the token
                    var principal = handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                    
                    // Return the claims from the token
                    var claims = principal.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
                    return Results.Ok(new { 
                        IsValid = true, 
                        TokenInfo = tokenInfo,
                        IsExpired = isExpired,
                        SecretSource = secretSource,
                        SecretIsBase64Format = isBase64,
                        ValidationResult = new {
                            Claims = claims,
                            TokenType = validatedToken.GetType().Name,
                            Algorithm = (validatedToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken)?.Header?.Alg
                        }
                    });
                }
                catch (Exception ex) {
                    return Results.BadRequest(new { 
                        IsValid = false, 
                        TokenInfo = tokenInfo,
                        IsExpired = isExpired,
                        SecretSource = secretSource,
                        SecretIsBase64Format = isBase64,
                        ValidationError = new {
                            Error = ex.Message,
                            ErrorType = ex.GetType().Name,
                            InnerError = ex.InnerException?.Message,
                            InnerErrorType = ex.InnerException?.GetType().Name,
                            StackTrace = ex.StackTrace
                        }
                    });
                }
            }
            catch (Exception ex) {
                return Results.BadRequest(new { 
                    IsValid = false, 
                    Error = "Could not decode token",
                    ErrorDetails = ex.Message
                });
            }
        }
        catch (Exception ex) {
            return Results.Problem($"Error processing request: {ex.Message}");
        }
    });
    
    // Add an endpoint to test different JWT secret formats
    app.MapPost("/debug/test-jwt-formats", (string token) => {
        if (string.IsNullOrEmpty(token)) {
            return Results.BadRequest("No token provided");
        }
        
        // Get the JWT secret using the same logic as in startup
        string secret;
        string secretSource;
        
        // First try configuration
        var configSecret = builder.Configuration["Supabase:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(configSecret))
        {
            secret = configSecret;
            secretSource = "appsettings configuration";
        }
        else
        {
            // Fall back to environment variable
            var envSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
            if (!string.IsNullOrWhiteSpace(envSecret))
            {
                secret = envSecret;
                secretSource = "SUPABASE_JWT_SECRET environment variable";
            }
            else
            {
                return Results.Problem("JWT secret not configured in either appsettings.json or SUPABASE_JWT_SECRET environment variable");
            }
        }
        
        var results = new List<object>();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        
        // Test 1: Original secret as-is with UTF8 encoding (recommended for Supabase)
        results.Add(TestJwtValidation(handler, token, secret, "Original secret with UTF8 encoding", useUtf8: true));
        
        // Test 2: Original secret as-is with ASCII encoding (alternative)
        results.Add(TestJwtValidation(handler, token, secret, "Original secret with ASCII encoding", useUtf8: false));
        
        // Test 3: Base64 decoded secret (not recommended for Supabase)
        try {
            var decodedBytes = Convert.FromBase64String(secret);
            var decodedSecret = Encoding.UTF8.GetString(decodedBytes);
            results.Add(TestJwtValidation(handler, token, decodedSecret, "Base64 decoded secret (not recommended)"));
        } catch {
            results.Add(new { Format = "Base64 decoded secret", IsValid = false, Error = "Secret is not valid Base64" });
        }
        
        return Results.Ok(new { 
            SecretSource = secretSource,
            Results = results 
        });
    });
    
    // Helper method to test JWT validation with different secret formats
    object TestJwtValidation(System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler handler, string token, string secret, string format, bool useUtf8 = true)
    {
        try {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = useUtf8 
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                    : new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = new[] { "HS256" }
            };
            
            var principal = handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            return new { 
                Format = format, 
                IsValid = true,
                SecretPreview = secret.Length > 10 ? $"{secret[..3]}...{secret[^3..]}" : "Too short",
                SecretLength = secret.Length
            };
        }
        catch (Exception ex) {
            return new { 
                Format = format, 
                IsValid = false, 
                Error = ex.Message,
                SecretPreview = secret.Length > 10 ? $"{secret[..3]}...{secret[^3..]}" : "Too short",
                SecretLength = secret.Length
            };
        }
    }
}

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Auth error handler middleware (must be after authentication but before other middleware)
app.UseAuthErrorHandler();

// Supabase UID extraction middleware
app.UseSupabaseAuth();

// Controller routes
app.MapControllers();

app.Run();
