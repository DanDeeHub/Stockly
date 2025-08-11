# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /src

# Copy the project file first for better layer caching
COPY Stockly.csproj ./

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Set working directory
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080

# Expose port 8080
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "Stockly.dll"]
