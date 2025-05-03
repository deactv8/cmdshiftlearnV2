# Render Deployment Guide

This guide provides instructions for deploying the CmdShiftLearn.Api to Render.

## Prerequisites

- A Render account
- A GitHub account with access to the CmdShiftLearn repository

## Deployment Steps

1. Log in to your Render account
2. Click on "New" and select "Web Service"
3. Connect your GitHub repository
4. Configure the web service:
   - Name: cmdshiftlearnv2
   - Environment: Docker
   - Branch: main
   - Root Directory: (leave blank)
   - Docker Build Context: ./CmdShiftLearn.Api
   - Dockerfile Path: ./CmdShiftLearn.Api/Dockerfile
   - Health Check Path: /health
   - Plan: Starter (or higher)
5. Set the environment variables as specified in the render.yaml file
6. Click "Create Web Service"

## Troubleshooting

### Static Files Not Being Served

If static files (like auth-test.html) are not being served, check the following:

1. Ensure the wwwroot directory is included in the published output:
   - Check the .csproj file for the ItemGroup that includes the wwwroot directory
   - Check the Dockerfile to ensure the wwwroot directory is copied to the final image

2. Ensure the static files middleware is registered in Program.cs:
   ```csharp
   app.UseStaticFiles();
   ```

3. Check the logs for any errors related to static files

### 404 Errors

If you're getting 404 errors when accessing static files, check the following:

1. Ensure the file exists in the wwwroot directory
2. Ensure the file is being copied to the published output
3. Ensure the static files middleware is registered before other middleware
4. Check the logs for any errors related to routing or static files

## Verifying the Deployment

After deploying, you can verify the deployment by accessing the following URLs:

- Health check: https://cmdshiftlearnv2.onrender.com/health
- Authentication test page: https://cmdshiftlearnv2.onrender.com/auth-test.html
- API documentation: https://cmdshiftlearnv2.onrender.com/swagger

## Logs

You can view the logs for your web service in the Render dashboard. Look for any errors related to static files or routing.