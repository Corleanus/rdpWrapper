# Troubleshooting Guide

## Overview

This guide provides solutions to common issues encountered when installing, configuring, or using rdpWrapper.

## Quick Diagnostics

### Run Status Check

```powershell
rdpWrapper.exe --status
```

This displays:
- Installation status
- Wrapper type (TermWrap/RdpWrap)
- Service status
- Current configuration
- Active connections

### Check Logs

```powershell
# Default log location
Get-Content "$env:TEMP\rdpWrapper.log" -Tail 50

# Custom log location (if specified during installation)
Get-Content "C:\Path\To\Custom.log" -Tail 50
```

### Verify Service Status

```powershell
Get-Service -Name TermService | Format-List *
```

### Check Registry Configuration

```powershell
# Check if RDP is enabled
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections

# Check ServiceDll redirection
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll

# Check concurrent sessions configuration
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\Licensing Core" -Name EnableConcurrentSessions -ErrorAction SilentlyContinue
```

## Installation Issues

### Issue 1: "Access denied" or Exit Code 5

**Symptoms:**
- Installation fails with "Access denied" message
- Exit code 5 (Insufficient Permissions)

**Cause:**
- Not running as Administrator
- User Account Control (UAC) blocking execution
- Antivirus blocking registry/service modifications

**Solution:**

1. **Run as Administrator:**
   ```powershell
   # Right-click PowerShell > Run as Administrator
   cd C:\Path\To\rdpWrapper
   .\rdpWrapper.exe --auto-install
   ```

2. **Disable UAC temporarily (if needed):**
   ```powershell
   # Check UAC status
   Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name EnableLUA

   # Disable UAC (requires reboot)
   Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name EnableLUA -Value 0
   Restart-Computer

   # After installation, re-enable UAC
   Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name EnableLUA -Value 1
   Restart-Computer
   ```

3. **Add antivirus exclusions (see Issue 10)**

### Issue 2: "Installation failed" or Exit Code 2

**Symptoms:**
- Installation fails with generic error
- Exit code 2 (Install Failed)
- Log shows exception during installation

**Cause:**
- Wrapper DLL extraction failed
- Service stop/start failed
- Registry modification failed
- Corrupted installation files

**Solution:**

1. **Check log for specific error:**
   ```powershell
   Get-Content "$env:TEMP\rdpWrapper.log" | Select-String -Pattern "ERROR"
   ```

2. **Ensure TermService can be stopped:**
   ```powershell
   # Stop service manually
   Stop-Service -Name TermService -Force

   # Wait for service to stop
   Start-Sleep -Seconds 5

   # Check status
   Get-Service -Name TermService

   # Try installation again
   rdpWrapper.exe --auto-install
   ```

3. **Check disk space:**
   ```powershell
   Get-PSDrive C | Select-Object Used,Free
   ```

4. **Verify file integrity:**
   ```powershell
   # Check if rdpWrapper.exe is corrupted
   Get-FileHash rdpWrapper.exe -Algorithm SHA256

   # Re-download if hash doesn't match expected value
   ```

5. **Clean install:**
   ```powershell
   # Uninstall if partially installed
   rdpWrapper.exe --uninstall --silent

   # Remove leftover files
   Remove-Item "$env:SystemRoot\System32\TermWrap.dll" -Force -ErrorAction SilentlyContinue
   Remove-Item "$env:SystemRoot\System32\Zydis.dll" -Force -ErrorAction SilentlyContinue

   # Reinstall
   rdpWrapper.exe --auto-install
   ```

### Issue 3: "Service error" or Exit Code 3

**Symptoms:**
- Exit code 3 (Service Error)
- TermService won't start or stop
- Service shows "Starting" indefinitely

**Cause:**
- TermService dependency failure
- Corrupted service configuration
- Conflicting software

**Solution:**

