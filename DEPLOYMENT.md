# Enterprise Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying rdpWrapper in enterprise environments. The tool has been designed for silent, automated deployment across multiple servers.

## Prerequisites

### System Requirements
- Windows 10/11 or Windows Server 2016/2019/2022
- .NET 8 Runtime (framework-dependent) or none required (self-contained)
- Administrator privileges
- Service Control Manager access
- Registry write permissions

### Network Requirements
- No internet connectivity required for installation
- Optional: Internet access for update checks (use `--offline` flag to disable)

## Deployment Methods

### Method 1: Single-Command Installation (Recommended)

The simplest deployment method using enterprise defaults:

```powershell
rdpWrapper.exe --auto-install --silent
```

**What this does:**
- Installs TermWrap wrapper (modern Windows 10/11 support)
- Configures for multiple concurrent sessions (max: 10)
- Disables single session per user restriction
- Enables RDP on port 3389
- Adds Windows Defender exclusions
- Creates firewall rules
- Runs completely silently (no console output)

**Exit Codes:**
- `0` - Success
- `1` - Invalid arguments
- `2` - Installation failed
- `3` - Service error
- `4` - Configuration error
- `5` - Insufficient permissions

**Example with error checking:**
```powershell
rdpWrapper.exe --auto-install --silent
if ($LASTEXITCODE -eq 0) {
    Write-Host "Installation successful"
} else {
    Write-Host "Installation failed with code $LASTEXITCODE"
    exit $LASTEXITCODE
}
```

### Method 2: Custom Configuration

Install with specific parameters:

```powershell
rdpWrapper.exe --install --wrapper=TermWrap --max-connections=20 --port=3389 --single-session=false --silent
```

**Available Parameters:**
- `--wrapper=<TermWrap|RdpWrap>` - Wrapper type
- `--max-connections=<number>` - Maximum concurrent connections (0-999999)
- `--port=<number>` - RDP port (1-65535, default: 3389)
- `--single-session=<true|false>` - Single session per user restriction
- `--nla=<0|1|2>` - Network Level Authentication (0=off, 1=on, 2=negotiate)
- `--security-layer=<0|1|2>` - RDP security layer (0=RDP, 1=Negotiate, 2=SSL/TLS)
- `--offline` - Skip update checks
- `--silent` - No console output
- `--log=<path>` - Custom log file path

### Method 3: Profile-Based Configuration

Use JSON configuration profiles for standardized deployments:

```powershell
rdpWrapper.exe --install --profile="C:\Deploy\configs\enterprise-default.json" --silent
```

**Profile Structure (`enterprise-default.json`):**
```json
{
  "name": "Enterprise Multi-Admin",
  "description": "Configuration for multiple administrators",
  "wrapper": "TermWrap",
  "rdpPort": 3389,
  "maxConnections": 10,
  "singleSessionPerUser": false,
  "allowTsConnections": true,
  "nlaLevel": 1,
  "securityLayer": 2,
  "addDefenderExclusion": true,
  "addFirewallRule": true
}
```

**Profile locations:**
- Included profiles: `profiles/enterprise-default.json`, `profiles/high-security.json`
- Custom profiles: Store in shared network location for centralized management

### Method 4: Group Policy Deployment (GPO)

See [GROUP_POLICY_GUIDE.md](GROUP_POLICY_GUIDE.md) for detailed GPO deployment instructions.

### Method 5: SCCM/Intune Deployment

**SCCM Package Configuration:**
1. Package Type: Standard Program
2. Command Line: `rdpWrapper.exe --auto-install --silent --log="%TEMP%\rdpWrapper-install.log"`
3. Run Mode: Run with administrative rights
4. Run: Whether or not a user is logged on
5. Detection Method: Registry key `HKLM\SYSTEM\CurrentControlSet\Services\TermService\Parameters\ServiceDll` contains path to wrapper DLL

**Intune Win32 App:**
1. Convert to `.intunewin` format using Microsoft Win32 Content Prep Tool
2. Install command: `rdpWrapper.exe --auto-install --silent`
3. Uninstall command: `rdpWrapper.exe --uninstall --silent`
4. Detection rule: Registry key as above
5. Return codes: Map 0=Success, 1-5=Failure codes

## Deployment Scenarios

### Scenario 1: Multiple Administrators on Shared Account

**Requirement:** 5-10 administrators need to connect simultaneously using a single "ServerAdmin" account.

