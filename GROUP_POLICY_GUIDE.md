# Group Policy Deployment Guide

## Overview

This guide provides step-by-step instructions for deploying rdpWrapper via Group Policy Objects (GPO) in Active Directory environments. GPO deployment enables centralized, automated installation across multiple servers.

## Prerequisites

- Active Directory domain environment
- Domain Admin or GPO management privileges
- Network file share accessible by target computers
- SYSVOL or DFS share for script storage (optional, for best practices)

## Deployment Architecture

### Method 1: Startup Script (Recommended)

**Advantages:**
- Runs with SYSTEM privileges automatically
- Executes before user logon
- Simple to implement
- Reliable execution

**Disadvantages:**
- Only runs at system startup
- Requires reboot for initial deployment

### Method 2: Scheduled Task via GPO

**Advantages:**
- Can run immediately without reboot
- Can schedule periodic checks/reinstalls
- Flexible timing options

**Disadvantages:**
- More complex configuration
- Requires GPO Scheduled Tasks (Windows Server 2008+)

### Method 3: Software Installation

**Advantages:**
- Native GPO software deployment
- Automatic retry on failure

**Disadvantages:**
- Requires MSI package (rdpWrapper is EXE)
- More overhead than script-based deployment

**Recommendation:** Use Method 1 (Startup Script) for simplicity and reliability.

## Method 1: Startup Script Deployment

### Step 1: Prepare File Share

1. **Create network share:**
   ```powershell
   # On file server
   New-Item -Path "C:\Deploy" -ItemType Directory
   New-Item -Path "C:\Deploy\rdpWrapper" -ItemType Directory
   New-SmbShare -Name "Deploy" -Path "C:\Deploy" -ReadAccess "Domain Computers"
   ```

2. **Copy files to share:**
   ```powershell
   # Copy rdpWrapper.exe to share
   Copy-Item "rdpWrapper.exe" -Destination "\\fileserver\Deploy\rdpWrapper\"

   # Optional: Copy configuration profiles
   Copy-Item "profiles\*.json" -Destination "\\fileserver\Deploy\rdpWrapper\profiles\"
   ```

3. **Set permissions:**
   ```powershell
   # Grant read access to Domain Computers
   $acl = Get-Acl "C:\Deploy\rdpWrapper"
   $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
       "DOMAIN\Domain Computers", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
   )
   $acl.AddAccessRule($rule)
   Set-Acl "C:\Deploy\rdpWrapper" $acl
   ```

### Step 2: Create Deployment Script

**Create `Install-RdpWrapper.ps1`:**

