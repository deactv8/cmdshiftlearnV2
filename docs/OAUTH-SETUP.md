# OAuth Setup Guide for CmdShiftLearn

This guide explains how to set up OAuth authentication for CmdShiftLearn, both for local development and for deployment on Render.

## Local Development

For local development, OAuth credentials are stored in `appsettings.Development.json`. This file is excluded from source control for security reasons.

1. Create or update `appsettings.Development.json` with your OAuth credentials:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    },
    "Jwt": {
      "Secret": "your-jwt-secret-key-for-local-development-only",
      "Issuer": "https://localhost:7071",
      "Audience": "cmdshiftlearn-api-dev",
      "ExpiryMinutes": 60
    }
  }
}
```

## Deployment on Render

For deployment on Render, OAuth credentials are stored as environment variables. This is more secure than storing them in configuration files.

1. In the Render dashboard, navigate to your web service.
2. Go to the "Environment" tab.
3. Add the following environment variables:

| Key | Value |
|-----|-------|
| `Authentication__Google__ClientId` | Your Google Client ID |
| `Authentication__Google__ClientSecret` | Your Google Client Secret |
| `Authentication__GitHub__ClientId` | Your GitHub Client ID |
| `Authentication__GitHub__ClientSecret` | Your GitHub Client Secret |
| `Authentication__Jwt__Secret` | Your JWT Secret Key |
| `Authentication__Jwt__Issuer` | `https://cmdshiftlearnv2.onrender.com` |
| `Authentication__Jwt__Audience` | `cmdshiftlearn-api` |

Note: Render uses double underscores (`__`) to represent nested configuration values.

## Obtaining OAuth Credentials

### Google OAuth

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project or select an existing one.
3. Navigate to "APIs & Services" > "Credentials".
4. Click "Create Credentials" > "OAuth client ID".
5. Select "Web application" as the application type.
6. Add authorized redirect URIs:
   - For local development: `https://localhost:7071/auth/callback?provider=google`
   - For production: `https://cmdshiftlearnv2.onrender.com/auth/callback?provider=google`
7. Click "Create" to generate your client ID and client secret.

### GitHub OAuth

1. Go to your GitHub account settings.
2. Navigate to "Developer settings" > "OAuth Apps".
3. Click "New OAuth App".
4. Fill in the application details:
   - Application name: "CmdShiftLearn"
   - Homepage URL: 
     - For local development: `https://localhost:7071`
     - For production: `https://cmdshiftlearnv2.onrender.com`
   - Authorization callback URL:
     - For local development: `https://localhost:7071/auth/callback?provider=github`
     - For production: `https://cmdshiftlearnv2.onrender.com/auth/callback?provider=github`
5. Click "Register application" to generate your client ID and client secret.

## Testing Authentication

1. Navigate to `/auth-test.html` in your browser.
2. Click on the "Login with Google" or "Login with GitHub" button.
3. Follow the OAuth flow to authenticate.
4. After successful authentication, you should be redirected back to the application with a JWT token.

## Troubleshooting

If authentication is not working:

1. Check that the OAuth credentials are correctly configured.
2. Verify that the redirect URIs are correctly set up in the OAuth provider.
3. Check the application logs for any error messages.
4. Make sure the static files are being served correctly.
5. Verify that the environment variables are correctly set in Render.

For local development, you can run the `scripts/verify-static-files.ps1` script to check that the static files are included in the published output.