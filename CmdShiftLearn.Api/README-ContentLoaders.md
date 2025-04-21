# CmdShiftLearn Content Loaders

CmdShiftLearn supports loading tutorials and challenges from multiple sources, making the application fully scalable and flexible for future growth.

## Available Content Sources

The application supports the following content sources:

1. **File** - Load content from local files (default, great for development)
2. **GitHub** - Load content from a GitHub repository
3. **Supabase** - Load content from Supabase tables

## Configuration

Content sources are configured in the `appsettings.json` file:

```json
"ContentSources": {
  "Tutorials": {
    "Source": "File",
    "Directory": "scripts/tutorials",
    "TableName": "tutorials"
  },
  "Challenges": {
    "Source": "File",
    "Directory": "scripts/challenges",
    "TableName": "challenges"
  }
}
```

### File Source Configuration

For the File source, you can specify the directory where the content files are stored:

```json
"ContentSources": {
  "Tutorials": {
    "Source": "File",
    "Directory": "scripts/tutorials"
  },
  "Challenges": {
    "Source": "File",
    "Directory": "scripts/challenges"
  }
}
```

### GitHub Source Configuration

For the GitHub source, you need to configure the GitHub repository details:

```json
"GitHub": {
  "Owner": "deactv8",
  "Repo": "content",
  "Branch": "main",
  "AccessToken": "",
  "TutorialsPath": "tutorials",
  "ChallengesPath": "challenges"
},
"ContentSources": {
  "Tutorials": {
    "Source": "GitHub"
  },
  "Challenges": {
    "Source": "GitHub"
  }
}
```

### Supabase Source Configuration

For the Supabase source, you need to configure the Supabase connection details:

```json
"Supabase": {
  "Url": "https://your-supabase-url.supabase.co",
  "ApiKey": "your-supabase-api-key",
  "JwtSecret": "your-supabase-jwt-secret"
},
"ContentSources": {
  "Tutorials": {
    "Source": "Supabase",
    "TableName": "tutorials"
  },
  "Challenges": {
    "Source": "Supabase",
    "TableName": "challenges"
  }
}
```

## Switching Between Sources

To switch between content sources, simply update the `Source` value in the `ContentSources` section of the `appsettings.json` file. The application will automatically use the appropriate loader based on the configuration.

## Content Format

Regardless of the source, the content format remains the same:

### Tutorials

Tutorials can be stored as JSON or YAML files with the following structure:

```json
{
  "id": "tutorial-id",
  "title": "Tutorial Title",
  "description": "Tutorial description",
  "content": "Tutorial content or path to content file",
  "xp": 100,
  "difficulty": "beginner"
}
```

### Challenges

Challenges can be stored as JSON or YAML files with the following structure:

```json
{
  "id": "challenge-id",
  "title": "Challenge Title",
  "description": "Challenge description",
  "script": "Challenge script or path to script file",
  "xp": 50,
  "difficulty": "beginner",
  "tutorialId": "associated-tutorial-id"
}
```

## Benefits of Multiple Content Sources

1. **Development Mode**: Use the File source during development for quick testing and iteration.
2. **Remote Content**: Use GitHub or Supabase for production to serve content remotely.
3. **Dynamic Updates**: Update content without redeploying the application.
4. **Community Contributions**: Accept community-submitted content through GitHub pull requests.
5. **AI-Generated Content**: Generate content programmatically and store it in Supabase.

## Future Enhancements

- Add support for more content sources (e.g., Azure Blob Storage, AWS S3)
- Implement content caching to improve performance
- Add content versioning and rollback capabilities