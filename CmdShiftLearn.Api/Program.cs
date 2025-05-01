using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using CmdShiftLearn.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
builder.Configuration
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure the port for Render deployment
// Render sets a PORT environment variable that we need to listen on
// See: https://render.com/docs/web-services#port-binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure forwarded headers for production environments with TLS termination (like Render)
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear(); // Allow all proxies
    options.KnownProxies.Clear();
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // In development, allow localhost
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:5173", "https://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // In production, use environment variable or default to all origins (can be restricted later)
            var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') ?? new[] { "*" };
            
            if (allowedOrigins.Length == 1 && allowedOrigins[0] == "*")
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});

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
// Get JWT secret directly from configuration to avoid any binding issues
var jwtSecret = builder.Configuration["Supabase:JwtSecret"] ?? 
                builder.Configuration["SUPABASE__JWTSECRET"] ?? 
                builder.Configuration["Supabase__JwtSecret"] ?? 
                builder.Configuration["Authentication:Jwt:Secret"] ?? 
                string.Empty;

// Log the JWT secret being used (masked for security)
Console.WriteLine($"DEBUG - JWT Secret Length: {jwtSecret.Length}");
Console.WriteLine($"DEBUG - JWT Secret Preview: {(string.IsNullOrEmpty(jwtSecret) ? "[MISSING]" : jwtSecret.Substring(0, Math.Min(4, jwtSecret.Length)) + "...")}");

// Add enhanced diagnostic logging
Console.WriteLine($"[DEBUG] JWT Secret Length: {jwtSecret.Length}");
Console.WriteLine($"[DEBUG] JWT Secret Present: {!string.IsNullOrEmpty(jwtSecret)}");
Console.WriteLine("Available Configuration Keys:");
foreach (var kv in builder.Configuration.AsEnumerable())
{
    if (kv.Key?.ToLower()?.Contains("jwt") == true)
    {
        Console.WriteLine($" {kv.Key} = {(string.IsNullOrEmpty(kv.Value) ? "[empty]" : "[set]")}");
    }
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
    Console.WriteLine("JWT Secret is NOT valid Base64 - this might be a problem as Supabase typically uses Base64 encoded secrets");
}

if (string.IsNullOrEmpty(jwtSecret))
{
    Console.WriteLine("WARNING: JWT Secret is empty or not found in configuration!");
}

