namespace CmdShiftLearn.Api.Auth
{
    public interface IApiKeyValidator
    {
        bool IsValidApiKey(string apiKey, out string userId);
    }
}