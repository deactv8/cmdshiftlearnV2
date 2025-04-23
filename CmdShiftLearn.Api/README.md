# CmdShiftLearn.Api

The API backend for the CmdShiftLearn platform, providing authentication, tutorial management, and user progress tracking.

## 🚀 Getting Started

### Prerequisites

- .NET 7.0 SDK or later
- Visual Studio 2022, VS Code, or Rider
- PowerShell or Bash for running scripts

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/cmdshiftlearn.git
   cd cmdshiftlearn/CmdShiftLearn.Api
   ```

2. Restore dependencies:
   ```
   dotnet restore
   ```

3. Configure environment variables (see below)

4. Run the application:
   ```
   dotnet run
   ```

## 🔐 Authentication

The API supports multiple authentication providers:

- Google OAuth
- GitHub OAuth
- Bluesky via AT Protocol
- JWT token validation

### Configuration

Authentication settings can be configured in several ways:

1. **appsettings.json** (default values, no secrets)
2. **appsettings.Development.json** (local development only, not checked into source control)
3. **Environment variables** (recommended for production)

### Environment Variables

For security, we use environment variables to store sensitive credentials. The following variables are required:

#### Authentication

| Variable | Description |
|----------|-------------|
| `Authentication__Google__ClientId` | Google OAuth client ID |
| `Authentication__Google__ClientSecret` | Google OAuth client secret |
| `Authentication__GitHub__ClientId` | GitHub OAuth client ID |
| `Authentication__GitHub__ClientSecret` | GitHub OAuth client secret |
| `Authentication__Jwt__Secret` | JWT secret key for token generation and validation |
| `Authentication__Jwt__Issuer` | JWT issuer (e.g., `https://api.cmdshiftlearn.com`) |
| `Authentication__Jwt__Audience` | JWT audience (e.g., `cmdshiftlearn-api`) |

#### Supabase

| Variable | Description |
|----------|-------------|
| `Supabase__Url` | Supabase URL |
| `Supabase__ApiKey` | Supabase API key |
| `Supabase__JwtSecret` | Supabase JWT secret |

#### GitHub

| Variable | Description |
|----------|-------------|
| `GitHub__AccessToken` | GitHub access token for content repository |

#### OpenAI

| Variable | Description |
|----------|-------------|
| `OpenAI__ApiKey` | OpenAI API key |

### Testing Environment Variables

For local testing, you can use the provided scripts:

- Windows: `scripts/test-env-vars.ps1`
- Linux/macOS: `scripts/test-env-vars.sh`

These scripts set test environment variables and run the API locally.

## 🧪 Testing

### Authentication Testing

You can test authentication using the included test page:

1. Run the API locally
2. Navigate to `https://localhost:7071/auth-test.html`
3. Test the different authentication providers

### API Testing

1. Run the API locally
2. Navigate to `https://localhost:7071/swagger` for Swagger UI
3. Use the Swagger UI to test API endpoints

## 📚 Documentation

- [API Documentation](https://localhost:7071/swagger)
- [Render Deployment Guide](../docs/render-deployment.md)

## 🔄 Continuous Integration

The API is automatically deployed to Render on push to the main branch.

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.