// Configure JWT authentication with Supabase JWKS
try
{
    Console.WriteLine("Configuring JWT authentication with Supabase JWKS");
    
    builder.Services.AddAuthentication(options => 
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        // Define the Supabase JWKS URL
        string jwksUrl = "https://fqceiphubiqnorytayiu.supabase.co/auth/v1/keys";
        Console.WriteLine($"Using Supabase JWKS URL: {jwksUrl}");
        
        // Log audience configuration
        Console.WriteLine($"DEBUG - JWT Validation - Using Audience: authenticated (for Supabase compatibility)");
        
        // Configure to use Microsoft's JWKS handling
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,        // Skip issuer validation 
            ValidateAudience = true,       // Validate audience for Supabase compatibility
            ValidAudience = "authenticated", // Set audience to match Supabase tokens
            ValidateLifetime = true,       // Validate token expiration
            ValidateIssuerSigningKey = true, // Validate signature
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name
        };
        
        // Configure the JWKS retrieval
        options.MetadataAddress = jwksUrl;
        options.RequireHttpsMetadata = true;
        
        // Use a custom signing key resolver for JWKS
        options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            // Create a logger for key resolution
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Resolving signing key using JWKS endpoint: {JwksUrl}", jwksUrl);
            
            try
            {
                // Create a HttpClient to fetch the JWKS
                var httpClient = new HttpClient();
                var jwksJson = httpClient.GetStringAsync(jwksUrl).Result;
                logger.LogInformation("JWKS response received from Supabase");
                
                // Parse the JWKS response
                var jwks = System.Text.Json.JsonDocument.Parse(jwksJson).RootElement;
                var keys = jwks.GetProperty("keys");
                
                // Create signing keys from the JWKS
                var signingKeys = new List<SecurityKey>();
                foreach (var key in keys.EnumerateArray())
                {
                    if (key.TryGetProperty("kty", out var kty) && 
                        key.TryGetProperty("kid", out var keyId))
                    {
                        var jwk = new JsonWebKey
                        {
                            Kty = kty.GetString(),
                            Kid = keyId.GetString(),
                        };
                        
                        // Add required parameters based on key type
                        if (kty.GetString() == "RSA")
                        {
                            // Add required RSA parameters
                            if (key.TryGetProperty("n", out var n)) jwk.N = n.GetString();
                            if (key.TryGetProperty("e", out var e)) jwk.E = e.GetString();
                            if (key.TryGetProperty("use", out var use)) jwk.Use = use.GetString();
                            if (key.TryGetProperty("alg", out var alg)) jwk.Alg = alg.GetString();
                            
                            logger.LogInformation("Added RSA key with kid: {KeyId}, alg: {Algorithm}", 
                                keyId.GetString(), jwk.Alg ?? "unknown");
                        }
                        
                        signingKeys.Add(jwk);
                    }
                }
                
                logger.LogInformation("Successfully loaded {KeyCount} keys from JWKS endpoint", signingKeys.Count);
                return signingKeys;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching or parsing JWKS from Supabase");
                throw;
            }
        };
        
        // Log the actual ValidAudience value at runtime
        Console.WriteLine($"JWT ValidAudience at runtime: {options.TokenValidationParameters.ValidAudience}");
        
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully using JWKS!");
                Console.WriteLine($"User: {context.Principal?.Identity?.Name}");
                Console.WriteLine($"Claims: {string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
                
                // Add more detailed logging for audience validation failures
                if (context.Exception is SecurityTokenInvalidAudienceException audienceEx)
                {
                    Console.WriteLine($"AUDIENCE VALIDATION ERROR: {audienceEx.Message}");
                    Console.WriteLine($"Expected audience: 'authenticated'");
                    
                    // Try to extract the actual audience from the token
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        var jsonToken = handler.ReadJwtToken(token);
                        var audience = jsonToken.Audiences.FirstOrDefault() ?? "none";
                        Console.WriteLine($"Token audience: '{audience}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting audience from token: {ex.Message}");
                    }
                }
                
                if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
                {
                    Console.WriteLine("SIGNATURE KEY NOT FOUND ERROR - This may indicate the token was signed with a key not present in the JWKS");
                    Console.WriteLine("Please ensure the JWKS endpoint is correct and contains all necessary keys");
                }
                
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {context.Exception.InnerException.GetType().Name}: {context.Exception.InnerException.Message}");
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
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadJwtToken(context.Token);
                        Console.WriteLine($"Token issuer: {jsonToken.Issuer}");
                        Console.WriteLine($"Token algorithm: {jsonToken.Header.Alg}");
                        Console.WriteLine($"Token key ID (kid): {jsonToken.Header.Kid}");
                        Console.WriteLine($"Token issued at: {jsonToken.IssuedAt}");
                        Console.WriteLine($"Token expires at: {jsonToken.ValidTo}");
                        
                        // Log audience information
                        var audience = jsonToken.Audiences.FirstOrDefault() ?? "none";
                        Console.WriteLine($"Token audience: '{audience}'");
                        Console.WriteLine($"Expected audience: 'authenticated'");
                        Console.WriteLine($"Audience match: {audience == "authenticated"}");
                        
                        // Log all claims for debugging
                        Console.WriteLine("Token claims:");
                        foreach (var claim in jsonToken.Claims)
                        {
                            Console.WriteLine($"  {claim.Type}: {claim.Value}");
                        }
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
    })
    .AddGoogle("Google", options =>
    {
        // Log cookie configuration
        Console.WriteLine("🔐 Setting Google OAuth correlation cookie to SameSite=Lax, SecurePolicy=Always");
        
        var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        
        // Use the dedicated Google callback path to avoid conflicts
        options.CallbackPath = new PathString("/auth/google/callback");
        
        // Configure correlation cookie settings to ensure proper OAuth flow
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        
        // Debug logs to show actual values during startup
        Console.WriteLine($"DEBUG - Google Auth - ClientId = {(string.IsNullOrEmpty(googleClientId) ? "[MISSING]" : googleClientId)}");
        Console.WriteLine($"DEBUG - Google Auth - ClientSecret = {(string.IsNullOrEmpty(googleClientSecret) ? "[MISSING]" : googleClientSecret.Substring(0, Math.Min(4, googleClientSecret.Length)) + "...")}");
        
        // Map Google claims to standard claims
        // Temporarily commented out to allow build to succeed
        // options.ClaimActions.Map(ClaimTypes.NameIdentifier, "sub");
        // options.ClaimActions.Map(ClaimTypes.Name, "name");
        // options.ClaimActions.Map(ClaimTypes.Email, "email");
        // options.ClaimActions.Map("picture", "picture");
        
        if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
        {
            Console.WriteLine("WARNING: Google OAuth credentials are not configured!");
        }
    })
    .AddGitHub("GitHub", options =>
    {
        var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? string.Empty;
        var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;
        
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
        
        // Debug logs to show actual values during startup
        Console.WriteLine($"DEBUG - GitHub Auth - ClientId = {(string.IsNullOrEmpty(githubClientId) ? "[MISSING]" : githubClientId)}");
        Console.WriteLine($"DEBUG - GitHub Auth - ClientSecret = {(string.IsNullOrEmpty(githubClientSecret) ? "[MISSING]" : githubClientSecret.Substring(0, Math.Min(4, githubClientSecret.Length)) + "...")}");
        
        // Request additional scopes
        options.Scope.Add("user:email");
        
        if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
        {
            Console.WriteLine("WARNING: GitHub OAuth credentials are not configured!");
        }
    });

