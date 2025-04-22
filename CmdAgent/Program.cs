﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.Extensions.Hosting;
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
    client.BaseAddress = new Uri("https://cmdshiftlearnv2.onrender.com/");
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

var root = new RootCommand("CmdAgent CLI")
{
    tutorialCommand
};

return await root.InvokeAsync(args);
