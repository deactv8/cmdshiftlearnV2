# Setting Up OpenAI API Key for Shello AI Assistant

The Shello AI Assistant in CmdShiftLearn uses OpenAI's API to generate helpful hints and guidance for users. To use this feature, you need to set up an OpenAI API key.

## Obtaining an OpenAI API Key

1. Create an account or log in at [OpenAI's platform](https://platform.openai.com/)
2. Navigate to the [API Keys section](https://platform.openai.com/api-keys)
3. Create a new API key
4. Copy the API key (you won't be able to see it again)

## Setting Up the API Key (In Order of Preference)

### 1. Environment Variable (Recommended)

Set the `OPENAI_API_KEY` environment variable:

#### Windows (PowerShell)
```powershell
$env:OPENAI_API_KEY = "your-api-key-here"
```

#### Windows (Command Prompt)
```cmd
set OPENAI_API_KEY=your-api-key-here
```

#### macOS/Linux
```bash
export OPENAI_API_KEY=your-api-key-here
```

### 2. User Secrets (Development Only)

For local development, you can use .NET's user secrets:

```bash
cd CmdShiftLearn.Api
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

### 3. appsettings.json (Not Recommended for Production)

As a last resort, you can add the API key to your `appsettings.json` or `appsettings.Development.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-3.5-turbo",
    "Temperature": 0.7
  }
}
```

**Note:** This method is not recommended for production as it risks exposing your API key in source control.

## Verifying the Setup

When you start the application, you should see a log message indicating that the OpenAI API key was loaded:

```
✅ OpenAI API key loaded from environment variable
```

If you see a warning message, it means the API key was not found:

```
⚠️ WARNING: OpenAI API key not found in environment variables or configuration!
```

## Testing the Shello AI Assistant

Once the API key is set up, you can test the Shello AI Assistant by making a request to the `/api/shello/hint` endpoint:

```
GET /api/shello/hint?tutorialId=powershell-basics-4&stepId=get-command&userInput=Get-Command
```

This should return a helpful hint from Shello based on the tutorial, step, and user input.