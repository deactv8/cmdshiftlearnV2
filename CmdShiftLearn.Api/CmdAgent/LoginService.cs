using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CmdAgent
{
    public class LoginService
    {
        private readonly HttpClient _httpClient;
        private string? _authToken;

        public LoginService(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        /// <summary>
        /// Authenticates a user with username and password
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <returns>JWT token if successful, null if failed</returns>
        public async Task<string?> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new
                {
                    username,
                    password
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginRequest),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (loginResponse != null)
                    {
                        _authToken = loginResponse.Token;
                        return _authToken;
                    }
                }
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Login failed. Status code: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error details: {errorContent}");
                Console.ResetColor();
                
                return null;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error during login: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        public string? GetToken() => _authToken;

        /// <summary>
        /// Adds the authentication token to the HttpClient as a default header
        /// </summary>
        public void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(_authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
            }
        }
    }

    /// <summary>
    /// Response model for login endpoint
    /// </summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}