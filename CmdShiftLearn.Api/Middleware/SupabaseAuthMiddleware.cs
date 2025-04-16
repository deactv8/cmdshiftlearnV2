using System.Security.Claims;
using System.Text.Json;
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
                string? supabaseUid = null;
                
                // First try the direct way (should work with our NameClaimType = "sub" setting)
                supabaseUid = context.User.FindFirstValue("sub");
                
                // If that fails, try to extract from user_metadata
                if (string.IsNullOrEmpty(supabaseUid))
                {
                    var userMetadataClaim = context.User?.FindFirst("user_metadata")?.Value;
                    if (!string.IsNullOrEmpty(userMetadataClaim))
                    {
                        try
                        {
                            var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userMetadataClaim);
                            if (metadata != null && metadata.TryGetValue("sub", out var subValue))
                            {
                                supabaseUid = subValue.GetString();
                                
                                // Attach it to context for later use
                                context.Items["UserId"] = supabaseUid;
                                
                                // Add the claim if it doesn't exist
                                var identity = context.User.Identity as ClaimsIdentity;
                                if (identity != null && !context.User.HasClaim(c => c.Type == "sub"))
                                {
                                    identity.AddClaim(new Claim("sub", supabaseUid));
                                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, supabaseUid));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing user_metadata: {ex.Message}");
                        }
                    }
                }
                
                // If we have a user ID, get or create the profile
                if (!string.IsNullOrEmpty(supabaseUid))
                {
                    // Get or create the user profile
                    var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
                    if (userProfile == null)
                    {
                        // Create a new user profile if it doesn't exist
                        var email = context.User.FindFirstValue(ClaimTypes.Email) ?? 
                                   context.User.FindFirstValue("email") ?? string.Empty;
                        userProfile = await _userProfileService.CreateUserProfileAsync(supabaseUid, email);
                    }

                    // Add the user profile to the HttpContext items for easy access in controllers
                    context.Items["UserProfile"] = userProfile;
                }
                else
                {
                    Console.WriteLine("Warning: Could not extract Supabase UID from token claims");
                    // Log all claims for debugging
                    foreach (var claim in context.User.Claims)
                    {
                        Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                    }
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