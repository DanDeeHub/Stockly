# Firebase Cloud Run Deployment Script for Stockly
# Make sure you have Firebase CLI installed: npm install -g firebase-tools

Write-Host "Starting Firebase deployment for Stockly..." -ForegroundColor Green

# Check if Firebase CLI is installed
try {
    firebase --version
} catch {
    Write-Host "Firebase CLI not found. Please install it with: npm install -g firebase-tools" -ForegroundColor Red
    exit 1
}

# Build the application
Write-Host "Building the application..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

# Deploy to Firebase Cloud Run
Write-Host "Deploying to Firebase Cloud Run..." -ForegroundColor Yellow
firebase deploy --only run

Write-Host "Deployment completed!" -ForegroundColor Green
Write-Host "Your application should be available at the URL provided by Firebase." -ForegroundColor Cyan
