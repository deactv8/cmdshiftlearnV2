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
builder.Configuration.AddEnvironmentVariables();

// Configure the port for Render deployment
// Render sets a PORT environment variable that we need to listen on
// See: https://render.com/docs/web-services#port-binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
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

// Configure multi-provider authentication
try
{
    builder.Services.AddAuthentication(options => 
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "CmdShiftLearn.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        // For JWT validation, we should use the raw JWT secret string
        if (string.IsNullOrEmpty(jwtSecret))
        {
            Console.WriteLine("ERROR: JWT Secret is empty! Authentication will fail.");
            // Provide a non-empty default for development only to prevent immediate crash
            // This won't work for actual token validation but prevents startup errors
            jwtSecret = builder.Environment.IsDevelopment() ? "development_fallback_key_not_for_production" : jwtSecret;
        }
        
        byte[] keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
        Console.WriteLine($"Using JWT secret for validation, key size: {keyBytes.Length} bytes");
        Console.WriteLine("Using the JWT secret as-is (without decoding Base64 first)");
        
        // Get issuer and audience with fallbacks
        var issuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? 
                     builder.Configuration["Supabase:Issuer"] ?? 
                     "https://cmdshiftlearn.api";
        
        var audience = builder.Configuration["Authentication:Jwt:Audience"] ?? 
                       builder.Configuration["Supabase:Audience"] ?? 
                       "cmdshiftlearn-api";
        
        Console.WriteLine($"JWT Validation - Issuer: {issuer}");
        Console.WriteLine($"JWT Validation - Audience: {audience}");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = new[] { "HS256" },
            NameClaimType = "sub"
        };
        
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
    })
    .AddGoogle("Google", options =>
    {
        var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        
        // Use the dedicated Google callback path to avoid conflicts
        options.CallbackPath = new PathString("/auth/google/callback");
        
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
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false
                };
            });
    }
}

builder.Services.AddAuthorization();

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
