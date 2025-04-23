FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["CmdShiftLearn.Api.csproj", "CmdShiftLearn.Api.csproj"]
RUN dotnet restore "CmdShiftLearn.Api.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "CmdShiftLearn.Api.csproj" -c Release -o /app/build
RUN dotnet publish "CmdShiftLearn.Api.csproj" -c Release -o /app/publish

# Debug: List the contents of the publish directory
RUN find /app/publish -type f | grep -v "\.dll\|\.exe\|\.pdb\|\.json" | sort
RUN find /app/publish/wwwroot -type f | sort || echo "wwwroot directory not found in publish output"

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create a non-root user to run the app
RUN adduser --disabled-password --gecos "" appuser
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "CmdShiftLearn.Api.dll"]