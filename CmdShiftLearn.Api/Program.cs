using System.Text;
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
});

// Load Supabase settings
var supabaseSettings = builder.Configuration.GetSection("Supabase").Get<SupabaseSettings>();
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));

// ✅ Add secure JWT authentication using Supabase token validation
// Get JWT secret directly from configuration to avoid any binding issues
var jwtSecret = builder.Configuration["Supabase:JwtSecret"] ?? string.Empty;

// Log the JWT secret being used (masked for security)
Console.WriteLine($"JWT Secret loaded: {(string.IsNullOrEmpty(jwtSecret) ? "EMPTY" : $"{jwtSecret[..Math.Min(3, jwtSecret.Length)]}...{(jwtSecret.Length > 3 ? jwtSecret[^Math.Min(3, jwtSecret.Length)..] : "")}")}");
Console.WriteLine($"JWT Secret length: {jwtSecret.Length}");

// Verify if the secret is Base64 encoded (Supabase JWT secrets are typically Base64 encoded)
bool isBase64 = false;
try {
    var bytes = Convert.FromBase64String(jwtSecret);
    isBase64 = true;
    Console.WriteLine($"JWT Secret is valid Base64, decoded length: {bytes.Length} bytes");
} catch {
    Console.WriteLine("JWT Secret is NOT valid Base64 - this might be a problem as Supabase typically uses Base64 encoded secrets");
}

if (string.IsNullOrEmpty(jwtSecret))
{
    Console.WriteLine("WARNING: JWT Secret is empty or not found in configuration!");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        // Try to handle the secret correctly based on Supabase's requirements
        byte[] keyBytes;
        
        // If the secret is Base64 encoded (which is likely for Supabase)
        if (isBase64)
        {
            // For Supabase, we should use the raw Base64 string as the key
            // This is important because Supabase uses the Base64 string directly, not its decoded value
            keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
            Console.WriteLine("Using Base64 string directly as the key");
        }
        else
        {
            // Fallback to using the string as-is
            keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
            Console.WriteLine("Using raw string as the key");
        }
        
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
        
        // Add detailed event logging for token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully!");
                Console.WriteLine($"User: {context.Principal?.Identity?.Name}");
                Console.WriteLine($"Claims: {string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {context.Exception.InnerException.GetType().Name}: {context.Exception.InnerException.Message}");
                }
                
                // If it's a SecurityTokenSignatureKeyNotFoundException, it means the key is wrong
                if (context.Exception is Microsoft.IdentityModel.Tokens.SecurityTokenSignatureKeyNotFoundException)
                {
                    Console.WriteLine("This suggests the JWT secret doesn't match what Supabase is using to sign tokens.");
                }
                
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"OnChallenge: {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrEmpty(context.Token))
                {
                    Console.WriteLine($"Token received: {context.Token.Substring(0, Math.Min(10, context.Token.Length))}...");
                    
                    try
                    {
                        // Try to decode the token without validation to see its structure
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadJwtToken(context.Token);
                        
                        Console.WriteLine($"Token issuer: {jsonToken.Issuer}");
                        Console.WriteLine($"Token algorithm: {jsonToken.Header.Alg}");
                        Console.WriteLine($"Token issued at: {jsonToken.IssuedAt}");
                        Console.WriteLine($"Token expires at: {jsonToken.ValidTo}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error decoding token: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("No token received in request");
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();

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
        var secret = builder.Configuration["Supabase:JwtSecret"] ?? "NOT FOUND IN CONFIG";
        return new {
            SecretFirstChars = string.IsNullOrEmpty(secret) ? "EMPTY" : secret.Length > 6 ? $"{secret[..3]}..." : "TOO SHORT",
            SecretLength = secret.Length,
            HasValidSecret = !string.IsNullOrEmpty(secret) && secret.Length > 32,
            Issuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = "TimeSpan.Zero"
        };
    });
}

app.UseHttpsRedirection();

// Health check
app.MapGet("/health", () => new { status = "OK" });

// Add a test endpoint to manually validate tokens (development only)
if (app.Environment.IsDevelopment())
{
    app.MapPost("/debug/validate-token", async (HttpContext context) => {
        try {
            // Read the token from the request body
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var token = requestBody.Trim();
            
            if (string.IsNullOrEmpty(token)) {
                return Results.BadRequest("No token provided");
            }
            
            // Get the JWT secret from configuration
            var secret = builder.Configuration["Supabase:JwtSecret"];
            if (string.IsNullOrEmpty(secret)) {
                return Results.Problem("JWT secret not configured");
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
                
                // Create token validation parameters
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
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
        
        var secret = builder.Configuration["Supabase:JwtSecret"];
        if (string.IsNullOrEmpty(secret)) {
            return Results.Problem("JWT secret not configured");
        }
        
        var results = new List<object>();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        
        // Test 1: Original secret as-is
        results.Add(TestJwtValidation(handler, token, secret, "Original secret as-is"));
        
        // Test 2: Base64 decoded secret
        try {
            var decodedBytes = Convert.FromBase64String(secret);
            var decodedSecret = Encoding.UTF8.GetString(decodedBytes);
            results.Add(TestJwtValidation(handler, token, decodedSecret, "Base64 decoded secret"));
        } catch {
            results.Add(new { Format = "Base64 decoded secret", IsValid = false, Error = "Secret is not valid Base64" });
        }
        
        // Test 3: UTF8 bytes of secret
        results.Add(TestJwtValidation(handler, token, secret, "UTF8 bytes of secret", useBytes: true));
        
        return Results.Ok(new { Results = results });
    });
    
    // Helper method to test JWT validation with different secret formats
    object TestJwtValidation(System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler handler, string token, string secret, string format, bool useBytes = false)
    {
        try {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1",
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = useBytes 
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                    : new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero
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

// Supabase UID extraction middleware
app.UseSupabaseAuth();

// Controller routes
app.MapControllers();

app.Run();
