using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Net.Http;
using System.Net.Http.Json;
using CmdAgent.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddHttpClient("CmdApi", client =>
{
    // Use the Render.com deployment
    client.BaseAddress = new Uri("https://cmdshiftlearnv2.onrender.com/");
    
    // For demo purposes, use a guest mode or test mode if available
    // If this doesn't work, you'll need to set up proper authentication
    // by modifying this section with your API key or token
    client.DefaultRequestHeaders.Add("X-Demo-Mode", "true");
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CmdAgent");

var tutorialIdArg = new Argument<string>("tutorialId", "The ID of the tutorial to run");
var apiOption = new Option<string>("--api", () => "https://cmdshiftlearnv2.onrender.com/", "The base URL of the CmdShiftLearn API");
var verboseOption = new Option<bool>("--verbose", () => false, "Show detailed output including raw server responses");

var tutorialCommand = new Command("tutorial", "Run an interactive tutorial")
{
    tutorialIdArg,
    apiOption,
    verboseOption
};

tutorialCommand.SetHandler(async (string tutorialId, string apiBase, bool verbose) =>
{
    // Create HttpClient using the factory
    var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient("CmdApi");
    
    // Override the base address if provided
    if (!string.IsNullOrEmpty(apiBase) && apiBase != client.BaseAddress?.ToString())
    {
        if (verbose)
        {
            Console.WriteLine($"🔄 Using custom API base URL: {apiBase}");
        }
        client.BaseAddress = new Uri(apiBase);
    }
    
    if (verbose)
    {
        Console.WriteLine($"🔄 Fetching tutorial from API: {client.BaseAddress}api/tutorials/{tutorialId}");
    }
    
    // Fetch the tutorial from the API
    Tutorial? tutorial;
    try
    {
        var response = await client.GetAsync($"api/tutorials/{tutorialId}");
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"⚠️ API returned status code: {(int)response.StatusCode} {response.StatusCode}");
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error details: {errorContent}");
            return;
        }
        
        tutorial = await response.Content.ReadFromJsonAsync<Tutorial>();
        
        if (tutorial == null)
        {
            Console.WriteLine("⚠️ Failed to deserialize tutorial from API response.");
            return;
        }
        
        if (verbose)
        {
            Console.WriteLine($"✅ Successfully loaded tutorial: {tutorial.Title}");
            Console.WriteLine($"📋 Description: {tutorial.Description}");
            Console.WriteLine($"🔢 Number of steps: {tutorial.Steps?.Count ?? 0}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error fetching tutorial: {ex.Message}");
        if (verbose)
        {
            Console.WriteLine($"Exception details: {ex}");
        }
        return;
    }
    
    if (tutorial?.Steps == null || tutorial.Steps.Count == 0)
    {
        Console.WriteLine("⚠️ This tutorial has no interactive steps.");
        return;
    }

    for (int i = 0; i < tutorial.Steps.Count; i++)
    {
        var step = tutorial.Steps[i];
        Console.WriteLine($"\nStep {i + 1}: {step.Title}\n{step.Instructions}\n");
        Console.Write("> ");
        var input = Console.ReadLine() ?? string.Empty;

        if (verbose)
        {
            Console.WriteLine($"📤 Sending step {i+1} to API...");
        }

        try
        {
            // Send the user input to the API
            var stepRequest = new
            {
                TutorialId = tutorialId,
                StepIndex = i,
                UserInput = input,
                RequestHint = false
            };
            
            var response = await client.PostAsJsonAsync("api/tutorials/run-step", stepRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️ API returned status code: {(int)response.StatusCode} {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error details: {errorContent}");
                continue;
            }
            
            var result = await response.Content.ReadFromJsonAsync<StepResult>();
            
            if (verbose)
            {
                Console.WriteLine($"📥 API response: IsCorrect={result?.IsCorrect}, IsComplete={result?.IsComplete}");
            }
            
            if (result == null)
            {
                Console.WriteLine("⚠️ Unexpected response format from the server.");
                break;
            }

            Console.WriteLine(result.IsCorrect ? $"\n✅ {result.Message}" : $"\n❌ {result.Message}");

            if (!result.IsCorrect && result.Hint != null)
                Console.WriteLine($"Hint: {result.Hint}");
            if (!result.IsCorrect && result.HintFromShello != null)
                Console.WriteLine($"🤖 Shello says: {result.HintFromShello}");

            if (result.IsComplete)
            {
                Console.WriteLine("\n🎉 You've completed the tutorial!");
                break;
            }
            
            // If correct but not complete, move to the next step
            if (result.IsCorrect && result.NextStepIndex.HasValue)
            {
                i = result.NextStepIndex.Value - 1; // -1 because the loop will increment i
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error processing step: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"Exception details: {ex}");
            }
            break;
        }
    }
}, tutorialIdArg, apiOption, verboseOption);

// Temporarily comment out CLI logic for demo purposes
// var root = new RootCommand("CmdAgent CLI")
// {
//     tutorialCommand
// };
// return await root.InvokeAsync(args);

// Run the demo directly
Console.WriteLine("🚀 Starting CmdShiftLearn Demo Runner...");
Console.WriteLine("----------------------------------------");

// Create HttpClient using the factory
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var client = httpClientFactory.CreateClient("CmdApi");
// Use the original tutorial ID
var tutorialId = "powershell-basics-1";

// Fetch the tutorial from the API
Console.WriteLine($"📥 Fetching tutorial: {tutorialId}...");
var response = await client.GetAsync($"api/tutorials/{tutorialId}");

// Initialize a variable for the tutorial
Tutorial tutorial;

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"⚠️ API returned status code: {(int)response.StatusCode} {response.StatusCode}");
    var errorContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Error details: {errorContent}");
    
    // Create a mock tutorial for demonstration purposes
    Console.WriteLine("\n⚠️ Unable to authenticate with the API. Using a mock tutorial for demonstration purposes.");
    
    tutorial = new Tutorial
    {
        Id = "powershell-basics-demo",
        Title = "PowerShell Basics Demo",
        Description = "A demonstration of PowerShell basics",
        Steps = new List<TutorialStep>
        {
            new TutorialStep 
            { 
                Title = "Getting Help", 
                Instructions = "Let's start by learning how to get help in PowerShell. Try the Get-Help command.", 
                ExpectedCommand = "Get-Help" 
            },
            new TutorialStep 
            { 
                Title = "Listing Commands", 
                Instructions = "Now let's see what commands are available. Try the Get-Command cmdlet.", 
                ExpectedCommand = "Get-Command" 
            },
            new TutorialStep 
            { 
                Title = "Working with Files", 
                Instructions = "Let's list the files in the current directory using Get-ChildItem.", 
                ExpectedCommand = "Get-ChildItem" 
            },
            new TutorialStep 
            { 
                Title = "Getting System Information", 
                Instructions = "Let's get information about the operating system using Get-ComputerInfo.", 
                ExpectedCommand = "Get-ComputerInfo" 
            }
        }
    };
}
else
{
    // Only try to parse the response if the request was successful
    tutorial = await response.Content.ReadFromJsonAsync<Tutorial>();
}

