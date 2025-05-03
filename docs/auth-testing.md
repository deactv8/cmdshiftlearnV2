# Authentication Testing

This document provides information about testing the authentication system for CmdShiftLearn.Api.

## Authentication Test Page

The authentication test page is available at:

```
https://cmdshiftlearnv2.onrender.com/auth-test.html
```

This page allows you to test the following authentication providers:

- Google OAuth
- GitHub OAuth
- Bluesky (via AT Protocol)

## Testing Google Authentication

1. Click the "Login with Google" button
2. You will be redirected to Google's login page
3. After successful authentication, you will be redirected back to the auth-success.html page
4. The page will display your authentication token and provider information

## Testing GitHub Authentication

1. Click the "Login with GitHub" button
2. You will be redirected to GitHub's login page
3. After successful authentication, you will be redirected back to the auth-success.html page
4. The page will display your authentication token and provider information

## Testing Bluesky Authentication

There are two ways to test Bluesky authentication:

### JavaScript-based Login (Default)

1. Click the "Login with Bluesky" button to reveal the login form
2. Enter your Bluesky handle (e.g., yourname.bsky.social)
3. Enter your Bluesky app password
4. Click the "Login with Bluesky (JS)" button
5. After successful authentication, your token will be displayed on the page

### Form-based Login (Alternative)

1. Click the "Login with Bluesky" button to reveal the login form
2. Scroll down to the "Alternative method" section
3. Enter your Bluesky handle and app password in the form
4. Click the "Login with Bluesky (Form)" button
5. After successful authentication, you will be redirected to the auth-success.html page

## Testing Protected API Endpoints

Once you have authenticated, you can test accessing protected API endpoints:

1. Click the "Get Tutorial (Requires Auth)" button in the API Test section
2. If your authentication is valid, the tutorial data will be displayed
3. If your authentication is invalid, an error message will be displayed

## JWT Testing

The JWT Testing tab allows you to:

1. Test a JWT token
2. Test the auth endpoint
3. Check JWT configuration
4. Check JWT secret
5. Check request headers

## Troubleshooting

If you encounter authentication errors:

1. Check that your credentials are correct
2. Ensure that the authentication providers are properly configured in the API
3. Check that your JWT token hasn't expired
4. Try clearing your token and logging in again

For more information, see the [Authentication Documentation](./authentication.md).