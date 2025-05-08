using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using CmdShiftLearn.Api.Auth;

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
            var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(',') ?? new[] { "*" };
            
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

// Configure Swagger with API Key support
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key Authentication. Example: \"ApiKey {key}\"",
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

// Configure API Key Authentication
Console.WriteLine("Configuring API Key authentication");

// Register API Key authentication services
builder.Services.AddSingleton<IApiKeyValidator, InMemoryApiKeyValidator>();

// Configure authentication
builder.Services
    .AddAuthentication(options => 
    {
        options.DefaultAuthenticateScheme = "ApiKey";
        options.DefaultChallengeScheme = "ApiKey";
    })
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

// Add authorization policy
builder.Services.AddAuthorization(options =>
{
    // Set a default policy that requires authentication for most endpoints
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    // But don't use the default policy as a fallback policy
    // This allows [AllowAnonymous] to work correctly
    options.FallbackPolicy = null;
});

// Configure application cookie settings globally
builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register services
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GitHub"); // Add a named HttpClient for GitHub API
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
builder.Services.AddSingleton<IEventLogger, EventLoggerService>();

// Configure OpenAI settings
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));

// Register HttpClient for OpenAI
builder.Services.AddHttpClient<IShelloService, ShelloService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    
    // Add API key if available - using GetValue to support both colon and double underscore formats
    var apiKey = builder.Configuration.GetValue<string>("OpenAI:ApiKey");
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }
});

// Register content loaders first (they are dependencies for services)
var tutorialSource = builder.Configuration.GetValue<string>("ContentSources:Tutorials:Source") ?? "File";
var challengeSource = builder.Configuration.GetValue<string>("ContentSources:Challenges:Source") ?? "File";

// Register tutorial loader based on configuration
Console.WriteLine($"Using {tutorialSource} as the source for tutorials");
switch (tutorialSource)
{
    case "GitHub":
        builder.Services.AddSingleton<ITutorialLoader, GitHubTutorialLoader>();
        Console.WriteLine("Registered GitHubTutorialLoader");
        break;
    case "File":
    default:
        builder.Services.AddSingleton<ITutorialLoader, FileTutorialLoader>();
        Console.WriteLine("Registered FileTutorialLoader");
        break;
}

// Register challenge loader based on configuration
Console.WriteLine($"Using {challengeSource} as the source for challenges");
switch (challengeSource)
{
    case "GitHub":
        builder.Services.AddSingleton<IChallengeLoader, GitHubChallengeLoader>();
        Console.WriteLine("Registered GitHubChallengeLoader");
        break;
    case "File":
    default:
        builder.Services.AddSingleton<IChallengeLoader, FileChallengeLoader>();
        Console.WriteLine("Registered FileChallengeLoader");
        break;
}

// Register tutorial and challenge services (after their dependencies)
builder.Services.AddSingleton<ITutorialService, TutorialService>();
builder.Services.AddSingleton<ChallengeService>();

var app = builder.Build();

// Add global exception handler
app.Use(async (context, next) => {
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
        }
        
        throw;
    }
});

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
   .WithOpenApi()
   .AllowAnonymous();

app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