if (tutorial.Steps == null || tutorial.Steps.Count == 0)
{
    Console.WriteLine("⚠️ Tutorial has no steps.");
    return 1;
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"📚 Tutorial Loaded: {tutorial.Title}");
Console.WriteLine($"📋 {tutorial.Steps.Count} steps to complete");
Console.ResetColor();
Console.WriteLine("----------------------------------------");

await Task.Delay(1000);

// Auto-play through tutorial steps
int totalXp = 0;
Random random = new Random();

for (int i = 0; i < tutorial.Steps.Count; i++)
{
    var step = tutorial.Steps[i];
    
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n📝 Step {i + 1}/{tutorial.Steps.Count}: {step.Instructions}");
    Console.ResetColor();
    
    await Task.Delay(1000);
    
    // Simulate typing the command
    string expectedCommand = step.ExpectedCommand ?? "Get-Command";
    Console.Write("PS> ");
    
    // Simulate typing with random delays
    foreach (char c in expectedCommand)
    {
        Console.Write(c);
        await Task.Delay(random.Next(50, 150));
    }
    Console.WriteLine();
    
    // For the demo, simulate a successful completion instead of calling the API
    // This allows the demo to work even without proper API authentication
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Correct! Great job executing the {expectedCommand} command!");
    totalXp += 10; // Each correct step earns 10 XP
    Console.ResetColor();
    
    // In a real implementation, we would submit the command to the API:
    /*
    var stepRequest = new
    {
        TutorialId = tutorialId,
        StepIndex = i,
        UserInput = expectedCommand,
        RequestHint = false
    };
    
    var stepResponse = await client.PostAsJsonAsync("api/tutorials/run-step", stepRequest);
    
    if (stepResponse.IsSuccessStatusCode)
    {
        var result = await stepResponse.Content.ReadFromJsonAsync<StepResult>();
        
        if (result != null)
        {
            if (result.IsCorrect)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Correct! {result.Message}");
                totalXp += 10; // Assuming each correct step earns 10 XP
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Incorrect: {result.Message}");
            }
            
            Console.ResetColor();
        }
    }
    */
    
    await Task.Delay(1500);
}

// Celebrate at the end
Console.WriteLine("\n----------------------------------------");
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine($"🎉 Tutorial Complete! Total XP earned: {totalXp}");
Console.ResetColor();

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

return 0;
