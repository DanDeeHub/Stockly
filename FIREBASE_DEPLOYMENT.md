# Firebase Cloud Run Deployment Guide for Stockly

## Prerequisites

1. **Firebase CLI**: Install Firebase CLI globally
   ```bash
   npm install -g firebase-tools
   ```

2. **Google Cloud SDK**: Install Google Cloud SDK
   - Download from: https://cloud.google.com/sdk/docs/install
   - Or use: `gcloud init`

3. **Docker**: Ensure Docker is installed and running
   - Download from: https://www.docker.com/products/docker-desktop

## Setup Steps

### 1. Firebase Project Configuration

1. **Create/Select Firebase Project**:
   ```bash
   firebase login
   firebase projects:list
   firebase use your-project-id
   ```

2. **Update `.firebaserc`**:
   Replace `your-firebase-project-id` with your actual Firebase project ID.

### 2. Enable Required Services

1. **Enable Cloud Run API**:
   ```bash
   gcloud services enable run.googleapis.com
   ```

2. **Enable Container Registry**:
   ```bash
   gcloud services enable containerregistry.googleapis.com
   ```

### 3. Configure Firebase

1. **Initialize Firebase** (if not already done):
   ```bash
   firebase init
   ```
   - Select "Hosting" and "Cloud Run"
   - Choose your project
   - Use default settings

### 4. Environment Variables

If your app uses environment variables, configure them in Firebase:

```bash
firebase functions:config:set app.environment="production"
```

## Deployment

### Option 1: Using the Deployment Script

```powershell
.\deploy.ps1
```

### Option 2: Manual Deployment

1. **Build the application**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to Firebase Cloud Run**:
   ```bash
   firebase deploy --only run
   ```

### Option 3: Using gcloud directly

1. **Build and push the Docker image**:
   ```bash
   gcloud builds submit --tag gcr.io/YOUR_PROJECT_ID/stockly-db
   ```

2. **Deploy to Cloud Run**:
   ```bash
   gcloud run deploy stockly-db \
     --image gcr.io/YOUR_PROJECT_ID/stockly-db \
     --platform managed \
     --region us-central1 \
     --allow-unauthenticated \
     --port 8080
   ```

## Configuration Details

### Port Configuration
- The application is configured to listen on port 8080 (Firebase Cloud Run default)
- The PORT environment variable is automatically set by Firebase

### HTTPS Handling
- HTTPS redirection is disabled for Firebase Cloud Run
- Firebase handles SSL termination automatically

### Resource Allocation
- Memory: 512Mi (configurable in firebase.json)
- CPU: 1 vCPU
- Timeout: 300 seconds
- Max instances: 10
- Min instances: 0

## Troubleshooting

### Common Issues

1. **Container fails to start**:
   - Check logs: `firebase functions:log`
   - Ensure Dockerfile is correct
   - Verify port configuration

2. **Port binding issues**:
   - Ensure the app listens on `0.0.0.0:8080`
   - Check that PORT environment variable is used

3. **Build failures**:
   - Check .dockerignore file
   - Ensure all dependencies are included
   - Verify .NET version compatibility

### Logs and Monitoring

1. **View Cloud Run logs**:
   ```bash
   gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=stockly-db"
   ```

2. **Firebase logs**:
   ```bash
   firebase functions:log
   ```

## Security Considerations

1. **Environment Variables**: Store sensitive data in Firebase environment variables
2. **Authentication**: Configure appropriate authentication for your app
3. **CORS**: Configure CORS settings if needed
4. **Firebase Rules**: Set up proper Firestore security rules

## Cost Optimization

1. **Min Instances**: Set to 0 for cost savings (cold starts)
2. **Max Instances**: Limit based on expected traffic
3. **Memory/CPU**: Adjust based on actual usage
4. **Region**: Choose closest to your users

## Next Steps

1. Set up custom domain (optional)
2. Configure monitoring and alerts
3. Set up CI/CD pipeline
4. Configure backup strategies
5. Set up staging environment

## Support

- Firebase Documentation: https://firebase.google.com/docs
- Cloud Run Documentation: https://cloud.google.com/run/docs
- .NET on Cloud Run: https://cloud.google.com/run/docs/quickstarts/build-and-deploy/dotnet
