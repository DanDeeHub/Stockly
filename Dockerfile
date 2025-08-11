# Build Stage
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0.303 AS build

WORKDIR /src

# Copy solution & props if they exist (optional for cache optimization)
COPY Stockly.sln .
COPY *.props .

# Copy project file and restore dependencies
COPY ["Stockly.csproj", "./"]
RUN dotnet restore "Stockly.csproj"

# Copy the rest of the source code
COPY . .

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

# Expose the port
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "Stockly.dll"]
