<<<<<<< HEAD
# Use the official .NET 9.0 runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 9.0.303 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0.303 AS build
WORKDIR /src

# Copy the project file and restore dependencies
=======
# Build Stage
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0.303 AS build

WORKDIR /src

# Copy solution & props if they exist (optional for cache optimization)
COPY Stockly.sln .
COPY *.props .

# Copy project file and restore dependencies
>>>>>>> d96b3c951fd3ee193f1d5cb3b71332f92e1e0e6c
COPY ["Stockly.csproj", "./"]
RUN dotnet restore "Stockly.csproj"

# Copy the rest of the source code
COPY . .

<<<<<<< HEAD
# Build the application
RUN dotnet build "Stockly.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Stockly.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the PORT environment variable
ENV PORT=8080
=======
# Build application
RUN dotnet build "Stockly.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Stockly.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage - Runtime only
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0.7 AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Non-root user (matches API)
USER 1000

# Environment variables
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
>>>>>>> d96b3c951fd3ee193f1d5cb3b71332f92e1e0e6c

# Expose the port
EXPOSE 8080

<<<<<<< HEAD
# Start the application
=======
# Start the app
>>>>>>> d96b3c951fd3ee193f1d5cb3b71332f92e1e0e6c
ENTRYPOINT ["dotnet", "Stockly.dll"]
