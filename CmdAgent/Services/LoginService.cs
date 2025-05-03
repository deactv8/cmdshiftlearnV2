using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CmdAgent.Services;

public class LoginService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public LoginService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    
    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new 
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Login failed: {response.StatusCode}");
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loginResponse = JsonDocument.Parse(responseContent);
                
                // Try to get token from response (handle different response formats)
                if (loginResponse.RootElement.TryGetProperty("token", out var tokenElement) && 
                    tokenElement.ValueKind == JsonValueKind.String)
                {
                    _token = tokenElement.GetString();
                }
                
                if (!string.IsNullOrEmpty(_token))
                {
                    // Set the auth token for subsequent requests
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", _token);
                    
                    return true;
                }
                
                Console.WriteLine("Invalid login response format: token not found");
                return false;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing login response: {ex.Message}");
                Console.WriteLine($"Response content: {responseContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }
    
    public void Logout()
    {
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}