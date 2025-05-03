# YAML Tutorial Steps Fix

## Issue
The GitHubTutorialLoader was not properly loading the `steps` field from interactive YAML tutorials. When fetching `/api/tutorials/{id}`, it always returned `steps: []` even when steps were defined in the YAML content.

## Root Cause
The issue was in the YAML deserialization process. The YamlDotNet deserializer was not correctly mapping the YAML `steps` array to the `Steps` property in the `Tutorial` class. This is likely due to how YamlDotNet handles complex nested objects and arrays.

## Solution
We implemented a robust solution with several components:

1. **Created a YamlHelpers class** with a specialized `DeserializeTutorial` method that:
   - First attempts standard deserialization
   - If steps are missing, falls back to manual extraction by:
     - Deserializing to a Dictionary<string, object>
     - Extracting the steps object
     - Converting to JSON and then deserializing to List<TutorialStep>

2. **Updated all tutorial loaders** to use this helper method:
   - GitHubTutorialLoader
   - FileTutorialLoader
   - SupabaseTutorialLoader

3. **Added detailed logging** to help diagnose any future issues

4. **Created a unit test** to verify the fix works correctly

## Files Modified
- `CmdShiftLearn.Api/Services/GitHubTutorialLoader.cs`
- `CmdShiftLearn.Api/Services/FileTutorialLoader.cs`
- `CmdShiftLearn.Api/Services/SupabaseTutorialLoader.cs`

## Files Created
- `CmdShiftLearn.Api/Helpers/YamlHelpers.cs`
- `CmdShiftLearn.Tests/YamlHelpersTests.cs`
- `CmdShiftLearn.Api/scripts/tutorials/powershell-interactive-yaml.yaml` (test file)

## Testing
The fix was tested with a sample YAML tutorial that includes steps. The YamlHelpers class successfully extracts and deserializes the steps, ensuring they are properly included in the Tutorial object returned by the API.

## Benefits
- More robust YAML parsing
- Consistent behavior across all tutorial loaders
- Better error handling and logging
- Easier maintenance with centralized YAML handling logic