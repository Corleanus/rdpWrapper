# rdpWrapper - Complete Testing Guide

**Version**: 2.0 (CLI Version)
**Date**: 2025-11-11
**Branch**: `claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1`

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Step 1: Clone/Pull Latest Code](#step-1-clonepull-latest-code)
3. [Step 2: Decrypt Encrypted Resources](#step-2-decrypt-encrypted-resources)
4. [Step 3: Update rdpwrap.ini (Optional)](#step-3-optional---update-rdpwrapini)
5. [Step 4: Build the Project](#step-4-build-the-project)
6. [Step 5: Basic CLI Tests](#step-5-basic-cli-tests-no-installation-yet)
7. [Step 6: Installation Tests](#step-6-installation-tests)
8. [Step 7: Functional Tests (CRITICAL)](#step-7-functional-tests-core-feature)
9. [Step 8: Configuration Tests](#step-8-configuration-tests)
10. [Step 9: Service Control Tests](#step-9-service-control-tests)
11. [Step 10: Uninstall Tests](#step-10-uninstall-tests)
12. [Step 11: Edge Cases](#step-11-edge-cases-and-error-handling)
13. [Step 12: Stress Testing](#step-12-stress-testing-optional)
14. [Step 13: Performance Measurements](#step-13-performance-measurements)
15. [Expected Results Summary](#expected-results-summary)
16. [Troubleshooting](#troubleshooting)
17. [After Testing - Commit Changes](#after-testing---commit-changes)

---

## Prerequisites

### System Requirements
- **Windows Machine**: Windows 10/11 or Windows Server 2016/2019/2022
- **Administrator Rights**: Required for all testing
- **.NET 8 SDK**: Install if not present
- **Git**: For cloning/pulling the repository
- **Clean Test VM** (highly recommended): Avoid testing on production systems

### Install .NET 8 SDK

```powershell
# Check if already installed
dotnet --version

# If not installed, install via winget
winget install Microsoft.DotNet.SDK.8

# Or download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

---

## Step 1: Clone/Pull Latest Code

```powershell
# If you haven't cloned yet
git clone https://github.com/Corleanus/rdpWrapper.git
cd rdpWrapper
git checkout claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1

# If you already have it, pull latest changes
cd rdpWrapper
git pull origin claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1
```

---

## Step 2: Decrypt Encrypted Resources

**CRITICAL**: The project has encrypted `.cr` files that need decryption before building.

```powershell
# Navigate to project root
cd C:\path\to\rdpWrapper

# Run decryption script
.\DecryptResources.ps1
```

### Expected Output:
```
Decrypting encrypted resources...
✓ Decrypted: externals\rdpwrap.ini.cr -> rdpwrap.ini
✓ Decrypted: externals\x86\rdpwrap.dll.cr -> rdpwrap.dll
✓ Decrypted: externals\x86\TermWrap.dll.cr -> TermWrap.dll
✓ Decrypted: externals\x86\Zydis.dll.cr -> Zydis.dll
✓ Decrypted: externals\x86\RDPWrapOffsetFinder.exe.cr -> RDPWrapOffsetFinder.exe
✓ Decrypted: externals\x64\rdpwrap.dll.cr -> rdpwrap.dll
✓ Decrypted: externals\x64\TermWrap.dll.cr -> TermWrap.dll
✓ Decrypted: externals\x64\UmWrap.dll.cr -> UmWrap.dll
✓ Decrypted: externals\x64\EndpWrap.dll.cr -> EndpWrap.dll
✓ Decrypted: externals\x64\Zydis.dll.cr -> Zydis.dll
✓ Decrypted: externals\x64\RDPWrapOffsetFinder.exe.cr -> RDPWrapOffsetFinder.exe

Successfully decrypted 11 files
```

### Verify Decryption:
```powershell
# Check that plain files now exist
Get-ChildItem -Path .\rdpWrapper\externals -Recurse -File | Where-Object { $_.Extension -ne ".cr" }
```

---

## Step 3: Optional - Update rdpwrap.ini

Download the latest community-maintained version for better Windows compatibility:

```powershell
# Download latest rdpwrap.ini (updated 2025-11-08)
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/sebaxakerhtc/rdpwrap.ini/master/rdpwrap.ini" `
  -OutFile "rdpWrapper\externals\rdpwrap.ini"

Write-Host "✓ Updated rdpwrap.ini to latest version" -ForegroundColor Green
```

**Why update?**
- Supports latest Windows 10/11 builds
- Community-maintained with recent patches
- Only needed if using RdpWrap wrapper (not TermWrap)

---

## Step 4: Build the Project

```powershell
# Navigate to solution directory
cd C:\path\to\rdpWrapper

# Build Debug version (for testing)
dotnet build rdpWrapper\rdpWrapper.csproj -c Debug

# Or build Release version
dotnet build rdpWrapper\rdpWrapper.csproj -c Release
```

### Expected Output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:05.23
```

### Verify Build Output:
```powershell
# Check executable exists
Get-Item .\rdpWrapper\bin\rdpWrapper.exe

# Check file size (should be ~2-3 MB)
$size = (Get-Item .\rdpWrapper\bin\rdpWrapper.exe).Length / 1MB
Write-Host "Executable size: $([math]::Round($size, 2)) MB"
```

---

## Step 5: Basic CLI Tests (No Installation Yet)

Test that the CLI works without installing anything:

### Test 5.1: Help Command
```powershell
.\rdpWrapper\bin\rdpWrapper.exe --help
```
**Expected**: Full help text showing all commands and options.

### Test 5.2: Status Check (Before Installation)
```powershell
.\rdpWrapper\bin\rdpWrapper.exe --status
```
**Expected**: "rdpWrapper is not installed" or similar message.

### Test 5.3: Invalid Arguments
```powershell
.\rdpWrapper\bin\rdpWrapper.exe --invalid-flag
```
**Expected**: Exit code 1, error message about invalid arguments.

### Test 5.4: Exit Codes
```powershell
# Test success (exit code 0)
.\rdpWrapper\bin\rdpWrapper.exe --help
Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 0) { "Green" } else { "Red" })

# Test invalid args (exit code 1)
.\rdpWrapper\bin\rdpWrapper.exe --invalid
Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 1) { "Green" } else { "Red" })
```

---

## Step 6: Installation Tests

**⚠️ IMPORTANT**: From this point forward, you're modifying system files. Use a test VM!

### Test 6.1: Check Prerequisites

```powershell
# 1. Verify running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Must run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit
}
Write-Host "✓ Running as Administrator" -ForegroundColor Green

# 2. Check RDP service exists
try {
    $service = Get-Service -Name TermService -ErrorAction Stop
    Write-Host "✓ TermService found: $($service.Status)" -ForegroundColor Green
} catch {
    Write-Host "✗ TermService not found" -ForegroundColor Red
}

# 3. Backup current RDP state
$rdpEnabled = (Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections).fDenyTSConnections
Write-Host "Current RDP state: $($rdpEnabled -eq 0 ? 'Enabled' : 'Disabled')"

# 4. Check current sessions
Write-Host "`nCurrent RDP sessions:"
qwinsta
```

### Test 6.2: Auto-Install (Default Configuration)

```powershell
# Run auto-install with enterprise defaults
Write-Host "`n=== Running Auto-Install ===" -ForegroundColor Cyan
.\rdpWrapper\bin\rdpWrapper.exe --auto-install

# Check exit code
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Installation successful (Exit code: $LASTEXITCODE)" -ForegroundColor Green
} else {
    Write-Host "✗ Installation failed (Exit code: $LASTEXITCODE)" -ForegroundColor Red
    Write-Host "Check logs at: $env:TEMP\rdpWrapper.log" -ForegroundColor Yellow
}
```

**What auto-install does**:
- Installs TermWrap wrapper (modern)
- Sets max connections to 10
- Disables single session per user
- Enables RDP on port 3389
- Adds Windows Defender exclusions
- Creates firewall rules

### Test 6.3: Verify Installation

```powershell
Write-Host "`n=== Verifying Installation ===" -ForegroundColor Cyan

# 1. Check status
Write-Host "`n1. Installation Status:"
.\rdpWrapper\bin\rdpWrapper.exe --status

# 2. Verify registry changes
Write-Host "`n2. Registry Configuration:"
$serviceDll = (Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll).ServiceDll
Write-Host "   ServiceDll: $serviceDll"
if ($serviceDll -like "*TermWrap.dll*") {
    Write-Host "   ✓ Wrapper DLL is loaded" -ForegroundColor Green
} else {
    Write-Host "   ✗ Wrapper DLL NOT loaded" -ForegroundColor Red
}

# 3. Verify RDP is enabled
Write-Host "`n3. RDP Status:"
$rdpEnabled = (Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections).fDenyTSConnections
Write-Host "   RDP Enabled: $($rdpEnabled -eq 0)" -ForegroundColor $(if ($rdpEnabled -eq 0) { "Green" } else { "Red" })

# 4. Check service is running
Write-Host "`n4. Service Status:"
$service = Get-Service -Name TermService
Write-Host "   Name: $($service.Name)"
Write-Host "   Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Red" })
Write-Host "   StartType: $($service.StartType)"

# 5. Verify wrapper files exist
Write-Host "`n5. Wrapper Files:"
$files = @(
    "$env:SystemRoot\System32\TermWrap.dll",
    "$env:SystemRoot\System32\Zydis.dll"
)
foreach ($file in $files) {
    $exists = Test-Path $file
    $status = if ($exists) { "✓" } else { "✗" }
    $color = if ($exists) { "Green" } else { "Red" }
    Write-Host "   $status $file" -ForegroundColor $color
}

# 6. Check firewall rules
Write-Host "`n6. Firewall Rules:"
Get-NetFirewallRule -DisplayName "*Remote Desktop*" | Select-Object DisplayName, Enabled | Format-Table

# 7. Check Defender exclusions
Write-Host "7. Windows Defender Exclusions:"
$exclusions = Get-MpPreference | Select-Object -ExpandProperty ExclusionPath
$rdpExclusions = $exclusions | Where-Object { $_ -like "*TermWrap*" -or $_ -like "*rdpWrapper*" }
if ($rdpExclusions) {
    $rdpExclusions | ForEach-Object { Write-Host "   ✓ $_" -ForegroundColor Green }
} else {
    Write-Host "   ⚠ No rdpWrapper exclusions found" -ForegroundColor Yellow
}
```

---

## Step 7: Functional Tests (Core Feature)

### Test 7.1: Single RDP Connection

From a **different computer** on the network:

```powershell
# On test machine, get IP address
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" })[0].IPAddress
Write-Host "`nConnect to RDP from remote machine:" -ForegroundColor Cyan
Write-Host "mstsc /v:$ip:3389" -ForegroundColor Yellow
```

**On remote machine**: Open RDP client and connect to the IP shown above.

**Expected**: Successful RDP connection.

---

### Test 7.2: Multiple Concurrent Sessions ⭐ CRITICAL TEST ⭐

**This is your primary requirement: Multiple admins on shared account**

#### Setup Test User:

```powershell
Write-Host "`n=== Creating Test User for Concurrent Sessions ===" -ForegroundColor Cyan

# Create test user
.\rdpWrapper\bin\rdpWrapper.exe --create-user --username="TestAdmin" --password="SecurePass123!"

# Verify user was created
Write-Host "`nVerifying test user:"
try {
    $user = Get-LocalUser -Name "TestAdmin" -ErrorAction Stop
    Write-Host "✓ User created: $($user.Name)" -ForegroundColor Green
} catch {
    Write-Host "✗ User not found" -ForegroundColor Red
}

# Verify added to Remote Desktop Users group
Write-Host "`nRemote Desktop Users group members:"
Get-LocalGroupMember -Group "Remote Desktop Users" | Select-Object Name
```

#### Test Concurrent Sessions:

**From Computer 1**:
```
1. Open Remote Desktop Connection (mstsc)
2. Computer: <test-server-ip>:3389
3. Username: TestAdmin
4. Password: SecurePass123!
5. Connect and leave session open
```

**From Computer 2**:
```
1. Open Remote Desktop Connection (mstsc)
2. Computer: <test-server-ip>:3389
3. Username: TestAdmin (SAME USER)
4. Password: SecurePass123!
5. Connect and leave session open
```

**From Computer 3** (optional):
```
1. Open Remote Desktop Connection (mstsc)
2. Computer: <test-server-ip>:3389
3. Username: TestAdmin (SAME USER)
4. Password: SecurePass123!
5. Connect
```

#### Verify on Test Server:

```powershell
Write-Host "`n=== Verifying Concurrent Sessions ===" -ForegroundColor Cyan

# Show all active sessions
Write-Host "`nActive sessions:"
qwinsta

# Count active RDP sessions
$activeSessions = (qwinsta | Select-String "Active").Count
Write-Host "`nTotal active sessions: $activeSessions" -ForegroundColor $(if ($activeSessions -gt 1) { "Green" } else { "Red" })

# Count TestAdmin sessions
$testAdminSessions = (qwinsta | Select-String "TestAdmin" | Select-String "Active").Count
Write-Host "TestAdmin active sessions: $testAdminSessions" -ForegroundColor $(if ($testAdminSessions -gt 1) { "Green" } else { "Red" })

if ($testAdminSessions -gt 1) {
    Write-Host "`n✓✓✓ SUCCESS! Multiple concurrent sessions working!" -ForegroundColor Green
} else {
    Write-Host "`n✗✗✗ FAILURE! Only $testAdminSessions session(s) active" -ForegroundColor Red
}
```

#### Expected Output:
```
SESSIONNAME       USERNAME                 ID  STATE   TYPE        DEVICE
services                                    0  Disc
console           Administrator            1  Active
rdp-tcp#0         TestAdmin                2  Active
rdp-tcp#1         TestAdmin                3  Active
rdp-tcp#2         TestAdmin                4  Active

Total active sessions: 4
TestAdmin active sessions: 3

✓✓✓ SUCCESS! Multiple concurrent sessions working!
```

#### SUCCESS CRITERIA:
- ✅ All 3 sessions connect successfully
- ✅ No "maximum connections" error
- ✅ No disconnection of previous sessions
- ✅ All sessions remain active simultaneously
- ✅ All using the SAME user account (TestAdmin)

#### FAILURE INDICATORS:
- ❌ "Another user is logged in" message
- ❌ Previous session disconnects when new one connects
- ❌ Only 2 sessions maximum
- ❌ "Maximum connections exceeded" error

---

## Step 8: Configuration Tests

### Test 8.1: Custom Parameters

```powershell
Write-Host "`n=== Testing Custom Configuration ===" -ForegroundColor Cyan

# Uninstall first
.\rdpWrapper\bin\rdpWrapper.exe --uninstall --silent

# Install with custom configuration
.\rdpWrapper\bin\rdpWrapper.exe --install `
  --wrapper=TermWrap `
  --max-connections=20 `
  --port=3390 `
  --single-session=false `
  --nla=1 `
  --security-layer=2

Write-Host "Exit code: $LASTEXITCODE"

# Verify custom settings
Write-Host "`nVerifying custom settings:"
.\rdpWrapper\bin\rdpWrapper.exe --status

# Test connection on custom port
Write-Host "`nTo test custom port from remote machine:"
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" })[0].IPAddress
Write-Host "mstsc /v:$ip`:3390" -ForegroundColor Yellow
```

### Test 8.2: Profile-Based Installation

```powershell
Write-Host "`n=== Testing Profile-Based Installation ===" -ForegroundColor Cyan

# Check included profiles
Write-Host "`nAvailable profiles:"
Get-ChildItem .\profiles\*.json | Select-Object Name

# Show enterprise-default profile
Write-Host "`nEnterprise default profile contents:"
Get-Content .\profiles\enterprise-default.json | ConvertFrom-Json | ConvertTo-Json

# Install using profile
.\rdpWrapper\bin\rdpWrapper.exe --uninstall --silent
.\rdpWrapper\bin\rdpWrapper.exe --install --profile=".\profiles\enterprise-default.json"

Write-Host "Exit code: $LASTEXITCODE"

# Verify profile settings applied
Write-Host "`nVerifying profile settings:"
.\rdpWrapper\bin\rdpWrapper.exe --status
```

### Test 8.3: Silent Mode

```powershell
Write-Host "`n=== Testing Silent Mode ===" -ForegroundColor Cyan

# Create log directory
New-Item -Path "C:\Temp" -ItemType Directory -Force | Out-Null

# Test silent installation
.\rdpWrapper\bin\rdpWrapper.exe --uninstall --silent
.\rdpWrapper\bin\rdpWrapper.exe --auto-install --silent --log="C:\Temp\rdp-test.log"

# Check exit code
Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 0) { "Green" } else { "Red" })

# Verify log file was created
if (Test-Path "C:\Temp\rdp-test.log") {
    Write-Host "`n✓ Log file created" -ForegroundColor Green
    Write-Host "`nLast 20 lines of log:"
    Get-Content C:\Temp\rdp-test.log -Tail 20
} else {
    Write-Host "`n✗ Log file not created" -ForegroundColor Red
}
```

---

## Step 9: Service Control Tests

```powershell
Write-Host "`n=== Testing Service Control ===" -ForegroundColor Cyan

# Test stop command
Write-Host "`n1. Testing --stop:"
.\rdpWrapper\bin\rdpWrapper.exe --stop
Start-Sleep -Seconds 2
$service = Get-Service -Name TermService
Write-Host "   Service status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Stopped") { "Green" } else { "Red" })

# Test start command
Write-Host "`n2. Testing --start:"
.\rdpWrapper\bin\rdpWrapper.exe --start
Start-Sleep -Seconds 2
$service = Get-Service -Name TermService
Write-Host "   Service status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Red" })

# Verify RDP still works after restart
Write-Host "`n3. After restart, try connecting from remote machine"
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" })[0].IPAddress
Write-Host "   mstsc /v:$ip:3389" -ForegroundColor Yellow
```

---

## Step 10: Uninstall Tests

```powershell
Write-Host "`n=== Testing Uninstall ===" -ForegroundColor Cyan

# Test uninstall
Write-Host "`n1. Running uninstall:"
.\rdpWrapper\bin\rdpWrapper.exe --uninstall

Write-Host "   Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 0) { "Green" } else { "Red" })

# Verify cleanup
Write-Host "`n2. Verifying cleanup:"

# Check ServiceDll restored
$serviceDll = (Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll).ServiceDll
Write-Host "   ServiceDll: $serviceDll"
if ($serviceDll -like "*termsrv.dll*" -and $serviceDll -notlike "*TermWrap*") {
    Write-Host "   ✓ ServiceDll restored to original" -ForegroundColor Green
} else {
    Write-Host "   ✗ ServiceDll not restored" -ForegroundColor Red
}

# Check wrapper files removed
$wrapperExists = Test-Path "$env:SystemRoot\System32\TermWrap.dll"
Write-Host "   TermWrap.dll exists: $wrapperExists" -ForegroundColor $(if (-not $wrapperExists) { "Green" } else { "Red" })

# Verify RDP still works (but in single-session mode)
Write-Host "`n3. RDP should still work (single-session mode)"
Write-Host "   Try connecting from remote machine"
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" })[0].IPAddress
Write-Host "   mstsc /v:$ip:3389" -ForegroundColor Yellow
Write-Host "   Expected: Only 1 session per user allowed"
```

---

## Step 11: Edge Cases and Error Handling

### Test 11.1: Running Without Admin Rights

```powershell
Write-Host "`n=== Testing Non-Admin Execution ===" -ForegroundColor Cyan
Write-Host "NOTE: Open a NEW PowerShell window as regular user (not admin)" -ForegroundColor Yellow
Write-Host "Then run: .\rdpWrapper\bin\rdpWrapper.exe --auto-install" -ForegroundColor Yellow
Write-Host "Expected: Exit code 5, 'Access denied' or 'Run as Administrator' message"
```

### Test 11.2: Invalid Configuration

```powershell
Write-Host "`n=== Testing Invalid Configuration ===" -ForegroundColor Cyan

# Test invalid port
Write-Host "`n1. Testing invalid port (99999):"
.\rdpWrapper\bin\rdpWrapper.exe --install --port=99999
Write-Host "   Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 4) { "Green" } else { "Red" })
Write-Host "   Expected: Exit code 4 (Config Error)"

# Test invalid wrapper type
Write-Host "`n2. Testing invalid wrapper type:"
.\rdpWrapper\bin\rdpWrapper.exe --install --wrapper=InvalidWrapper
Write-Host "   Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -ne 0) { "Green" } else { "Red" })
Write-Host "   Expected: Exit code 1 or 4"

# Test out of range max connections
Write-Host "`n3. Testing out of range max connections:"
.\rdpWrapper\bin\rdpWrapper.exe --install --max-connections=9999999
Write-Host "   Exit code: $LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 4) { "Green" } else { "Red" })
Write-Host "   Expected: Exit code 4 (Config Error)"
```

### Test 11.3: Offline Mode

```powershell
Write-Host "`n=== Testing Offline Mode ===" -ForegroundColor Cyan

# Note: Requires network disconnection or firewall blocking
Write-Host "This test requires network disconnection"
Write-Host "Run: .\rdpWrapper\bin\rdpWrapper.exe --auto-install --offline"
Write-Host "Expected: Should work without checking for updates"
```

---

## Step 12: Stress Testing (Optional)

### Test 12.1: Maximum Concurrent Sessions

```powershell
Write-Host "`n=== Stress Test: Maximum Concurrent Sessions ===" -ForegroundColor Cyan

# Set high connection limit
.\rdpWrapper\bin\rdpWrapper.exe --install --max-connections=50 --single-session=false

Write-Host "`nConfiguration set for 50 max connections"
Write-Host "Try connecting 10-20 concurrent sessions from multiple machines"

# Monitor system resources
Write-Host "`nMonitoring system resources (press Ctrl+C to stop):"
while ($true) {
    Clear-Host
    Write-Host "=== System Resources ===" -ForegroundColor Cyan

    # CPU
    $cpu = (Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples.CookedValue
    Write-Host "CPU Usage: $([math]::Round($cpu, 2))%"

    # Memory
    $memory = Get-Counter '\Memory\Available MBytes'
    Write-Host "Available Memory: $([math]::Round($memory.CounterSamples.CookedValue, 2)) MB"

    # Active sessions
    Write-Host "`nActive RDP Sessions:"
    qwinsta | Select-String "Active"

    Start-Sleep -Seconds 5
}
```

---

## Step 13: Performance Measurements

```powershell
Write-Host "`n=== Performance Measurements ===" -ForegroundColor Cyan

# Measure startup time
Write-Host "`n1. Startup Time:"
$startup = Measure-Command { .\rdpWrapper\bin\rdpWrapper.exe --help | Out-Null }
Write-Host "   Time: $($startup.TotalMilliseconds) ms"

# Measure installation time
Write-Host "`n2. Installation Time:"
.\rdpWrapper\bin\rdpWrapper.exe --uninstall --silent | Out-Null
$install = Measure-Command { .\rdpWrapper\bin\rdpWrapper.exe --auto-install --silent | Out-Null }
Write-Host "   Time: $($install.TotalSeconds) seconds"

# Check memory usage
Write-Host "`n3. Memory Usage:"
Start-Process -FilePath ".\rdpWrapper\bin\rdpWrapper.exe" -ArgumentList "--status" -NoNewWindow -Wait -PassThru | Out-Null
$process = Get-Process rdpWrapper -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "   Working Set: $([math]::Round($process.WorkingSet / 1MB, 2)) MB"
    Write-Host "   Private Memory: $([math]::Round($process.PrivateMemorySize / 1MB, 2)) MB"
}

# Check file size
Write-Host "`n4. File Size:"
$size = (Get-Item .\rdpWrapper\bin\rdpWrapper.exe).Length / 1MB
Write-Host "   rdpWrapper.exe: $([math]::Round($size, 2)) MB"
Write-Host "   Target: ~2-3 MB (framework-dependent)"
Write-Host "   Target: ~50-80 MB (self-contained)"
```

---

## Expected Results Summary

### ✅ Pass Criteria

| Test | Expected Result |
|------|----------------|
| **Build** | No errors, executable ~2-3 MB |
| **Help** | Full help text displayed |
| **Status (before)** | "Not installed" message |
| **Auto-install** | Exit code 0, installation successful |
| **Status (after)** | Shows installed, TermWrap, max 10 connections |
| **Single RDP** | Successful connection |
| **⭐ Multiple RDP (CRITICAL)** | **3+ sessions with same user simultaneously** |
| **Custom config** | Settings applied correctly |
| **Profile-based** | Profile settings applied |
| **Service control** | Stop/start works |
| **Uninstall** | Clean removal, RDP still works |
| **Non-admin** | Exit code 5, access denied |
| **Silent mode** | No output, log file created |
| **Invalid config** | Exit code 4, validation error |

### ❌ Fail Indicators

- Build errors or warnings
- Exit codes other than expected
- Only 2 concurrent sessions (wrapper not working)
- Sessions disconnect each other
- RDP completely broken after uninstall
- Files left behind after uninstall
- Crashes or exceptions

---

## Troubleshooting

### If Build Fails

```powershell
# Clean and rebuild
dotnet clean
dotnet build -v detailed

# Check for missing resources
Get-ChildItem .\rdpWrapper\externals -Recurse

# Ensure DecryptResources.ps1 was run
if (Test-Path ".\rdpWrapper\externals\rdpwrap.ini.cr") {
    Write-Host "ERROR: Encrypted files still present. Run DecryptResources.ps1" -ForegroundColor Red
}
```

### If Installation Fails

```powershell
# Check logs
Get-Content "$env:TEMP\rdpWrapper.log" -Tail 50

# Check service status
Get-Service -Name TermService

# Check for file locks
Get-Process | Where-Object { $_.Modules.FileName -like "*termsrv*" }

# Try manual service restart
Restart-Service -Name TermService -Force
```

### If Multiple Sessions Don't Work

```powershell
Write-Host "=== Diagnosing Multiple Session Failure ===" -ForegroundColor Cyan

# 1. Verify wrapper is actually loaded
$serviceDll = (Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll).ServiceDll
Write-Host "`n1. ServiceDll check:"
Write-Host "   Current: $serviceDll"
if ($serviceDll -notlike "*TermWrap*") {
    Write-Host "   ✗ ERROR: Wrapper DLL not loaded!" -ForegroundColor Red
    Write-Host "   Try reinstalling: .\rdpWrapper\bin\rdpWrapper.exe --auto-install"
} else {
    Write-Host "   ✓ Wrapper DLL is loaded" -ForegroundColor Green
}

# 2. Check registry for concurrent sessions
Write-Host "`n2. Concurrent sessions registry:"
$concurrent = Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\Licensing Core" -Name EnableConcurrentSessions -ErrorAction SilentlyContinue
if ($concurrent) {
    Write-Host "   EnableConcurrentSessions: $($concurrent.EnableConcurrentSessions)"
} else {
    Write-Host "   EnableConcurrentSessions: Not set (may be default)"
}

# 3. Check single session setting
$singleSession = Get-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services" -Name fSingleSessionPerUser -ErrorAction SilentlyContinue
if ($singleSession) {
    Write-Host "   fSingleSessionPerUser: $($singleSession.fSingleSessionPerUser)"
    if ($singleSession.fSingleSessionPerUser -eq 1) {
        Write-Host "   ⚠ WARNING: Single session is ENABLED (should be 0)" -ForegroundColor Yellow
    }
}

# 4. Restart service
Write-Host "`n3. Restarting service:"
.\rdpWrapper\bin\rdpWrapper.exe --stop
Start-Sleep -Seconds 3
.\rdpWrapper\bin\rdpWrapper.exe --start
Write-Host "   Service restarted. Try connecting again."
```

### Check Event Logs

```powershell
# Check for errors in Event Viewer
Write-Host "`nRecent TermService errors:"
Get-WinEvent -LogName System -MaxEvents 20 |
    Where-Object { $_.ProviderName -eq "Service Control Manager" -and $_.Message -like "*TermService*" } |
    Select-Object TimeCreated, Message |
    Format-List
```

---

## After Testing - Commit Changes

If Phase 2 testing is successful and all tests pass:

```powershell
# Stage decrypted files
git add rdpWrapper/externals/

# Optional: Add updated rdpwrap.ini if downloaded
git add rdpWrapper/externals/rdpwrap.ini

# Commit
git commit -m "Phase 2 Testing: Add decrypted resources (tested on Windows)"

# Push to remote
git push origin claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1
```

---

## Exit Codes Reference

| Code | Meaning | Description |
|------|---------|-------------|
| 0 | Success | Operation completed successfully |
| 1 | Invalid Args | Command-line arguments invalid or missing |
| 2 | Install Failed | Installation operation failed |
| 3 | Service Error | Windows service operation failed |
| 4 | Config Error | Configuration validation failed |
| 5 | Insufficient Permissions | Not running as Administrator |
| 99 | Exception | Unexpected exception occurred |

---

## Quick Test Checklist

Use this checklist to track your testing progress:

- [ ] Prerequisites installed (.NET 8 SDK, Git)
- [ ] Code cloned/pulled
- [ ] Resources decrypted (DecryptResources.ps1)
- [ ] rdpwrap.ini updated (optional)
- [ ] Project builds successfully
- [ ] Help command works
- [ ] Status command works (before install)
- [ ] Auto-install succeeds
- [ ] Installation verified (registry, files, service)
- [ ] **⭐ Multiple concurrent sessions work (CRITICAL)**
- [ ] Custom configuration works
- [ ] Profile-based installation works
- [ ] Silent mode works
- [ ] Service control (stop/start) works
- [ ] Uninstall works and cleans up
- [ ] Non-admin execution fails appropriately
- [ ] Invalid configuration fails appropriately
- [ ] Performance acceptable
- [ ] Changes committed to git

---

## Support

For issues or questions:
- Check TROUBLESHOOTING.md
- Check DEPLOYMENT.md
- Review logs at `%TEMP%\rdpWrapper.log`
- Check Event Viewer for TermService errors

---

**Good luck with testing! The most critical test is Step 7.2: Multiple Concurrent Sessions**

---

**Document Version**: 1.0
**Last Updated**: 2025-11-11
**Author**: rdpWrapper Development Team