**Configuration:**
```powershell
rdpWrapper.exe --auto-install --max-connections=10 --single-session=false --silent
```

**Why this works:**
- `singleSessionPerUser=false` allows multiple sessions per user account
- `maxConnections=10` supports all administrators connecting simultaneously
- TermWrap provides modern Windows 10/11 compatibility

### Scenario 2: High-Security Environment

**Requirement:** RDP access with maximum security settings.

**Configuration:**
```powershell
rdpWrapper.exe --install --profile="C:\Deploy\profiles\high-security.json" --silent
```

**Profile includes:**
- NLA Level: Required (2)
- Security Layer: SSL/TLS (2)
- Single session per user: Enabled
- Shadow/remote control: Disabled
- Audio/video capture: Disabled
- USB redirection: Admin only

### Scenario 3: Development/Test Servers

**Requirement:** Maximum flexibility for developer testing.

**Configuration:**
```powershell
rdpWrapper.exe --auto-install --max-connections=999999 --silent
```

### Scenario 4: Remote Branch Offices

**Requirement:** Deploy to 100+ branch servers without internet connectivity.

**Deployment:**
1. Copy `rdpWrapper.exe` to network share `\\fileserver\deploy\rdpWrapper\`
2. Create GPO with startup script:
```powershell
\\fileserver\deploy\rdpWrapper\rdpWrapper.exe --auto-install --silent --offline --log="\\fileserver\logs\rdpWrapper-%COMPUTERNAME%.log"
```

## Verification

### Check Installation Status

```powershell
rdpWrapper.exe --status
```

**Output includes:**
- Installation status (installed/not installed)
- Wrapper type (TermWrap/RdpWrap)
- RDP service status
- Current configuration (port, max connections, etc.)
- Active connections count

### Verify Service

```powershell
Get-Service -Name TermService | Select-Object Status, StartType
```

Should show: `Running` and `Automatic`

### Verify Registry Configuration

```powershell
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections
```

Should return: `0` (connections allowed)

### Test RDP Connection

From remote machine:
```powershell
mstsc /v:servername:3389
```

### Check Concurrent Sessions

```powershell
qwinsta
```

Should show multiple active sessions if configured correctly.

## Post-Deployment

### Monitoring

**Check logs:**
```powershell
# Default log location
Get-Content "$env:TEMP\rdpWrapper.log" -Tail 50

# Custom log location
Get-Content "C:\Logs\rdpWrapper-install.log" -Tail 50
```

**Monitor active sessions:**
```powershell
# List all sessions
qwinsta

# Count active sessions
(qwinsta | Select-String "Active").Count
```

### Maintenance

**Check for updates (manual):**
```powershell
rdpWrapper.exe --status
# Output will show if newer version is available
```

**Restart RDP service if needed:**
```powershell
rdpWrapper.exe --stop
rdpWrapper.exe --start
```

**Verify configuration hasn't changed:**
```powershell
rdpWrapper.exe --status
```

### Uninstallation

**Remove rdpWrapper:**
```powershell
rdpWrapper.exe --uninstall --silent
```

**What this does:**
- Stops TermService
- Restores original ServiceDll registry value
- Removes wrapper DLLs
- Removes Windows Defender exclusions (if added)
- Leaves RDP enabled (doesn't disable RDP)
- Leaves firewall rules (for safety)

**Complete cleanup (optional):**
```powershell
# Uninstall rdpWrapper
rdpWrapper.exe --uninstall --silent

# Remove firewall rules (if you want to disable RDP)
Remove-NetFirewallRule -DisplayName "Remote Desktop*"

# Disable RDP (if you want to disable RDP)
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name fDenyTSConnections -Value 1
```

## Security Considerations

### Antivirus Exclusions

rdpWrapper automatically adds Windows Defender exclusions for its own files. For third-party antivirus:

**Manual exclusions:**
- `%SystemRoot%\System32\termsrv.dll.bak` (backup file)
- `%SystemRoot%\System32\TermWrap.dll` (wrapper DLL)
- `%SystemRoot%\System32\Zydis.dll` (dependency)

**Why needed:**
- DLL injection techniques trigger heuristic detection
- Modifying system service behavior looks suspicious
- Registry redirection appears as suspicious activity

**Note:** Phase 2 encryption removal reduced false positives from 40-60% to expected 20-30%. Code signing would further reduce to 5-10%.

### Firewall Configuration

rdpWrapper automatically creates firewall rules when `addFirewallRule=true` in profile.

**Manual firewall rule:**
```powershell
New-NetFirewallRule -DisplayName "Remote Desktop - User Mode (TCP-In)" `
  -Direction Inbound -Protocol TCP -LocalPort 3389 -Action Allow `
  -Profile Domain,Private -Enabled True
