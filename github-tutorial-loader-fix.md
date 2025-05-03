# GitHub Tutorial Loader Fix

## Issue
The `/api/tutorials` endpoint was returning an empty array (`[]`), even though the GitHub repository at https://github.com/deactv8/content/tree/master/tutorials contains multiple `.yaml`, `.yml`, and `.json` tutorial files.

## Root Cause
1. **Incorrect Branch Configuration**: The application was configured to use the "main" branch, but the repository uses "master" branch.
2. **Insufficient Logging**: The existing code didn't provide enough logging to diagnose issues with GitHub API calls.
3. **Error Handling**: The error handling wasn't specific enough to identify the exact cause of failures.

## Changes Made

### 1. Configuration Updates
- Updated the default branch from "main" to "master" in both `appsettings.json` and `appsettings.Development.json`
- Updated the `GitHubTutorialLoader` constructor to default to "master" branch

### 2. Enhanced Logging
- Added detailed logging throughout the GitHub tutorial loading process
- Added logging for file types found in the repository
- Added logging for successful and failed deserialization attempts
- Added logging for steps count in tutorials
- Added logging in the TutorialService and TutorialsController

### 3. Improved Error Handling
- Added specific exception handling for different GitHub API errors:
  - NotFoundException
  - RateLimitExceededException
  - AuthorizationException
  - ApiException
- Added more context to error messages

### 4. Debug Endpoint
- Added a `/debug/github-config` endpoint to check the GitHub configuration

## Files Modified
1. `CmdShiftLearn.Api/Services/GitHubTutorialLoader.cs`
2. `CmdShiftLearn.Api/Services/TutorialService.cs`
3. `CmdShiftLearn.Api/Controllers/TutorialsController.cs`
4. `CmdShiftLearn.Api/Program.cs`
5. `CmdShiftLearn.Api/appsettings.json`
6. `CmdShiftLearn.Api/appsettings.Development.json`

## Testing
After applying these changes, the `/api/tutorials` endpoint should return the expected list of tutorials from the GitHub repository, and `/api/tutorials/{id}` should return a populated steps array for tutorials that have steps defined.

## Validation
To validate the fix:
1. Start the API
2. Access `/api/tutorials` - should return a list of tutorials
3. Access `/api/tutorials/{id}` for a tutorial with steps - should include the steps array
4. Check the logs for detailed information about the GitHub API calls and tutorial loading process
5. Access `/debug/github-config` to verify the GitHub configuration

## Additional Notes
- The fix ensures that all supported tutorial file formats (`.yaml`, `.yml`, `.json`) are correctly detected and loaded
- The YAML files with steps are now parsed correctly using the YamlHelpers class
- Detailed logging has been added to help diagnose any future issues