1. **Check service dependencies:**
   ```powershell
   Get-Service -Name TermService | Select-Object -ExpandProperty DependentServices
   Get-Service -Name TermService | Select-Object -ExpandProperty RequiredServices
   ```

2. **Verify dependencies are running:**
   ```powershell
   # Remote Desktop Services typically depends on:
   # - RPC
   # - Remote Procedure Call (RPC) Locator (optional)
   Get-Service -Name "RpcSs" | Format-List *
   ```

3. **Reset service to known good state:**
   ```powershell
   # Stop service
   Stop-Service -Name TermService -Force

   # Reset ServiceDll to original value
   Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" `
     -Name ServiceDll -Value "%SystemRoot%\System32\termsrv.dll"

   # Start service to verify it works
   Start-Service -Name TermService

   # If successful, try rdpWrapper installation again
   rdpWrapper.exe --auto-install
   ```

4. **Check Event Logs:**
   ```powershell
   Get-WinEvent -LogName System -MaxEvents 50 | Where-Object { $_.ProviderName -eq "Service Control Manager" -and $_.Message -like "*TermService*" }
   ```

### Issue 4: "Configuration error" or Exit Code 4

**Symptoms:**
- Exit code 4 (Config Error)
- Invalid configuration profile
- Configuration validation failed

**Cause:**
- Invalid JSON in profile file
- Out-of-range parameter values
- Incompatible configuration options

**Solution:**

1. **Validate profile JSON:**
   ```powershell
   # Test JSON syntax
   $profile = Get-Content "C:\Path\To\profile.json" -Raw
   try {
       $json = $profile | ConvertFrom-Json
       Write-Host "JSON is valid"
   }
   catch {
       Write-Host "JSON is invalid: $($_.Exception.Message)"
   }
   ```

2. **Check parameter ranges:**
   ```json
   {
     "maxConnections": 10,          // Must be 0-999999
     "rdpPort": 3389,                // Must be 1-65535
     "nlaLevel": 1,                  // Must be 0, 1, or 2
     "securityLayer": 2,             // Must be 0, 1, or 2
     "singleSessionPerUser": false   // Must be true or false
   }
   ```

3. **Use default profile:**
   ```powershell
   # Copy included profile
   Copy-Item "profiles\enterprise-default.json" -Destination "C:\Temp\test-profile.json"

   # Test with default profile
   rdpWrapper.exe --install --profile="C:\Temp\test-profile.json"
   ```

4. **Use command-line parameters instead:**
   ```powershell
   rdpWrapper.exe --auto-install --max-connections=10 --single-session=false
   ```

## Connection Issues

### Issue 5: Cannot connect to RDP

**Symptoms:**
- RDP connection times out
- Connection refused
- "Remote Desktop can't connect to the remote computer"

**Cause:**
- RDP service not running
- Firewall blocking port 3389
- RDP disabled in Windows settings
- User not in Remote Desktop Users group

**Solution:**

1. **Verify RDP is enabled:**
   ```powershell
   Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections
   # Should return 0 (connections allowed)

   # Enable if disabled:
   Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections -Value 0
   ```

2. **Check service status:**
   ```powershell
   Get-Service -Name TermService

   # Start if stopped:
   Start-Service -Name TermService
   Set-Service -Name TermService -StartupType Automatic
   ```

3. **Check firewall:**
   ```powershell
   Get-NetFirewallRule -DisplayName "Remote Desktop*" | Select-Object DisplayName, Enabled, Direction, Action

   # Enable firewall rule:
   Enable-NetFirewallRule -DisplayGroup "Remote Desktop"
   ```

4. **Test connectivity:**
   ```powershell
   # From remote machine, test port 3389
   Test-NetConnection -ComputerName servername -Port 3389
   ```

5. **Verify user permissions:**
   ```powershell
   # Check Remote Desktop Users group membership
   Get-LocalGroupMember -Group "Remote Desktop Users"

   # Add user to group:
   Add-LocalGroupMember -Group "Remote Desktop Users" -Member "DOMAIN\User"
   ```

### Issue 6: Only one user can connect at a time

**Symptoms:**
- First user connects successfully
- Second user gets "maximum connections" error
- Second connection disconnects first user

**Cause:**
- `singleSessionPerUser` is set to `true`
- `maxConnections` is set to 1
- rdpWrapper not properly installed

**Cause:**
- License configuration limiting connections
- Group Policy overriding settings

**Solution:**

1. **Verify rdpWrapper is installed:**
   ```powershell
   rdpWrapper.exe --status

   # Look for:
   # - "Wrapper installed: Yes"
   # - "Single session per user: No" (should be No for concurrent sessions)
   ```

2. **Check configuration:**
   ```powershell
   # Check concurrent sessions registry key
   Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\Licensing Core" `
     -Name EnableConcurrentSessions -ErrorAction SilentlyContinue

   # Should return 1 (enabled)
   ```

3. **Reinstall with correct configuration:**
   ```powershell
   rdpWrapper.exe --uninstall --silent
   rdpWrapper.exe --auto-install --max-connections=10 --single-session=false
   ```

4. **Check Group Policy:**
   ```powershell
   # Check for GPO restrictions
   gpresult /h gpresult.html

   # Open gpresult.html and search for "Terminal" or "Remote Desktop"
   # Look for: "Restrict Remote Desktop Services users to a single Remote Desktop Services session"
   ```

5. **Override Group Policy (if needed):**
   ```powershell
   # Set registry to override GPO (use with caution)
   Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services" `
     -Name fSingleSessionPerUser -Value 0 -Force
   ```

6. **Restart service:**
   ```powershell
   Restart-Service -Name TermService -Force
   ```

### Issue 7: Users can connect but get black screen or session hangs

**Symptoms:**
- RDP connection establishes
- Authentication succeeds
- Black screen or stuck at "Welcome" screen
- Session becomes unresponsive

**Cause:**
- Graphics driver issues
- Profile corruption
- Video redirection problems
- Resource constraints

**Solution:**

1. **Disable video redirection:**
   ```powershell
   # On RDP client, create .rdp file with:
   # redirectvideo:i:0
   # videoplaybackmode:i:1
   ```

2. **Check system resources:**
   ```powershell
   # Check CPU, memory, disk
   Get-Counter '\Processor(_Total)\% Processor Time'
   Get-Counter '\Memory\Available MBytes'
   Get-Counter '\PhysicalDisk(_Total)\% Disk Time'
   ```

3. **Check Event Logs:**
   ```powershell
   Get-WinEvent -LogName "Microsoft-Windows-TerminalServices-LocalSessionManager/Operational" -MaxEvents 20
   ```

4. **Try safe mode connection:**
   ```
   # In RDP client .rdp file:
   screen mode id:i:1
   use multimon:i:0
   desktopwidth:i:800
   desktopheight:i:600
   session bpp:i:16
   ```

5. **Recreate user profile:**
   ```powershell
   # Rename existing profile (as admin)
   Rename-Item "C:\Users\Username" "C:\Users\Username.old"

   # User logs in again with fresh profile
   ```

## Concurrent Session Issues

### Issue 8: Maximum of 2 connections allowed (not 10 as configured)

**Symptoms:**
- 2 users can connect simultaneously
- 3rd user gets "maximum connections" error
- `maxConnections` is set to 10 but limit is 2

**Cause:**
- Windows Home/Professional edition limitation (not Server)
- rdpWrapper configured but not working correctly
- Wrapper DLL not loading

**Solution:**

1. **Verify wrapper is actually loaded:**
   ```powershell
   # Check ServiceDll value
   $serviceDll = Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll
   $serviceDll.ServiceDll

   # Should show path to TermWrap.dll, not termsrv.dll
   # Example: C:\Windows\System32\TermWrap.dll
   ```

2. **Check wrapper type:**
   ```powershell
   rdpWrapper.exe --status
   # Look for "Wrapper: TermWrap" (or RdpWrap)
   ```

3. **Verify termsrv.dll is patched:**
   ```powershell
   # TermWrap works by wrapping, not patching
   # Verify TermWrap.dll exists:
   Test-Path "$env:SystemRoot\System32\TermWrap.dll"
   ```

4. **Reinstall wrapper:**
   ```powershell
   rdpWrapper.exe --stop
   rdpWrapper.exe --uninstall --silent
   rdpWrapper.exe --auto-install --max-connections=10 --single-session=false
   rdpWrapper.exe --status
   ```

5. **Check Windows edition:**
   ```powershell
   Get-ComputerInfo | Select-Object WindowsProductName, WindowsEditionId

   # rdpWrapper works on:
   # - Windows 10/11 Pro, Enterprise, Education
   # - Windows Server 2016/2019/2022
   # Does NOT work reliably on Home editions
   ```

## Antivirus and Security Software Issues

### Issue 9: Antivirus quarantines rdpWrapper.exe

**Symptoms:**
- Installation completes but rdpWrapper.exe is removed
- Antivirus shows detection/quarantine notification
- Files disappear after installation

**Cause:**
- Heuristic detection of DLL injection
- Behavioral detection of service modification
- False positive due to wrapper behavior

**Solution:**

1. **Add exclusions before installation:**
   ```powershell
   # Windows Defender (done automatically by rdpWrapper)
   Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\rdpWrapper.exe"
   Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\TermWrap.dll"
   Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\Zydis.dll"
   Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\termsrv.dll.bak"
   ```

2. **Third-party antivirus exclusions:**
   ```
   Sophos: Add to "Authorized Applications"
   McAfee: Add to "Exclusions" under "Real-Time Scanning"
   Symantec: Add to "Exceptions" in policy
   Trend Micro: Add to "Approved List"
   ```

3. **Restore from quarantine:**
   ```powershell
   # Windows Defender
   Get-MpThreat | Where-Object { $_.Resources -like "*rdpWrapper*" }

   # Remove from threat history
   Remove-MpThreat -Threat <ThreatID>
   ```

4. **Use code signing (future):**
   - Signed executables have lower false positive rates
   - Awaiting code signing certificate

### Issue 10: Windows Defender blocks installation with "Behavior:Win32/Suspic" detection

**Symptoms:**
- Installation blocked immediately
- Windows Security shows alert
- Real-time protection blocks execution

**Cause:**
- Cloud-delivered protection detecting suspicious behavior
- PUA (Potentially Unwanted Application) detection
- Behavioral monitoring flagging system modification

**Solution:**

1. **Disable real-time protection temporarily:**
   ```powershell
   # Disable real-time protection (requires admin)
   Set-MpPreference -DisableRealtimeMonitoring $true

   # Install rdpWrapper
   rdpWrapper.exe --auto-install

   # Re-enable real-time protection
   Set-MpPreference -DisableRealtimeMonitoring $false
   ```

2. **Add exclusions first:**
   ```powershell
   Add-MpPreference -ExclusionProcess "rdpWrapper.exe"
   Add-MpPreference -ExclusionPath "C:\Path\To\rdpWrapper"

   # Then install
   rdpWrapper.exe --auto-install
   ```

3. **Disable cloud-delivered protection:**
   ```powershell
   Set-MpPreference -MAPSReporting Disabled
   Set-MpPreference -SubmitSamplesConsent NeverSend

   # Install
   rdpWrapper.exe --auto-install

   # Re-enable (optional)
   Set-MpPreference -MAPSReporting Advanced
   Set-MpPreference -SubmitSamplesConsent SendSafeSamples
   ```

4. **Submit false positive report:**
   - Visit: https://www.microsoft.com/en-us/wdsi/filesubmission
   - Upload rdpWrapper.exe
   - Select "I don't think this file is malware"
   - Provide context (legitimate RDP enhancement tool)

## Update and Maintenance Issues

### Issue 11: rdpWrapper stops working after Windows Update

**Symptoms:**
- rdpWrapper worked before Windows Update
- After update, only 2 connections allowed
- Service starts but wrapper not functioning

**Cause:**
- Windows Update may replace termsrv.dll
- Registry changes reverted
- Service configuration reset

**Solution:**

1. **Check if wrapper is still installed:**
   ```powershell
   rdpWrapper.exe --status

   # Check ServiceDll
   Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll
   ```

2. **Reinstall wrapper:**
   ```powershell
   # Usually just need to reinstall
   rdpWrapper.exe --uninstall --silent
   rdpWrapper.exe --auto-install
   ```

3. **Prevent automatic reversal:**
   ```powershell
   # Set registry permissions to prevent modification
   $acl = Get-Acl "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters"
   $rule = New-Object System.Security.AccessControl.RegistryAccessRule(
       "NT AUTHORITY\SYSTEM", "SetValue", "Deny"
   )
   $acl.AddAccessRule($rule)
   Set-Acl -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -AclObject $acl

   # WARNING: This may prevent legitimate Windows Updates from functioning correctly
   # Use with extreme caution
   ```

4. **Monitor for updates:**
   ```powershell
   # Create scheduled task to check status after updates
   # See GROUP_POLICY_GUIDE.md for scheduled task examples
   ```

### Issue 12: Cannot uninstall rdpWrapper

**Symptoms:**
- Uninstall command fails
- Files remain after uninstall
- Service still redirected to wrapper

**Cause:**
- Files in use (service running)
- Permission issues
- Corrupted configuration

**Solution:**

1. **Force stop service first:**
   ```powershell
   Stop-Service -Name TermService -Force

   # Wait for service to stop
   Start-Sleep -Seconds 5

   # Verify stopped
   Get-Service -Name TermService

   # Now uninstall
   rdpWrapper.exe --uninstall --silent
   ```

2. **Manual uninstall:**
   ```powershell
   # Stop service
   Stop-Service -Name TermService -Force

   # Restore original ServiceDll
   Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" `
     -Name ServiceDll -Value "%SystemRoot%\System32\termsrv.dll"

   # Delete wrapper files
   Remove-Item "$env:SystemRoot\System32\TermWrap.dll" -Force
   Remove-Item "$env:SystemRoot\System32\Zydis.dll" -Force
   Remove-Item "$env:SystemRoot\System32\rdpwrap.ini" -Force -ErrorAction SilentlyContinue
   Remove-Item "$env:SystemRoot\System32\termsrv.dll.bak" -Force -ErrorAction SilentlyContinue

   # Start service
   Start-Service -Name TermService
   ```

3. **Remove Defender exclusions:**
   ```powershell
   Remove-MpPreference -ExclusionPath "$env:SystemRoot\System32\rdpWrapper.exe"
   Remove-MpPreference -ExclusionPath "$env:SystemRoot\System32\TermWrap.dll"
   Remove-MpPreference -ExclusionPath "$env:SystemRoot\System32\Zydis.dll"
   ```

## Performance Issues

### Issue 13: Slow RDP connections or laggy sessions

**Symptoms:**
- Slow screen updates
- Mouse/keyboard lag
- Audio stuttering
- Sessions feel unresponsive

**Cause:**
- Network bandwidth limitations
- Server resource constraints
- Too many concurrent sessions
- Inefficient RDP settings

**Solution:**

1. **Check server resources:**
   ```powershell
   # CPU usage
   Get-Counter '\Processor(_Total)\% Processor Time'

   # Memory usage
   Get-Counter '\Memory\% Committed Bytes In Use'

   # Disk I/O
   Get-Counter '\PhysicalDisk(_Total)\% Disk Time'

   # Network
   Get-Counter '\Network Interface(*)\Bytes Total/sec'
   ```

2. **Limit concurrent connections:**
   ```powershell
   # Reduce max connections if server is overloaded
   rdpWrapper.exe --uninstall --silent
   rdpWrapper.exe --auto-install --max-connections=5
   ```

3. **Optimize RDP client settings:**
   ```
   # In .rdp file or connection settings:
   - Reduce color depth: 16-bit instead of 32-bit
   - Disable desktop composition
   - Disable font smoothing
   - Disable Windows animations
   - Limit audio quality
   ```

4. **Check network:**
   ```powershell
   # Test latency to server
   Test-Connection -ComputerName servername -Count 10

   # Check bandwidth
   # Use iperf or similar tool
   ```

5. **Enable RDP compression:**
   ```powershell
   Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services" `
     -Name "fEnableCompression" -Value 1 -Type DWord
   ```

## Diagnostic Commands Summary

```powershell
# Quick diagnostic script
Write-Host "=== rdpWrapper Diagnostic Report ===" -ForegroundColor Cyan

