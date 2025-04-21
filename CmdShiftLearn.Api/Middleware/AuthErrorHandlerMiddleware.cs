using System.Net;

namespace CmdShiftLearn.Api.Middleware
{
    public class AuthErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public AuthErrorHandlerMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
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
                    if (context.Request.Path.StartsWithSegments("/api") || 
                        context.Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        // The response has already been set by the authentication middleware
                        return;
                    }
                    
                    // For browser requests, redirect to the error page
                    context.Response.Redirect("/auth-error.html");
                }
            }
            catch (Exception)
            {
                // Let other middleware handle exceptions
                throw;
            }
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