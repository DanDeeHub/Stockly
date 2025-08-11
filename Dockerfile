# Use the official .NET 9.0 runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 9.0.303 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0.303 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["Stockly.csproj", "./"]
RUN dotnet restore "Stockly.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "Stockly.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Stockly.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the PORT environment variable
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Expose the port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "Stockly.dll"]