# 1. Check installation
Write-Host "`n1. Installation Status:" -ForegroundColor Yellow
rdpWrapper.exe --status

# 2. Check service
Write-Host "`n2. TermService Status:" -ForegroundColor Yellow
Get-Service -Name TermService | Format-List Name, Status, StartType

# 3. Check registry
Write-Host "`n3. Registry Configuration:" -ForegroundColor Yellow
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections

# 4. Check files
Write-Host "`n4. Wrapper Files:" -ForegroundColor Yellow
Get-Item "$env:SystemRoot\System32\TermWrap.dll" -ErrorAction SilentlyContinue
Get-Item "$env:SystemRoot\System32\Zydis.dll" -ErrorAction SilentlyContinue

# 5. Check active sessions
Write-Host "`n5. Active RDP Sessions:" -ForegroundColor Yellow
qwinsta

# 6. Check firewall
Write-Host "`n6. Firewall Rules:" -ForegroundColor Yellow
Get-NetFirewallRule -DisplayName "*Remote Desktop*" | Select-Object DisplayName, Enabled

# 7. Check logs
Write-Host "`n7. Recent Log Entries:" -ForegroundColor Yellow
Get-Content "$env:TEMP\rdpWrapper.log" -Tail 20 -ErrorAction SilentlyContinue

