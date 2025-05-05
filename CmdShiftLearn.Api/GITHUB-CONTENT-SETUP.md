# Setting Up GitHub Content Loading for CmdShiftLearn

This guide explains how to set up CmdShiftLearn.Api to load tutorials and challenges from a GitHub repository.

## Prerequisites

1. A GitHub repository to store your content
2. Basic understanding of YAML or JSON file formats
3. Git installed on your local machine

## Content Repository Structure

Your GitHub repository should follow this structure:

```
repository/
├── tutorials/
│   ├── beginner/
│   │   ├── tutorial1.yaml
│   │   ├── tutorial2.yaml
│   │   └── ...
│   ├── intermediate/
│   │   └── ...
│   └── advanced/
│       └── ...
├── challenges/
│   ├── beginner/
│   │   ├── challenge1.yaml
│   │   ├── challenge2.yaml
│   │   └── ...
│   ├── intermediate/
│   │   └── ...
│   └── advanced/
│       └── ...
```

## Environment Variables for GitHub Content Source

When deploying to Render or running locally, ensure these environment variables are set:

| Variable | Description | Example |
|----------|-------------|---------|
| `GitHub__Owner` | The GitHub username or organization | `deactv8` |
| `GitHub__Repo` | The repository name | `content` |
| `GitHub__Branch` | The branch to use | `master` or `main` |
| `GitHub__TutorialsPath` | Path to tutorials in the repo | `tutorials` |
| `GitHub__ChallengesPath` | Path to challenges in the repo | `challenges` |
| `GitHub__RawBaseUrl` | Base URL for raw content | `https://raw.githubusercontent.com` |
| `GitHub__AccessToken` | GitHub personal access token | `ghp_1234567890abcdef` |
| `ContentSources__Tutorials__Source` | Source for tutorials | `GitHub` |
| `ContentSources__Challenges__Source` | Source for challenges | `GitHub` |

## Set Up for Render Deployment

In the Render dashboard, add these environment variables to your Web Service settings.

## Set Up for Local Development

For local development, you can set these variables in `appsettings.Development.json`:

```json
{
  "GitHub": {
    "Owner": "deactv8",
    "Repo": "content",
    "Branch": "master",
    "TutorialsPath": "tutorials",
    "ChallengesPath": "challenges",
    "AccessToken": "your_github_token_here",
    "RawBaseUrl": "https://raw.githubusercontent.com"
  },
  "ContentSources": {
    "Tutorials": {
      "Source": "GitHub"
    },
    "Challenges": {
      "Source": "GitHub"
    }
  }
}
```

## Creating Content Files

### Tutorial YAML Example

```yaml
id: powershell-basics
title: PowerShell Basics
description: Learn the basics of PowerShell scripting
difficulty: beginner
xp: 100
content: |
  # PowerShell Basics
  
  In this tutorial, you will learn the basics of PowerShell scripting.
  
  ## Variables
  
  PowerShell variables are prefixed with a $ symbol:
  
  ```powershell
  $name = "John"
  $age = 25
  ```
  
  ## Basic Commands
  
  PowerShell has many built-in commands called cmdlets:
  
  ```powershell
  Get-Process
  Get-ChildItem
  Write-Host "Hello, World!"
  ```
```

### Challenge YAML Example

```yaml
id: powershell-variables
title: PowerShell Variables Challenge
description: Test your knowledge of PowerShell variables
difficulty: beginner
xp: 50
tutorialId: powershell-basics
script: |
  # PowerShell Variables Challenge
  
  # 1. Create a variable named $name and assign your name to it
  # 2. Create a variable named $age and assign your age to it
  # 3. Create a variable named $skills as an array with at least three skills
  # 4. Print a message using these variables
  
  # Your solution below:
  
steps:
  - id: step1
    title: Create variables
    expectedCommand: "$name = 'YourName'; $age = 25; $skills = @('Skill1', 'Skill2', 'Skill3')"
    hint: "Remember to use $ for variables and @() for arrays"
```

## Updating Content

1. Create or edit YAML/JSON files in your local clone of the repository
2. Commit and push your changes to GitHub
3. The API will automatically load the latest content on the next request (no cache)

## Troubleshooting

If content is not loading correctly:

1. Check the API logs for any errors related to GitHub loading
2. Verify your environment variables are set correctly
3. Ensure your GitHub access token has permission to read from the repository
4. Make sure your YAML or JSON files are properly formatted
5. Confirm the file paths in your repository match the configured paths

## Benefits of GitHub Content Source

- Version control for your content
- Easy collaboration with others
- Simple content updates without redeploying the API
- Separation of content from code
- Public or private repository options
