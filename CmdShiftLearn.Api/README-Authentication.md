# CmdShiftLearn Authentication System

This document provides an overview of the authentication system implemented in CmdShiftLearn.Api.

## Features

- Multi-provider authentication:
  - Google OAuth
  - GitHub OAuth
  - Bluesky login via AT Protocol
- JWT token generation and validation
- XP bonuses for login events
- Protected API endpoints
- User profile management

## Authentication Flow

1. User initiates login via one of the providers:
   - Google: `/auth/google/login`
   - GitHub: `/auth/github/login`
   - Bluesky: POST to `/auth/bluesky` with handle and password

2. For OAuth providers (Google, GitHub):
   - User is redirected to the provider's login page
   - After successful authentication, the provider redirects back to `/auth/callback?provider=<provider>`
   - The callback handler creates or retrieves the user profile and issues a JWT token

3. For Bluesky:
   - The API calls the Bluesky AT Protocol endpoint to authenticate the user
   - Upon successful authentication, a user profile is created or retrieved and a JWT token is issued

4. The JWT token contains claims:
   - `sub`: User ID (includes provider prefix, e.g., "google:123456")
   - `email`: User's email address
   - `provider`: Authentication provider (google, github, bluesky)
   - Additional claims as needed

5. XP bonuses are awarded:
   - First login: +50 XP
   - Bluesky login: +100 XP

6. Protected endpoints require the JWT token in the Authorization header:
   - `Authorization: Bearer <token>`

## Configuration

Authentication settings are configured in `appsettings.json`:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "GitHub": {
    "ClientId": "YOUR_GITHUB_CLIENT_ID",
    "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET"
  },
  "Bluesky": {
    "ApiUrl": "https://bsky.social/xrpc"
  },
  "Jwt": {
    "Secret": "your_jwt_secret_key_here_make_it_long_and_secure",
    "Issuer": "https://cmdshiftlearn.api",
    "Audience": "cmdshiftlearn-api",
    "ExpiryMinutes": 60
  }
}
```

## Testing

The authentication system can be tested using the following HTML pages:

- `/auth-test.html`: Test authentication with different providers and JWT token validation
- `/auth-success.html`: Displayed after successful OAuth authentication
- `/auth-error.html`: Displayed when authentication errors occur

## API Endpoints

- `GET /auth/google/login`: Initiates Google OAuth login
- `GET /auth/github/login`: Initiates GitHub OAuth login
- `POST /auth/bluesky`: Authenticates with Bluesky credentials
- `GET /auth/callback?provider=<provider>`: OAuth callback handler
- `GET /auth/me`: Returns the current user's information (requires authentication)

## Protected Resources

The following API endpoints require authentication:

- `GET /api/tutorials/{id}`: Get a specific tutorial
- `POST /api/tutorials/run-step`: Process a tutorial step

## Implementation Details

- `AuthController.cs`: Handles authentication requests and callbacks
- `AuthService.cs`: Provides authentication logic and JWT token generation
- `UserProfileService.cs`: Manages user profiles and XP rewards
- `Program.cs`: Configures authentication middleware and JWT validation

## Future Enhancements

- Refresh tokens for extended sessions
- Role-based authorization
- Social profile integration
- Two-factor authentication
- Account linking (connecting multiple providers to the same user account)