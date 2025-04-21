﻿﻿﻿using Microsoft.Extensions.Hosting;
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
    var client = new HttpClient { BaseAddress = new Uri(apiBase) };
    Tutorial? tutorial;
    
    try
    {
        if (verbose)
        {
            Console.WriteLine($"🔍 Fetching tutorial with ID: {tutorialId} from {apiBase}");
        }
        
        tutorial = await client.GetFromJsonAsync<Tutorial>($"api/tutorials/{tutorialId}");
        if (tutorial?.Steps == null || tutorial.Steps.Count == 0)
        {
            Console.WriteLine("⚠️ This tutorial has no interactive steps.");
            return;
        }
        
        if (verbose)
        {
            Console.WriteLine($"✅ Successfully loaded tutorial: {tutorial.Title}");
            Console.WriteLine($"📋 Description: {tutorial.Description}");
            Console.WriteLine($"🔢 Number of steps: {tutorial.Steps.Count}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to load tutorial '{tutorialId}': {ex.Message}");
        if (verbose)
        {
            Console.WriteLine($"Exception details: {ex}");
        }
        return;
    }

    for (int i = 0; i < tutorial.Steps.Count; i++)
    {
        var step = tutorial.Steps[i];
        Console.WriteLine($"\nStep {i + 1}: {step.Title}\n{step.Instructions}\n");
        Console.Write("> ");
        var input = Console.ReadLine() ?? string.Empty;

        var response = await client.PostAsJsonAsync("/api/tutorials/run-step", new
        {
            tutorialId = tutorial.Id,
            stepIndex = i,
            userInput = input,
            requestHint = true
        });

        if (verbose)
        {
            Console.WriteLine($"📤 Sending step {i+1} response to server...");
        }

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Server returned error: {(int)response.StatusCode} {response.ReasonPhrase}");
            var raw = await response.Content.ReadAsStringAsync();
            if (verbose || response.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            {
                Console.WriteLine($"Raw response:\n{raw}");
            }
            break;
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (verbose)
            {
                Console.WriteLine($"📥 Raw server response:\n{content}");
            }
            
            var result = System.Text.Json.JsonSerializer.Deserialize<StepResult>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error parsing server response: {ex.Message}");
            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw response:\n{raw}");
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
