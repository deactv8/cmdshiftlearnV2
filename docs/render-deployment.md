# Render Deployment Configuration

This document outlines the environment variables that need to be set in the Render dashboard for the CmdShiftLearn.Api application.

## Deployment Steps

1. Create a new Web Service in Render
2. Connect your GitHub repository
3. Configure the following settings:
   - Name: `cmdshiftlearnv2`
   - Environment: `Docker`
   - Branch: `main` (or your preferred branch)
   - Root Directory: `/` (or the directory containing your Dockerfile)
   - Build Command: `docker build -t cmdshiftlearn .`
   - Start Command: `docker run -p 8080:80 cmdshiftlearn`

## Environment Variables

The following environment variables should be set in the Render dashboard:

### Authentication

| Key | Description |
|-----|-------------|
| `Authentication__Google__ClientId` | Google OAuth client ID |
| `Authentication__Google__ClientSecret` | Google OAuth client secret |
| `Authentication__GitHub__ClientId` | GitHub OAuth client ID |
| `Authentication__GitHub__ClientSecret` | GitHub OAuth client secret |
| `Authentication__Jwt__Secret` | JWT secret key for token generation and validation |
| `Authentication__Jwt__Issuer` | JWT issuer (e.g., `https://api.cmdshiftlearn.com`) |
| `Authentication__Jwt__Audience` | JWT audience (e.g., `cmdshiftlearn-api`) |
| `Authentication__Jwt__ExpiryMinutes` | JWT token expiry in minutes (e.g., `60`) |

### Supabase

| Key | Description |
|-----|-------------|
| `Supabase__Url` | Supabase URL |
| `Supabase__ApiKey` | Supabase API key |
| `Supabase__JwtSecret` | Supabase JWT secret |

### GitHub

| Key | Description |
|-----|-------------|
| `GitHub__AccessToken` | GitHub access token for content repository |

### OpenAI

| Key | Description |
|-----|-------------|
| `OpenAI__ApiKey` | OpenAI API key |

## Setting Environment Variables in Render

1. Go to the Render dashboard
2. Select your CmdShiftLearn.Api service
3. Click on the "Environment" tab
4. Add each of the environment variables listed above
5. Click "Save Changes"

## Callback URLs

Configure the following callback URLs in your OAuth providers:

### Google

- Authorized redirect URI: `https://cmdshiftlearnv2.onrender.com/auth/callback?provider=google`

### GitHub

- Authorization callback URL: `https://cmdshiftlearnv2.onrender.com/auth/callback?provider=github`

## Testing the Deployment

1. Navigate to `https://cmdshiftlearnv2.onrender.com/auth-test.html`
2. Test the authentication flow with Google and GitHub
3. Check the logs in the Render dashboard for any errors

## Notes

- Use double underscores (`__`) to represent nested configuration values
- All environment variables are case-sensitive
- Restart the service after updating environment variables
- For local development, use the `appsettings.Development.json` file instead

## Troubleshooting

If you encounter issues with the deployment:

1. Check the logs in the Render dashboard
2. Verify that all environment variables are correctly set
3. Make sure the callback URLs are correctly configured in your OAuth providers
4. Check that the static files are being served correctly

For more information, see the [OAuth Setup Guide](./OAUTH-SETUP.md).