# 8. System info
Write-Host "`n8. System Information:" -ForegroundColor Yellow
Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsArchitecture

Write-Host "`n=== End of Diagnostic Report ===" -ForegroundColor Cyan
```

## Getting Help

If you continue to experience issues:

1. **Collect diagnostic information:**
   - Run `rdpWrapper.exe --status` and save output
   - Collect log file from `%TEMP%\rdpWrapper.log`
   - Run diagnostic script above
   - Export relevant registry keys

2. **Check documentation:**
   - [README.md](README.md) - CLI reference
   - [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment guide
   - [GROUP_POLICY_GUIDE.md](GROUP_POLICY_GUIDE.md) - GPO deployment
   - [TEST_PLAN.md](TEST_PLAN.md) - Testing procedures

3. **Report issue:**
   - Include diagnostic information
   - Describe expected vs actual behavior
   - Provide exact commands used
   - Include relevant log excerpts
   - Specify Windows version and edition

## Common Exit Codes Reference

| Code | Meaning | Common Causes |
|------|---------|---------------|
| 0 | Success | Operation completed successfully |
| 1 | Invalid Args | Missing required parameters, typos in command |
| 2 | Install Failed | Service issues, file extraction failure, disk full |
| 3 | Service Error | TermService won't start/stop, dependency failure |
| 4 | Config Error | Invalid JSON profile, out-of-range values |
| 5 | Insufficient Permissions | Not running as admin, UAC blocking, AV blocking |
| 99 | Exception | Unexpected error, check logs for details |
