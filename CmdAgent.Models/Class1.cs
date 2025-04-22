﻿namespace CmdAgent.Models;

/// <summary>
/// Represents a tutorial with metadata and interactive steps
/// </summary>
public class Tutorial
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Xp { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<TutorialStep> Steps { get; set; } = new List<TutorialStep>();
}

/// <summary>
/// Represents a step in an interactive tutorial
/// </summary>
public class TutorialStep
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string ExpectedCommand { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

/// <summary>
/// Represents the result of executing a tutorial step
/// </summary>
public class StepResult
{
    public bool IsCorrect { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public string? HintFromShello { get; set; }
    public int? NextStepIndex { get; set; }
    public bool IsComplete { get; set; }
}
