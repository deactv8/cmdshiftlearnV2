using System.Security.Claims;
using CmdShiftLearn.Api.Services;

namespace CmdShiftLearn.Api.Middleware
{
    public class SupabaseAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUserProfileService _userProfileService;

        public SupabaseAuthMiddleware(RequestDelegate next, IUserProfileService userProfileService)
        {
            _next = next;
            _userProfileService = userProfileService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Extract the Supabase UID from the JWT claims
                var supabaseUid = context.User.FindFirstValue("sub");
                if (!string.IsNullOrEmpty(supabaseUid))
                {
                    // Get or create the user profile
                    var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
                    if (userProfile == null)
                    {
                        // Create a new user profile if it doesn't exist
                        var email = context.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                        userProfile = await _userProfileService.CreateUserProfileAsync(supabaseUid, email);
                    }

                    // Add the user profile to the HttpContext items for easy access in controllers
                    context.Items["UserProfile"] = userProfile;
                }
            }

            await _next(context);
        }
    }

    // Extension method to add the middleware to the request pipeline
    public static class SupabaseAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseSupabaseAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SupabaseAuthMiddleware>();
        }
    }
}