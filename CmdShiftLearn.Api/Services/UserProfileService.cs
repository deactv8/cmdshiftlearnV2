using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile?> GetUserProfileAsync(string supabaseUid);
        Task<UserProfile> CreateUserProfileAsync(string supabaseUid, string email);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile);
    }

    public class UserProfileService : IUserProfileService
    {
        // In a real application, this would be stored in a database
        private static readonly Dictionary<string, UserProfile> _userProfiles = new();

        public Task<UserProfile?> GetUserProfileAsync(string supabaseUid)
        {
            _userProfiles.TryGetValue(supabaseUid, out var userProfile);
            return Task.FromResult(userProfile);
        }

        public Task<UserProfile> CreateUserProfileAsync(string supabaseUid, string email)
        {
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                SupabaseUid = supabaseUid,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _userProfiles[supabaseUid] = userProfile;
            return Task.FromResult(userProfile);
        }

        public Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            userProfile.UpdatedAt = DateTime.UtcNow;
            _userProfiles[userProfile.SupabaseUid] = userProfile;
            return Task.FromResult(userProfile);
        }
    }
}