// Add diagnostic logging for auth providers
Console.WriteLine($"[Auth Setup] Google ClientId = {(string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]) ? "[MISSING]" : "[SET]")}");
Console.WriteLine($"[Auth Setup] GitHub ClientId = {(string.IsNullOrEmpty(builder.Configuration["Authentication:GitHub:ClientId"]) ? "[MISSING]" : "[SET]")}");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR configuring authentication: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
    
    // Fallback to minimal authentication for development only
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("Falling back to minimal authentication for development");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                // Log fallback authentication settings
                Console.WriteLine("DEBUG - FALLBACK JWT Validation - Using Audience: authenticated (hardcoded for Supabase compatibility)");
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidAudience = "authenticated", // Set audience to match Supabase tokens
                    ValidateLifetime = false
                };
                
                // Log the actual ValidAudience value at runtime for fallback configuration
                Console.WriteLine($"JWT ValidAudience at runtime (fallback): {options.TokenValidationParameters.ValidAudience}");
            });
    }
}

builder.Services.AddAuthorization();

// Configure application cookie settings globally
builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register authentication services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
builder.Services.AddSingleton<IEventLogger, EventLoggerService>();

// Register tutorial and challenge services
builder.Services.AddSingleton<ITutorialService, TutorialService>();

// Configure OpenAI settings
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));

// Register HttpClient for OpenAI
builder.Services.AddHttpClient<IShelloService, ShelloService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    
    // Add API key if available
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }
});

// Register the appropriate content loader based on configuration
var tutorialsSource = builder.Configuration.GetValue<string>("ContentSources:Tutorials:Source") ?? "File";
var challengesSource = builder.Configuration.GetValue<string>("ContentSources:Challenges:Source") ?? "File";

Console.WriteLine($"Using {tutorialsSource} as the source for tutorials");
Console.WriteLine($"Using {challengesSource} as the source for challenges");

// Register tutorial loader
if (tutorialsSource.Equals("GitHub", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ITutorialLoader, GitHubTutorialLoader>();
    Console.WriteLine("Registered GitHubTutorialLoader");
    
    // Log GitHub configuration
    var owner = builder.Configuration["GitHub:Owner"] ?? "deactv8";
    var repo = builder.Configuration["GitHub:Repo"] ?? "content";
    var branch = builder.Configuration["GitHub:Branch"] ?? "master";
    var tutorialsPath = builder.Configuration["GitHub:TutorialsPath"] ?? "tutorials";
    var hasToken = !string.IsNullOrEmpty(builder.Configuration["GitHub:AccessToken"]);
    
    Console.WriteLine($"GitHub Configuration:");
    Console.WriteLine($"- Owner: {owner}");
    Console.WriteLine($"- Repo: {repo}");
    Console.WriteLine($"- Branch: {branch}");
    Console.WriteLine($"- TutorialsPath: {tutorialsPath}");
    Console.WriteLine($"- Has Access Token: {hasToken}");
}
else if (tutorialsSource.Equals("Supabase", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ITutorialLoader, SupabaseTutorialLoader>();
    Console.WriteLine("Registered SupabaseTutorialLoader");
}
else
{
    // Default to file-based loader
    builder.Services.AddSingleton<ITutorialLoader, FileTutorialLoader>();
    Console.WriteLine("Registered FileTutorialLoader");
}

var app = builder.Build();

// Configure the HTTP request pipeline
// Serve static files from wwwroot - placed first in the pipeline for better performance
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // In production, still enable Swagger but with a path prefix for security
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "api-docs/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/api-docs/v1/swagger.json", "CmdShiftLearn API v1");
        options.RoutePrefix = "api-docs";
    });
}

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSupabaseAuth();
app.UseAuthErrorHandler();
app.MapControllers();

app.Run();
