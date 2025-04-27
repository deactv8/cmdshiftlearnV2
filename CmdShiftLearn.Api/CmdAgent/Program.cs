using CmdAgent;
using CmdAgent.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CmdAgent
{
    class Program
    {
        private static readonly string ApiBaseUrl = "https://cmdshiftlearnv2.onrender.com/";
        private static HttpClient _httpClient = null!;

        static async Task Main(string[] args)
        {
            Console.WriteLine("CmdShiftLearn CLI");
            Console.WriteLine("=================");
            
            // Build host with HttpClientFactory
            var host = CreateHostBuilder(args).Build();
            
            // Get HttpClient from DI
            _httpClient = host.Services.GetRequiredService<IHttpClientFactory>()
                .CreateClient("CmdShiftLearnApi");
            
            // Get LoginService from DI
            var loginService = host.Services.GetRequiredService<LoginService>();
            
            // Login flow
            if (!await LoginFlow(loginService))
            {
                Console.WriteLine("\nLogin failed. Press any key to exit...");
                Console.ReadKey();
                return;
            }
            
            // Tutorial flow
            await TutorialFlow();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register HttpClient with base address
                    services.AddHttpClient("CmdShiftLearnApi", client =>
                    {
                        client.BaseAddress = new Uri(ApiBaseUrl);
                    });
                    
                    // Register LoginService
                    services.AddSingleton<LoginService>();
                });
        
        static async Task<bool> LoginFlow(LoginService loginService)
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
                var token = await loginService.LoginAsync(username, password);
                
                if (!string.IsNullOrEmpty(token))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Login successful!");
                    Console.ResetColor();
                    
                    // Set auth header on the HttpClient
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
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
            int currentStepIndex = 0;
            bool tutorialComplete = false;
            
            while (!tutorialComplete && currentStepIndex < tutorial.Steps.Count)
            {
                var step = tutorial.Steps[currentStepIndex];
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n📝 Step {currentStepIndex + 1}/{tutorial.Steps.Count}: {step.Instructions}");
                Console.ResetColor();
                
                // Get user input
                Console.Write("PS> ");
                string userInput = Console.ReadLine() ?? "";
                
                // Run step
                var result = await RunStepAsync(tutorial.Id, currentStepIndex, userInput);
                
                if (result != null)
                {
                    if (result.IsCorrect)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Correct! {result.Message}");
                        Console.ResetColor();
                        
                        // Move to next step or complete tutorial
                        if (result.IsComplete)
                        {
                            tutorialComplete = true;
                        }
                        else if (result.NextStepIndex.HasValue)
                        {
                            currentStepIndex = result.NextStepIndex.Value;
                        }
                        else
                        {
                            currentStepIndex++;
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Incorrect: {result.Message}");
                        Console.ResetColor();
                        
                        // Show hint if available
                        if (!string.IsNullOrEmpty(result.Hint))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"💡 Hint: {result.Hint}");
                            Console.ResetColor();
                        }
                        
                        // Show Shello hint if available
                        if (!string.IsNullOrEmpty(result.HintFromShello))
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"🤖 Shello says: {result.HintFromShello}");
                            Console.ResetColor();
                        }
                    }
                }
            }
            
            // Celebrate completion
            if (tutorialComplete)
            {
                Console.WriteLine("\n----------------------------------------");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"🎉 Tutorial Complete! Congratulations!");
                Console.WriteLine($"🏆 You've earned XP and improved your command line skills!");
                Console.ResetColor();
            }
        }
        
        static async Task<Tutorial?> LoadTutorialAsync(string tutorialId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/tutorials/{tutorialId}");
                
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
        
        static async Task<StepResult?> RunStepAsync(string tutorialId, int stepIndex, string userInput)
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
                
                var response = await _httpClient.PostAsync($"api/tutorials/run-step", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<StepResult>();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Failed to run step. Status code: {response.StatusCode}");
                    Console.ResetColor();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error running step: {ex.Message}");
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