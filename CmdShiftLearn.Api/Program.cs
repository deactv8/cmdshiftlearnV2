using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using CmdShiftLearn.Api.Models;
using CmdShiftLearn.Api.Services;
using CmdShiftLearn.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// Configure Supabase settings
var supabaseSettings = builder.Configuration.GetSection("Supabase").Get<SupabaseSettings>();
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                supabaseSettings?.JwtSecret ?? "default-dev-secret-key-for-testing-only")),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add services
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();

var app = builder.Build();
app.MapGet("/", () => "CmdShiftLearn API is working!");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add health endpoint
app.MapGet("/health", () => new { status = "OK" });

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Add Supabase auth middleware
app.UseSupabaseAuth();

app.MapControllers();

app.Run();