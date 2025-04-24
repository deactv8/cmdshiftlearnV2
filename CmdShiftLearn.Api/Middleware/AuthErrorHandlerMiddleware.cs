using System.Net;
using System.Security.Authentication;

namespace CmdShiftLearn.Api.Middleware
{
    /// <summary>
    /// Middleware to handle authentication errors and redirect to auth-error.html
    /// </summary>
    public class AuthErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AuthErrorHandlerMiddleware> _logger;

        public AuthErrorHandlerMiddleware(
            RequestDelegate next, 
            IWebHostEnvironment env,
            ILogger<AuthErrorHandlerMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
                
                // If we get a 401 Unauthorized response, handle it
                if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    // For API requests, leave the JSON response as is
                    if (IsApiRequest(context.Request))
                    {
                        // The response has already been set by the authentication middleware
                        _logger.LogDebug("API request received 401 Unauthorized, returning JSON response");
                        return;
                    }
                    
                    // Check if response has already started before attempting to redirect
                    if (context.Response.HasStarted)
                    {
                        Console.WriteLine("❌ Cannot redirect, response has already started.");
                        return;
                    }
                    
                    try
                    {
                        // For browser requests, redirect to the error page
                        _logger.LogInformation("Browser request received 401 Unauthorized, redirecting to auth-error.html");
                        context.Response.Redirect("/auth-error.html");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Redirect failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during request processing");
                
                // Don't modify the response if it has already started
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response has already started, cannot modify status code");
                    throw;
                }

                // For authentication exceptions, redirect to auth-error.html for browser requests
                if ((ex is Exception || ex.InnerException is Exception) && 
                    !IsApiRequest(context.Request) &&
                    (ex.Message.Contains("auth") || ex.Message.Contains("token") || ex.Message.Contains("unauthorized")))
                {
                    try
                    {
                        if (!context.Response.HasStarted)
                        {
                            _logger.LogInformation("Authentication exception occurred, redirecting to auth-error.html");
                            context.Response.Redirect("/auth-error.html");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("❌ Cannot redirect, response has already started.");
                        }
                    }
                    catch (Exception redirectEx)
                    {
                        Console.WriteLine($"❌ Redirect failed: {redirectEx.Message}");
                    }
                }

                // Let other middleware handle other types of exceptions
                throw;
            }
        }

        private bool IsApiRequest(HttpRequest request)
        {
            // Check if the request is for the API
            if (request.Path.StartsWithSegments("/api"))
            {
                return true;
            }

            // Check if the request accepts JSON
            if (request.Headers.Accept.ToString().Contains("application/json") && 
                !request.Headers.Accept.ToString().Contains("text/html"))
            {
                return true;
            }

            // Check if it's an AJAX request
            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return true;
            }

            return false;
        }
    }

    // Extension method to add the middleware to the request pipeline
    public static class AuthErrorHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthErrorHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthErrorHandlerMiddleware>();
        }
    }
}