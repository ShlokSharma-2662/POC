# IIS Deployment Guide for Ecommerce API

## Problem: net::ERR_NETWORK_IO_SUSPENDED Error

This error occurs when the checkout API works on localhost but fails on IIS server due to timeout issues.

## Solutions Implemented

### 1. Web.config Configuration
- Created `web.config` with proper timeout settings
- Set execution timeout to 5 minutes
- Configured request limits and compression

### 2. Application Settings
- Updated `appsettings.Production.json` with timeout configurations
- Added database connection timeout settings
- Enhanced connection string with timeout parameters

### 3. Request Timeout Middleware
- Created `RequestTimeoutMiddleware.cs` to handle long-running requests
- Configurable timeout settings from appsettings
- Proper error responses for timeout scenarios

### 4. Enhanced Logging
- Added detailed logging to track checkout process timing
- Request ID tracking for better debugging
- Performance metrics logging

## Deployment Steps

### 1. Build the Application
```bash
dotnet publish -c Release -o ./publish
```

### 2. Deploy to IIS
1. Copy the published files to `C:\inetpub\wwwroot\EcommerceAPI`
2. Ensure `web.config` is in the root directory
3. Set proper permissions for the application pool identity

### 3. Configure Application Pool
Use the PowerShell commands in `IIS_ApplicationPool_Configuration.md`:

```powershell
# Import IIS module
Import-Module WebAdministration

# Create and configure application pool
New-WebAppPool -Name "EcommerceAPI_AppPool"
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "processModel.idleTimeout" -Value "00:20:00"
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "recycling.periodicRestart.time" -Value "01:09:00"

# Create website
New-Website -Name "EcommerceAPI" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\EcommerceAPI" -ApplicationPool "EcommerceAPI_AppPool"
```

### 4. Environment Configuration
- Set `ASPNETCORE_ENVIRONMENT=Production`
- Ensure database connection string is correct
- Verify all required services are running

## Troubleshooting

### Check Logs
1. **IIS Logs**: `C:\inetpub\logs\LogFiles\`
2. **Application Logs**: Check Serilog configuration
3. **Event Viewer**: Windows Logs > Application

### Common Issues
1. **Database Connection**: Verify SQL Server connectivity
2. **Permissions**: Ensure application pool has proper permissions
3. **Firewall**: Check if ports are open
4. **Rate Limiting**: Disable in development, configure properly in production

### Performance Monitoring
- Monitor application pool CPU and memory usage
- Check database connection pool settings
- Review request timing in logs

## Testing the Fix

1. **Local Test**: Verify the API works on localhost
2. **IIS Test**: Deploy and test on IIS server
3. **Load Test**: Test with multiple concurrent requests
4. **Timeout Test**: Test with large orders that might take longer

## Additional Recommendations

1. **Database Optimization**: Ensure database queries are optimized
2. **Connection Pooling**: Configure proper connection pool settings
3. **Caching**: Implement caching for frequently accessed data
4. **Monitoring**: Set up application performance monitoring

