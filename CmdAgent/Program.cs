using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using CmdAgent.Models;
using CmdAgent.Services;

namespace CmdAgent;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Clean terminal
        Console.Clear();
        
        // Setup dependency injection
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure services
        builder.Services.AddLogging(config => 
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Information);
        });
        
        // Configure HttpClient
        builder.Services.AddHttpClient("CmdApi", client => 
        {
            client.BaseAddress = new Uri("https://cmdshiftlearnv2.onrender.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        
        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            await RunCmdShiftLearnCli(app, logger);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static async Task RunCmdShiftLearnCli(IHost app, ILogger logger)
    {
        // Display welcome banner
        DisplayWelcomeBanner();
        
        // Create HTTP client and services
        var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("CmdApi");
        var loginService = new LoginService(httpClient);
        
        // Step 1: Login
        if (!await HandleLogin(loginService))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Login failed after multiple attempts. Exiting.");
            Console.ResetColor();
            return;
        }
        
        // Step 2: Fetch tutorial
        string tutorialId = "powershell-basics-1";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nFetching tutorial: {tutorialId}...");
        Console.ResetColor();
        
        var tutorial = await FetchTutorial(httpClient, tutorialId);
        
        // Step 3: Run through tutorial steps
        if (tutorial != null && tutorial.Steps != null && tutorial.Steps.Count > 0)
        {
            await RunTutorialSteps(httpClient, tutorial, tutorialId);
        }
    }
    
    private static void DisplayWelcomeBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
   ______  __       __ _______    ______  __    __ __    __ ______  ________  __        ______   ______ __ __    __
  /      \|  \     /  \       \  /      \|  \  |  \  \  |  \      \|        \|  \      |      \ |      \  \  \  |  \
 |  $$$$$$\ $$\   /  $$ $$$$$$$\|  $$$$$$\ $$  | $$ \$$ | $$\$$$$$$\$$\$$$$$$\ $$      \$$$$$$\| $$$$$$\$$ \$$ | $$
 | $$   \$$\$$$\ /  $$$ $$  | $$| $$___\$$\$$__| $$|  $$_| $$ | $$  | $$ | $$ $$        | $$ | $$ | $$ $$|  $$_| $$
 | $$      \$$$$\  $$$$\$$  | $$ \$$    \ |  \   $$ \  \  $$ | $$  | $$ | $$ $$        | $$ | $$ | $$ $$ \  \  $$ 
 | $$   __ | $$\$$ $$ $$ $$ | $$ _\$$$$$$\| $$$$$$$ /$$$$$$$ | $$  | $$ | $$ $$       _| $$ | $$ | $$ $$  \$$$$$$\
 | $$__/  \| $$ \$$$| $$$$ | $$|  \__| $$| $$  | $$|  \__| $$_| $$_ | $$_/ $$ $$_____ |  \__/  $$ _/ $$ $$|  \__| $$
  \$$    $$| $$  \$ | $$ | $$ $$ \$$    $$| $$  | $$ \$$    $$   $$ \|   $$ $$\$$     \ \$$    $$|   $$ $$  \$$    $$
   \$$$$$$  \$$      \$$  \$$$$   \$$$$$$  \$$   \$$  \$$$$$$$$\$$$$$$\$$$$$$$$ \$$$$$$$  \$$$$$$  \$$$$$$    \$$$$$$                                                                                                       
");
        Console.WriteLine("\n🚀 Welcome to CmdShiftLearn CLI!");
        Console.WriteLine("=================================\n");
        Console.ResetColor();
    }
    
    private static async Task<bool> HandleLogin(LoginService loginService)
    {
        int attempts = 0;
        const int maxAttempts = 3;
        
        while (attempts < maxAttempts)
        {
            Console.Write("Username: ");
            string username = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Password: ");
            string password = ReadMaskedInput();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nAuthenticating...");
            Console.ResetColor();
            
            if (await loginService.LoginAsync(username, password))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Login successful! Welcome, {username}!");
                Console.ResetColor();
                return true;
            }
            
            attempts++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Login failed. {maxAttempts - attempts} attempts remaining.\n");
            Console.ResetColor();
        }
        
        return false;
    }
    
    private static string ReadMaskedInput()
    {
        string password = string.Empty;
        ConsoleKeyInfo keyInfo;
        
        do
        {
            keyInfo = Console.ReadKey(true);
            
            if (keyInfo.Key != ConsoleKey.Enter && keyInfo.Key != ConsoleKey.Backspace)
            {
                password += keyInfo.KeyChar;
                Console.Write("*");
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
        } while (keyInfo.Key != ConsoleKey.Enter);
        
        Console.WriteLine();
        return password;
    }
    
    private static async Task<Tutorial?> FetchTutorial(HttpClient client, string tutorialId)
    {
        try
        {
            var response = await client.GetAsync($"api/tutorials/{tutorialId}");
            
            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to fetch tutorial: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error details: {errorContent}");
                Console.ResetColor();
                return null;
            }
            
            var tutorial = await response.Content.ReadFromJsonAsync<Tutorial>();
            
            if (tutorial != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Tutorial loaded: {tutorial.Title}");
                Console.WriteLine($"Number of steps: {tutorial.Steps?.Count ?? 0}");
                Console.ResetColor();
            }
            
            return tutorial;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error fetching tutorial: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }
    
    private static async Task RunTutorialSteps(HttpClient client, Tutorial tutorial, string tutorialId)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n----------------------------------------");
        Console.WriteLine($"Tutorial: {tutorial.Title}");
        Console.WriteLine("----------------------------------------");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Steps to complete: {tutorial.Steps!.Count}\n");
        Console.ResetColor();
        
        int totalXp = 0;
        
        for (int i = 0; i < tutorial.Steps.Count; i++)
        {
            var step = tutorial.Steps[i];
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nStep {i + 1}/{tutorial.Steps.Count}: {step.Instructions}");
            Console.ResetColor();
            
            bool stepCompleted = false;
            
            while (!stepCompleted)
            {
                Console.Write("PS> ");
                string userInput = Console.ReadLine() ?? string.Empty;
                
                if (userInput.Trim().ToLower() == "hint")
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Hint: Try using '{step.ExpectedCommand}'");
                    Console.ResetColor();
                    continue;
                }
                
                var stepRequest = new
                {
                    StepIndex = i,
                    UserInput = userInput,
                    RequestHint = false
                };
                
                try
                {
                    var response = await client.PostAsJsonAsync($"api/tutorials/{tutorialId}/validate-step", stepRequest);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error validating step: {response.StatusCode}");
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorContent}");
                        Console.ResetColor();
                        continue;
                    }
                    
                    var result = await response.Content.ReadFromJsonAsync<StepResult>();
                    
                    if (result == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid response from server");
                        Console.ResetColor();
                        continue;
                    }
                    
                    if (result.IsCorrect)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Correct! {result.Message}");
                        Console.ResetColor();
                        
                        int xpEarned = result.XpEarned > 0 ? result.XpEarned : 10;
                        totalXp += xpEarned;
                        
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"XP earned: +{xpEarned} (Total: {totalXp})");
                        Console.ResetColor();
                        
                        stepCompleted = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Incorrect. {result.Message}");
                        Console.ResetColor();
                        
                        if (!string.IsNullOrEmpty(result.Hint))
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"Hint: {result.Hint}");
                            Console.ResetColor();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
        
        // Celebrate completion
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n----------------------------------------");
        Console.WriteLine("🎉 Tutorial Complete!");
        Console.WriteLine("----------------------------------------");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Total XP earned: {totalXp}");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("You've successfully completed the tutorial!");
        Console.ResetColor();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}