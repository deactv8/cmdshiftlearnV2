using CmdAgent;
using CmdAgent.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CmdAgent
{
    class Program
    {
        private static readonly string ApiBaseUrl = "https://cmdshiftlearnv2.onrender.com/";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static LoginService? _loginService;
        private static int _totalXp = 0;

        static async Task Main(string[] args)
        {
            Console.WriteLine("CmdShiftLearn CLI");
            Console.WriteLine("=================");
            
            // Initialize login service
            _loginService = new LoginService(ApiBaseUrl);
            
            // Login flow
            if (!await LoginFlow())
            {
                Console.WriteLine("\nLogin failed. Press any key to exit...");
                Console.ReadKey();
                return;
            }
            
            // Set auth header for future requests
            _loginService.SetAuthHeader();
            
            // Set auth header on the static HttpClient
            if (!string.IsNullOrEmpty(_loginService.GetToken()))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _loginService.GetToken());
            }
            
            // Tutorial flow
            await TutorialFlow();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static async Task<bool> LoginFlow()
        {
            Console.WriteLine("\n📝 Login to CmdShiftLearn");
            Console.WriteLine("------------------------");
            
            int attempts = 0;
            const int maxAttempts = 3;
            
            while (attempts < maxAttempts)
            {
                Console.Write("Username: ");
                string username = Console.ReadLine() ?? "";
                
                Console.Write("Password: ");
                string password = ReadPassword();
                Console.WriteLine();
                
                Console.WriteLine("Logging in...");
                var token = await _loginService!.LoginAsync(username, password);
                
                if (!string.IsNullOrEmpty(token))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Login successful!");
                    Console.ResetColor();
                    return true;
                }
                
                attempts++;
                if (attempts < maxAttempts)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️ Login failed. {maxAttempts - attempts} attempts remaining.");
                    Console.ResetColor();
                }
            }
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Maximum login attempts reached.");
            Console.ResetColor();
            return false;
        }
        
        static async Task TutorialFlow()
        {
            // Hardcoded tutorial ID for MVP
            const string tutorialId = "powershell-basics-1";
            
            Console.WriteLine("\n📚 Loading tutorial...");
            var tutorial = await LoadTutorialAsync(tutorialId);
            
            if (tutorial == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Failed to load tutorial.");
                Console.ResetColor();
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n📚 {tutorial.Title}");
            Console.WriteLine($"📋 {tutorial.Steps.Count} steps to complete");
            Console.ResetColor();
            Console.WriteLine("----------------------------------------");
            
            // Process each step
            for (int i = 0; i < tutorial.Steps.Count; i++)
            {
                var step = tutorial.Steps[i];
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n📝 Step {i + 1}/{tutorial.Steps.Count}: {step.Instruction}");
                Console.ResetColor();
                
                // Get user input
                Console.Write("PS> ");
                string userInput = Console.ReadLine() ?? "";
                
                // Validate step
                var result = await ValidateStepAsync(tutorial.Id, i, userInput);
                
                if (result != null)
                {
                    if (result.Correct)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Correct! Earned {result.XP} XP");
                        _totalXp += result.XP;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Incorrect: {result.Message}");
                    }
                    
                    Console.ResetColor();
                }
            }
            
            // Celebrate completion
            Console.WriteLine("\n----------------------------------------");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"🎉 Tutorial Complete! Total XP earned: {_totalXp}");
            Console.ResetColor();
        }
        
        static async Task<Tutorial?> LoadTutorialAsync(string tutorialId)
        {
            try
            {
                // Use the HttpClient from the LoginService which already has the auth header set
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}api/tutorials/{tutorialId}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Tutorial>();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Failed to load tutorial. Status code: {response.StatusCode}");
                    Console.ResetColor();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error loading tutorial: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }
        
        static async Task<StepValidationResult?> ValidateStepAsync(string tutorialId, int stepIndex, string userInput)
        {
            try
            {
                var payload = new
                {
                    stepIndex,
                    userInput
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");
                
                // Use the HttpClient from the LoginService which already has the auth header set
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}api/tutorials/{tutorialId}/validate-step", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<StepValidationResult>();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Failed to validate step. Status code: {response.StatusCode}");
                    Console.ResetColor();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error validating step: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }
        
        // Helper method to read password without displaying it
        static string ReadPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;
            
            do
            {
                key = Console.ReadKey(true);
                
                // Ignore control keys like Ctrl, Alt, etc.
                if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            
            return password.ToString();
        }
    }
}