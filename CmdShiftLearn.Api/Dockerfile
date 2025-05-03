FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["CmdShiftLearn.Api.csproj", "CmdShiftLearn.Api.csproj"]
COPY ["global.json", "global.json"]

# Force SDK to ignore version constraints
ENV DOTNET_ROLL_FORWARD=Major

# Restore and build
RUN dotnet restore "CmdShiftLearn.Api.csproj" --disable-parallel
COPY . .
RUN dotnet publish "CmdShiftLearn.Api.csproj" -c Release -o /app/publish --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
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