```

### Access Control

**Limit RDP access to specific users:**
```powershell
# Add user to Remote Desktop Users group
Add-LocalGroupMember -Group "Remote Desktop Users" -Member "DOMAIN\User"

# Or use GPO: Computer Configuration > Windows Settings > Security Settings > Restricted Groups
```

**Limit concurrent sessions:**
- Use `--max-connections=<number>` to enforce hard limit
- Monitor with `qwinsta` command

### Audit Logging

**Enable RDP audit logging:**
```powershell
auditpol /set /subcategory:"Logon" /success:enable /failure:enable
auditpol /set /subcategory:"Other Logon/Logoff Events" /success:enable /failure:enable
```

**Check logs:**
```powershell
Get-WinEvent -LogName Security | Where-Object { $_.Id -eq 4624 -and $_.Properties[8].Value -eq 10 }
```

Event ID 4624 with Logon Type 10 = RDP logon

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for comprehensive troubleshooting guide.

## Best Practices

1. **Test in staging environment first**
   - Deploy to test server
   - Verify functionality with multiple concurrent sessions
   - Test uninstall and rollback procedures

2. **Use profile-based configuration**
   - Create standardized profiles for different server types
   - Store profiles in version control
   - Use network share for centralized profile distribution

3. **Implement monitoring**
   - Collect installation logs to central location
   - Monitor RDP service status
   - Alert on failed installations (exit code != 0)

4. **Document your configuration**
   - Record which servers have rdpWrapper installed
   - Document the specific configuration used (profile/parameters)
   - Keep rollback procedures documented

5. **Keep deployment packages updated**
   - Check for rdpWrapper updates quarterly
   - Test updates in staging before production deployment
   - Maintain previous version for quick rollback

6. **Security hardening**
   - Use NLA (Network Level Authentication) when possible
   - Implement IP restrictions via firewall
   - Use strong passwords or certificate-based authentication
   - Monitor for unauthorized access attempts
   - Limit concurrent connections to actual need

7. **Backup before deployment**
   - Backup system state before installation
   - rdpWrapper creates backup of original termsrv.dll automatically
   - Test restore procedures in staging

## Support and Resources

- **Project Repository:** [GitHub Link]
- **Issue Reporting:** [GitHub Issues]
- **Documentation:** See `README.md` for CLI reference
- **Test Plan:** See `TEST_PLAN.md` for testing procedures
- **Migration Notes:** See `MIGRATION_NOTES.md` for changes from GUI version

## Appendix: Exit Codes Reference

| Code | Meaning | Description |
|------|---------|-------------|
| 0 | Success | Operation completed successfully |
| 1 | Invalid Args | Command-line arguments are invalid or missing |
| 2 | Install Failed | Installation operation failed (check logs) |
| 3 | Service Error | Windows service operation failed (TermService) |
| 4 | Config Error | Configuration validation failed or apply failed |
| 5 | Insufficient Permissions | Not running as Administrator or access denied |
| 99 | Exception | Unexpected exception occurred (check logs) |

## Appendix: Registry Keys Modified

| Key Path | Value Name | Purpose |
|----------|------------|---------|
| `HKLM\SYSTEM\CurrentControlSet\Services\TermService\Parameters` | `ServiceDll` | Redirected to wrapper DLL |
| `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server` | `fDenyTSConnections` | Enable/disable RDP |
| `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp` | `PortNumber` | RDP port configuration |
| `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\Licensing Core` | `EnableConcurrentSessions` | Multiple sessions |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services` | Various | Group Policy settings |

## Appendix: File Locations

| File | Location | Purpose |
|------|----------|---------|
| `rdpWrapper.exe` | Deployment location | Main executable |
| `TermWrap.dll` | `%SystemRoot%\System32\` | Wrapper DLL (x64) |
| `Zydis.dll` | `%SystemRoot%\System32\` | Dependency library |
| `termsrv.dll.bak` | `%SystemRoot%\System32\` | Backup of original DLL |
| `rdpwrap.ini` | `%SystemRoot%\System32\` | Configuration file (optional) |
| Log file | `%TEMP%\rdpWrapper.log` | Default log location |
