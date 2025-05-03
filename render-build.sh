#!/usr/bin/env bash
# Exit on error
set -e

# Print commands before executing
set -x

# Build the project
dotnet restore
dotnet build --configuration Release --no-restore
dotnet publish CmdShiftLearn.Api/CmdShiftLearn.Api.csproj -c Release -o publish --no-build

# Print the contents of the publish directory to verify static files are included
echo "Contents of publish directory:"
find publish -type f | grep -v "\.dll\|\.exe\|\.pdb\|\.json" | sort

# Print the contents of the wwwroot directory in the publish output
echo "Contents of wwwroot directory in publish output:"
find publish/wwwroot -type f | sort

# Make the script executable
chmod +x publish/CmdShiftLearn.Api