```powershell
<#
.SYNOPSIS
    Installs rdpWrapper on target computer via GPO startup script
.DESCRIPTION
    This script checks if rdpWrapper is installed and installs/updates if needed.
    Designed to run as computer startup script with SYSTEM privileges.
.NOTES
    Author: Your Organization
    Version: 1.0
    Last Updated: 2025-01-11
#>

[CmdletBinding()]
param(
    [string]$SourcePath = "\\fileserver\Deploy\rdpWrapper",
    [string]$LogPath = "$env:SystemRoot\Logs\rdpWrapper-gpo-install.log",
    [string]$ProfilePath = "",  # Optional: "\\fileserver\Deploy\rdpWrapper\profiles\enterprise-default.json"
    [switch]$ForceReinstall
)

# Function to write log
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Add-Content -Path $LogPath -Value $logMessage -Force
    Write-Host $logMessage
}

# Function to check if rdpWrapper is installed
function Test-RdpWrapperInstalled {
    try {
        $serviceDll = Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll -ErrorAction SilentlyContinue
        if ($serviceDll -and ($serviceDll.ServiceDll -like "*TermWrap.dll*" -or $serviceDll.ServiceDll -like "*rdpwrap.dll*")) {
            return $true
        }
        return $false
    }
    catch {
        return $false
    }
}

# Function to get installed version
function Get-RdpWrapperVersion {
    $wrapperExe = "$env:SystemRoot\System32\rdpWrapper.exe"
    if (Test-Path $wrapperExe) {
        try {
            $version = (Get-Item $wrapperExe).VersionInfo.FileVersion
            return $version
        }
        catch {
            return "Unknown"
        }
    }
    return "Not Found"
}

# Main execution
try {
    Write-Log "=== rdpWrapper GPO Installation Script Started ==="
    Write-Log "Computer: $env:COMPUTERNAME"
    Write-Log "Source Path: $SourcePath"

    # Check if source is accessible
    if (-not (Test-Path "$SourcePath\rdpWrapper.exe")) {
        Write-Log "ERROR: Source file not found at $SourcePath\rdpWrapper.exe"
        exit 1
    }

    # Check if already installed
    $isInstalled = Test-RdpWrapperInstalled
    $currentVersion = Get-RdpWrapperVersion

    Write-Log "Current Installation Status: $($isInstalled ? 'Installed' : 'Not Installed')"
    Write-Log "Current Version: $currentVersion"

    # Determine if installation is needed
    $needsInstall = $false

    if ($ForceReinstall) {
        Write-Log "Force reinstall flag detected"
        $needsInstall = $true
    }
    elseif (-not $isInstalled) {
        Write-Log "rdpWrapper is not installed, proceeding with installation"
        $needsInstall = $true
    }
    else {
        Write-Log "rdpWrapper is already installed, skipping installation"
    }

    if (-not $needsInstall) {
        Write-Log "=== No installation needed, exiting ==="
        exit 0
    }

    # Copy rdpWrapper.exe to local system
    Write-Log "Copying rdpWrapper.exe to system..."
    $localPath = "$env:SystemRoot\System32\rdpWrapper.exe"
    Copy-Item -Path "$SourcePath\rdpWrapper.exe" -Destination $localPath -Force

    # Build installation command
    $installArgs = @("--auto-install", "--silent", "--offline", "--log=`"$LogPath`"")

    # Add profile if specified
    if ($ProfilePath -and (Test-Path $ProfilePath)) {
        Write-Log "Using configuration profile: $ProfilePath"
        $installArgs += "--profile=`"$ProfilePath`""
    }

    # Execute installation
    Write-Log "Executing installation command..."
    Write-Log "Command: $localPath $($installArgs -join ' ')"

    $process = Start-Process -FilePath $localPath -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
    $exitCode = $process.ExitCode

    Write-Log "Installation completed with exit code: $exitCode"

    # Check exit code
    switch ($exitCode) {
        0 { Write-Log "SUCCESS: Installation completed successfully" }
        1 { Write-Log "ERROR: Invalid arguments" }
        2 { Write-Log "ERROR: Installation failed" }
        3 { Write-Log "ERROR: Service error" }
        4 { Write-Log "ERROR: Configuration error" }
        5 { Write-Log "ERROR: Insufficient permissions" }
        default { Write-Log "ERROR: Unexpected exit code $exitCode" }
    }

    # Verify installation
    Start-Sleep -Seconds 2
    $isInstalledNow = Test-RdpWrapperInstalled
    Write-Log "Post-installation check: $($isInstalledNow ? 'Installed' : 'Not Installed')"

    # Check service status
    $service = Get-Service -Name TermService -ErrorAction SilentlyContinue
    if ($service) {
        Write-Log "TermService Status: $($service.Status)"
    }

    Write-Log "=== rdpWrapper GPO Installation Script Completed ==="
    exit $exitCode
}
catch {
    Write-Log "EXCEPTION: $($_.Exception.Message)"
    Write-Log "Stack Trace: $($_.ScriptStackTrace)"
    exit 99
}
```

**Save script to:**
- `\\fileserver\Deploy\rdpWrapper\Install-RdpWrapper.ps1`

**Or use SYSVOL (recommended for production):**
- `\\domain.com\SYSVOL\domain.com\scripts\Install-RdpWrapper.ps1`

### Step 3: Create Group Policy Object

1. **Open Group Policy Management Console:**
   ```
   gpmc.msc
   ```

2. **Create new GPO:**
   - Right-click on the OU containing target servers
   - Select "Create a GPO in this domain, and Link it here..."
   - Name: "Deploy rdpWrapper"

3. **Edit GPO:**
   - Right-click the new GPO
   - Select "Edit"

4. **Configure Startup Script:**
   - Navigate to: `Computer Configuration > Policies > Windows Settings > Scripts (Startup/Shutdown)`
   - Double-click "Startup"
   - Click "Add"
   - Script Name: `\\fileserver\Deploy\rdpWrapper\Install-RdpWrapper.ps1`
   - Script Parameters: (leave blank for defaults)
   - Click "OK"

5. **Configure GPO Security Filtering (Optional):**
   - In GPMC, select the GPO
   - Under "Security Filtering", remove "Authenticated Users" if present
   - Add specific computer groups or OUs that should receive rdpWrapper

6. **Configure WMI Filtering (Optional):**
   - Create WMI filter to target specific OS versions:
   ```sql
   SELECT * FROM Win32_OperatingSystem WHERE Version LIKE "10.%" AND ProductType = "3"
   ```
   - This targets Windows Server 2016+ only

### Step 4: Test Deployment

1. **Link GPO to test OU:**
   - Create test OU with single server
   - Link "Deploy rdpWrapper" GPO to test OU

2. **Force GPO update on test server:**
   ```powershell
   gpupdate /force
   ```

3. **Reboot test server:**
   ```powershell
   Restart-Computer -Force
   ```

4. **Verify installation:**
   ```powershell
   # Check log file
   Get-Content "C:\Windows\Logs\rdpWrapper-gpo-install.log"

   # Check installation status
   C:\Windows\System32\rdpWrapper.exe --status

   # Verify service
   Get-Service -Name TermService
   ```

5. **Test RDP connection with multiple concurrent sessions**

### Step 5: Production Rollout

1. **Staged rollout (recommended):**
   - Week 1: Link GPO to 5-10% of servers
   - Week 2: Monitor logs and expand to 25%
   - Week 3: Expand to 50%
   - Week 4: Full deployment

2. **Or immediate rollout:**
   - Link GPO to production OU
   - Coordinate server reboots during maintenance window

## Method 2: Scheduled Task Deployment

### Create GPO Scheduled Task

1. **Open GPO Editor**
2. **Navigate to:**
   - `Computer Configuration > Preferences > Control Panel Settings > Scheduled Tasks`

3. **Create new Scheduled Task:**
   - Right-click > New > Scheduled Task (Windows Vista and later)

4. **General Tab:**
   - Action: Create
   - Name: Install rdpWrapper
   - User account: SYSTEM
   - Run whether user is logged on or not: Checked
   - Run with highest privileges: Checked

5. **Triggers Tab:**
   - New Trigger
   - Begin: At startup
   - Delay task for: 1 minute
   - Enabled: Checked

6. **Actions Tab:**
   - New Action
   - Action: Start a program
   - Program: `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
   - Arguments: `-ExecutionPolicy Bypass -File "\\fileserver\Deploy\rdpWrapper\Install-RdpWrapper.ps1"`

7. **Conditions Tab:**
   - Uncheck "Start only if on AC power"

8. **Settings Tab:**
   - Allow task to be run on demand: Checked
   - If task is already running: Do not start new instance

### Immediate Execution (No Reboot Required)

To trigger installation immediately without reboot:

1. **Modify Trigger:**
   - Begin: At task creation/modification
   - Enabled: Checked

2. **Force GPO update:**
   ```powershell
   gpupdate /force
   ```

3. **Task runs automatically within 1-2 minutes**

## Centralized Logging

### Configure Central Log Collection

**Create log collection script `Collect-RdpWrapperLogs.ps1`:**

```powershell
<#
.SYNOPSIS
    Collects rdpWrapper installation logs from all servers
.DESCRIPTION
    Queries all computers in specified OU and copies their logs to central location
#>

param(
    [string]$SearchBase = "OU=Servers,DC=domain,DC=com",
    [string]$LogDestination = "\\fileserver\Logs\rdpWrapper",
    [string]$LogSourcePath = "C$\Windows\Logs\rdpWrapper-gpo-install.log"
)

# Get all computers in OU
$computers = Get-ADComputer -SearchBase $SearchBase -Filter * | Select-Object -ExpandProperty Name

# Create destination folder
New-Item -Path $LogDestination -ItemType Directory -Force | Out-Null

# Collect logs
foreach ($computer in $computers) {
    try {
        $sourcePath = "\\$computer\$LogSourcePath"
        $destPath = Join-Path $LogDestination "$computer.log"

        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $destPath -Force
            Write-Host "Collected log from $computer"
        }
        else {
            Write-Host "No log found on $computer" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Failed to collect log from $computer : $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nLog collection complete. Logs saved to: $LogDestination"
```

**Run centralized log collection:**
```powershell
.\Collect-RdpWrapperLogs.ps1 -SearchBase "OU=Servers,DC=domain,DC=com" -LogDestination "\\fileserver\Logs\rdpWrapper"
```

### Analyze Installation Success Rate

**Create analysis script `Analyze-RdpWrapperLogs.ps1`:**

```powershell
param(
    [string]$LogDirectory = "\\fileserver\Logs\rdpWrapper"
)

$logs = Get-ChildItem -Path $LogDirectory -Filter "*.log"
$total = $logs.Count
$successful = 0
$failed = 0
$errors = @()

foreach ($log in $logs) {
    $content = Get-Content $log.FullName -Raw

    if ($content -match "SUCCESS: Installation completed successfully") {
        $successful++
    }
    elseif ($content -match "ERROR:") {
        $failed++
        $computerName = $log.BaseName
        $errorMatch = $content | Select-String -Pattern "ERROR: (.*)" -AllMatches
        if ($errorMatch) {
            $errors += [PSCustomObject]@{
                Computer = $computerName
                Error = $errorMatch.Matches[0].Groups[1].Value
            }
        }
    }
}

Write-Host "`n=== rdpWrapper Deployment Summary ===" -ForegroundColor Cyan
Write-Host "Total Servers: $total"
Write-Host "Successful: $successful ($([math]::Round($successful/$total*100,2))%)" -ForegroundColor Green
Write-Host "Failed: $failed ($([math]::Round($failed/$total*100,2))%)" -ForegroundColor Red

if ($errors.Count -gt 0) {
    Write-Host "`n=== Failed Installations ===" -ForegroundColor Red
    $errors | Format-Table -AutoSize
}
```

## Rollback Procedure

### Create Uninstall GPO

1. **Create new GPO:** "Uninstall rdpWrapper"

2. **Create uninstall script `Uninstall-RdpWrapper.ps1`:**

```powershell
param(
    [string]$LogPath = "$env:SystemRoot\Logs\rdpWrapper-gpo-uninstall.log"
)

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $LogPath -Value "[$timestamp] $Message" -Force
}

try {
    Write-Log "=== rdpWrapper Uninstall Started ==="

    $wrapperExe = "$env:SystemRoot\System32\rdpWrapper.exe"

    if (Test-Path $wrapperExe) {
        Write-Log "Executing uninstall..."
        $process = Start-Process -FilePath $wrapperExe -ArgumentList "--uninstall", "--silent" -Wait -PassThru -NoNewWindow
        Write-Log "Uninstall completed with exit code: $($process.ExitCode)"

        # Remove executable
        Remove-Item -Path $wrapperExe -Force -ErrorAction SilentlyContinue
        Write-Log "Removed rdpWrapper.exe"
    }
    else {
        Write-Log "rdpWrapper.exe not found, nothing to uninstall"
    }

    Write-Log "=== rdpWrapper Uninstall Completed ==="
    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
```

3. **Link uninstall GPO to target OU**
4. **Servers will uninstall on next reboot**

## Advanced Configurations

### Profile-Based Deployment for Different Server Types

**Create multiple deployment scripts for different profiles:**

**Web Servers:**
```powershell
# Install-RdpWrapper-WebServers.ps1
$ProfilePath = "\\fileserver\Deploy\rdpWrapper\profiles\web-servers.json"
# ... rest of script with $ProfilePath
```

**Database Servers:**
```powershell
# Install-RdpWrapper-DBServers.ps1
$ProfilePath = "\\fileserver\Deploy\rdpWrapper\profiles\db-servers-highsec.json"
# ... rest of script with $ProfilePath
```

**Assign different GPOs to different OUs based on server role.**

### Conditional Installation Based on Criteria

**Modify script to check conditions:**

```powershell
# Only install on servers with specific Windows version
$os = Get-WmiObject -Class Win32_OperatingSystem
if ($os.Version -notlike "10.*") {
    Write-Log "Unsupported OS version: $($os.Version)"
    exit 0
}

# Only install on servers with specific role
$roles = Get-WindowsFeature | Where-Object { $_.Installed -eq $true }
if ($roles.Name -notcontains "Web-Server") {
    Write-Log "Web-Server role not found, skipping installation"
    exit 0
}

# Continue with installation...
```

## Monitoring and Reporting

### Create Installation Status Report

**PowerShell script to check installation status across domain:**

```powershell
param(
    [string]$SearchBase = "OU=Servers,DC=domain,DC=com",
    [string]$ReportPath = "C:\Reports\rdpWrapper-status.html"
)

Import-Module ActiveDirectory

$computers = Get-ADComputer -SearchBase $SearchBase -Filter { OperatingSystem -like "*Server*" }
$results = @()

foreach ($computer in $computers) {
    $computerName = $computer.Name

    try {
        $online = Test-Connection -ComputerName $computerName -Count 1 -Quiet

        if ($online) {
            $serviceDll = Invoke-Command -ComputerName $computerName -ScriptBlock {
                Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll -ErrorAction SilentlyContinue
            }

            $isInstalled = $serviceDll -and ($serviceDll.ServiceDll -like "*TermWrap.dll*" -or $serviceDll.ServiceDll -like "*rdpwrap.dll*")

            $results += [PSCustomObject]@{
                ComputerName = $computerName
                Online = "Yes"
                Installed = if ($isInstalled) { "Yes" } else { "No" }
                ServiceDll = $serviceDll.ServiceDll
            }
        }
        else {
            $results += [PSCustomObject]@{
                ComputerName = $computerName
                Online = "No"
                Installed = "Unknown"
                ServiceDll = "N/A"
            }
        }
    }
    catch {
        $results += [PSCustomObject]@{
            ComputerName = $computerName
            Online = "Error"
            Installed = "Error"
            ServiceDll = $_.Exception.Message
        }
    }
}

# Generate HTML report
$html = $results | ConvertTo-Html -Title "rdpWrapper Installation Status" -PreContent "<h1>rdpWrapper Installation Status</h1><p>Generated: $(Get-Date)</p>"
$html | Out-File -FilePath $ReportPath -Encoding UTF8

Write-Host "Report generated: $ReportPath"
```

## Troubleshooting GPO Deployment

### Common Issues

1. **Script doesn't execute:**
   - Check: `gpresult /h gpresult.html` on target server
   - Verify GPO is linked and applied
   - Check security filtering and WMI filters
   - Verify file share permissions (Domain Computers must have Read access)

2. **Access denied errors:**
   - Ensure script runs as SYSTEM (not user context)
   - Verify computer account has Read access to share
   - Check ExecutionPolicy: `Set-ExecutionPolicy RemoteSigned` in GPO

3. **Script runs but installation fails:**
   - Check log file: `C:\Windows\Logs\rdpWrapper-gpo-install.log`
   - Verify rdpWrapper.exe is accessible from share
   - Check exit codes in log

4. **GPO doesn't apply:**
   - Check OU linking: `Get-GPInheritance -Target "OU=Servers,DC=domain,DC=com"`
   - Check GPO status: Ensure GPO is enabled
   - Force update: `gpupdate /force /boot` and reboot

### Enable GPO Logging

**Enable script execution logging:**

1. Edit GPO
2. Navigate to: `Computer Configuration > Administrative Templates > System > Scripts`
3. Enable "Display instructions in Windows PowerShell scripts as they execute"
4. Enable "Allow Windows PowerShell scripts to run first at user logon, logoff"

**Check GPO application logs:**
```powershell
Get-WinEvent -LogName "Microsoft-Windows-GroupPolicy/Operational" | Where-Object { $_.Id -eq 4016 -or $_.Id -eq 5016 }
```

## Best Practices

1. **Test thoroughly in lab environment** before production deployment
2. **Use staged rollout** - deploy to small groups first
3. **Store scripts in SYSVOL** for reliability (or replicated DFS share)
4. **Implement centralized logging** for deployment tracking
5. **Create rollback procedure** before deployment
6. **Document GPO configuration** for future reference
7. **Monitor installation success rates** and investigate failures
8. **Use WMI filters** to target specific OS versions/editions
9. **Implement error notification** (email alerts on failures)
10. **Schedule periodic verification** to ensure configuration persistence

## Support

For additional help, see:
- [DEPLOYMENT.md](DEPLOYMENT.md) - General deployment guide
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Troubleshooting guide
- [README.md](README.md) - CLI reference
