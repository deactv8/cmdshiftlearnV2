# Deploying CmdShiftLearn.Api to Render

This guide provides step-by-step instructions for deploying the CmdShiftLearn.Api project to Render.

## Prerequisites

1. A Render account (https://render.com)
2. Git repository with your CmdShiftLearn.Api code
3. Supabase account with your project set up
4. GitHub access token (if using GitHub as a content source)

## Deployment Steps

### 1. Push Your Code to a Git Repository

Make sure your code is pushed to a Git repository (GitHub, GitLab, etc.) that Render can access.

### 2. Create a New Web Service on Render

1. Log in to your Render dashboard
2. Click "New" and select "Blueprint" (to use the render.yaml configuration)
3. Connect your Git repository
4. Select the repository containing your CmdShiftLearn.Api project
5. Render will detect the render.yaml file and configure the service accordingly

### 3. Configure Environment Variables

In the Render dashboard, set the following environment variables for your service:

- `ASPNETCORE_ENVIRONMENT`: Production
- `Supabase__Url`: Your Supabase project URL
- `Supabase__ApiKey`: Your Supabase API key
- `Supabase__JwtSecret`: Your Supabase JWT secret
- `GitHub__AccessToken`: Your GitHub access token (if using GitHub as a content source)
- `AllowedOrigins`: Comma-separated list of allowed origins for CORS (e.g., "https://yourdomain.com,https://app.yourdomain.com")

### 4. Deploy the Service

1. Click "Create Blueprint" to start the deployment process
2. Render will build and deploy your service according to the configuration in render.yaml
3. Once deployed, you can access your API at the provided Render URL (e.g., https://cmdshiftlearn-api.onrender.com)

### 5. Update CmdAgent Configuration

After deployment, update your CmdAgent configuration to point to the new API URL instead of localhost:

1. Find the configuration file for CmdAgent
2. Replace any references to `https://localhost:7001` with your new Render URL
3. Restart CmdAgent to apply the changes

## Troubleshooting

- **Health Check Failures**: Check the logs in the Render dashboard to see why the health check is failing
- **Authentication Issues**: Verify that your Supabase JWT secret is correctly set in the environment variables
- **CORS Errors**: Make sure the `AllowedOrigins` environment variable includes all domains that need to access the API

## Monitoring and Maintenance

- Use the Render dashboard to monitor your service's health, logs, and resource usage
- Set up alerts for service outages or high resource usage
- Consider upgrading your plan if you need more resources or better performance

## Scaling

If you need to scale your service:

1. Go to the service settings in the Render dashboard
2. Upgrade to a higher plan with more resources
3. Configure auto-scaling if needed (available on higher-tier plans)

## Backup and Disaster Recovery

- Regularly back up your Supabase database
- Consider setting up a staging environment for testing changes before deploying to production
- Document your deployment process and keep it up to date