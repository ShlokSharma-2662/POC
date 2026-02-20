# IIS Application Pool Configuration for Ecommerce API

## Application Pool Settings

### 1. Basic Settings
- **Name**: EcommerceAPI_AppPool
- **.NET CLR Version**: No Managed Code (for ASP.NET Core)
- **Managed Pipeline Mode**: Integrated
- **Process Model Identity**: ApplicationPoolIdentity

### 2. Process Model Settings
- **Idle Time-out**: 20 minutes (1200 seconds)
- **Maximum Worker Processes**: 1
- **Start Mode**: AlwaysRunning

### 3. Advanced Settings
- **Enable 32-Bit Applications**: False
- **Queue Length**: 1000
- **CPU Limit**: 0 (unlimited)
- **Memory Limit**: 0 (unlimited)

### 4. Recycling Settings
- **Regular Time Interval**: 1740 minutes (29 hours)
- **Private Memory Limit**: 0 (unlimited)
- **Virtual Memory Limit**: 0 (unlimited)
- **Request Limit**: 0 (unlimited)

## PowerShell Commands to Configure

```powershell
# Import IIS module
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "EcommerceAPI_AppPool"

# Configure application pool settings
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "processModel.idleTimeout" -Value "00:20:00"
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "processModel.maxProcesses" -Value 1
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "processModel.loadUserProfile" -Value $true
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "recycling.periodicRestart.time" -Value "01:09:00"
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "recycling.periodicRestart.privateMemory" -Value 0
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "recycling.periodicRestart.memory" -Value 0
Set-ItemProperty -Path "IIS:\AppPools\EcommerceAPI_AppPool" -Name "recycling.periodicRestart.requests" -Value 0

# Start the application pool
Start-WebAppPool -Name "EcommerceAPI_AppPool"
```

## Website Configuration

```powershell
# Create website
New-Website -Name "EcommerceAPI" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\EcommerceAPI" -ApplicationPool "EcommerceAPI_AppPool"

# Configure website settings
Set-ItemProperty -Path "IIS:\Sites\EcommerceAPI" -Name "limits.connectionTimeout" -Value "00:05:00"
Set-ItemProperty -Path "IIS:\Sites\EcommerceAPI" -Name "limits.headerWaitTimeout" -Value "00:01:00"
```

## Additional IIS Settings

### 1. Enable Detailed Error Messages
```xml
<system.webServer>
  <httpErrors errorMode="Detailed" />
</system.webServer>
```

### 2. Increase Request Limits
```xml
<system.webServer>
  <security>
    <requestFiltering>
      <requestLimits maxAllowedContentLength="52428800" maxQueryString="2048" />
    </requestFiltering>
  </security>
</system.webServer>
```

### 3. Configure Compression
```xml
<system.webServer>
  <httpCompression>
    <dynamicTypes>
      <add mimeType="application/json" enabled="true" />
    </dynamicTypes>
  </httpCompression>
</system.webServer>
```

