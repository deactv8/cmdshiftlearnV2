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
            // Log the raw Authorization header for debugging
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                Console.WriteLine($"[SupabaseAuthMiddleware] Authorization header present: {authHeader.ToString()[0..Math.Min(15, authHeader.ToString().Length)]}...");
            }
            else
            {
                Console.WriteLine("[SupabaseAuthMiddleware] No Authorization header found");
            }
            
            // Check if the user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                Console.WriteLine("[SupabaseAuthMiddleware] User is authenticated");
                string? supabaseUid = null;
                
                // First try the direct way (should work with our NameClaimType = "sub" setting)
                supabaseUid = context.User.FindFirstValue("sub");
                Console.WriteLine($"[SupabaseAuthMiddleware] Found sub claim: {(supabaseUid != null ? "Yes" : "No")}");
                
                // If that fails, try to extract from user_metadata
                if (string.IsNullOrEmpty(supabaseUid))
                {
                    Console.WriteLine("[SupabaseAuthMiddleware] Trying to extract from user_metadata");
                    var userMetadataClaim = context.User?.FindFirst("user_metadata")?.Value;
                    if (!string.IsNullOrEmpty(userMetadataClaim))
                    {
                        try
                        {
                            Console.WriteLine($"[SupabaseAuthMiddleware] Found user_metadata: {userMetadataClaim[0..Math.Min(30, userMetadataClaim.Length)]}...");
                            var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userMetadataClaim);
                            if (metadata != null && metadata.TryGetValue("sub", out var subValue))
                            {
                                supabaseUid = subValue.GetString();
                                Console.WriteLine($"[SupabaseAuthMiddleware] Extracted sub from user_metadata: {supabaseUid}");
                                
                                // Attach it to context for later use
                                context.Items["UserId"] = supabaseUid;
                                
                                // Add the claim if it doesn't exist
                                var identity = context.User.Identity as ClaimsIdentity;
                                if (identity != null && !context.User.HasClaim(c => c.Type == "sub"))
                                {
                                    identity.AddClaim(new Claim("sub", supabaseUid));
                                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, supabaseUid));
                                    Console.WriteLine("[SupabaseAuthMiddleware] Added sub claim to identity");
                                }
                            }
                            else
                            {
                                Console.WriteLine("[SupabaseAuthMiddleware] No sub found in user_metadata");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[SupabaseAuthMiddleware] Error parsing user_metadata: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[SupabaseAuthMiddleware] No user_metadata claim found");
                    }
                }
                
                // If we have a user ID, get or create the profile
                if (!string.IsNullOrEmpty(supabaseUid))
                {
                    Console.WriteLine($"[SupabaseAuthMiddleware] Processing user with ID: {supabaseUid}");
                    // Get or create the user profile
                    var userProfile = await _userProfileService.GetUserProfileAsync(supabaseUid);
                    if (userProfile == null)
                    {
                        // Create a new user profile if it doesn't exist
                        Console.WriteLine("[SupabaseAuthMiddleware] Creating new user profile");
                        var email = context.User.FindFirstValue(ClaimTypes.Email) ?? 
                                   context.User.FindFirstValue("email") ?? string.Empty;
                        userProfile = await _userProfileService.CreateUserProfileAsync(supabaseUid, email);
                    }
                    else
                    {
                        Console.WriteLine("[SupabaseAuthMiddleware] Found existing user profile");
                    }

                    // Add the user profile to the HttpContext items for easy access in controllers
                    context.Items["UserProfile"] = userProfile;
                }
                else
                {
                    Console.WriteLine("[SupabaseAuthMiddleware] WARNING: Could not extract Supabase UID from token claims");
                    // Log all claims for debugging
                    Console.WriteLine("[SupabaseAuthMiddleware] Available claims:");
                    foreach (var claim in context.User.Claims)
                    {
                        Console.WriteLine($"[SupabaseAuthMiddleware] Claim: {claim.Type} = {claim.Value}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[SupabaseAuthMiddleware] User is NOT authenticated");
                
                // Check if there's an Authorization header but authentication failed
                if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    Console.WriteLine("[SupabaseAuthMiddleware] Authorization header is present but authentication failed");
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