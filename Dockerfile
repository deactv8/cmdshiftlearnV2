FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY CmdShiftLearn.Api/*.csproj ./CmdShiftLearn.Api/
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish CmdShiftLearn.Api/CmdShiftLearn.Api.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port 80
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the app
ENTRYPOINT ["dotnet", "CmdShiftLearn.Api.dll"]