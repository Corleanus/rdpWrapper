# RDP Wrapper
[![Release](https://img.shields.io/github/v/release/rdp-wrapper/rdpWrapper)](https://github.com/rdp-wrapper/rdpWrapper/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/rdp-wrapper/rdpWrapper/total?color=ff4f42)](https://sergiye.github.io/github-release-stats/?username=rdp-wrapper&repository=rdpWrapper&page=1&per_page=100)
![Last commit](https://img.shields.io/github/last-commit/rdp-wrapper/rdpWrapper?color=00AD00)

[![](https://img.shields.io/badge/WINDOWS-7%20%E2%80%93%2011-blue)](https://endoflife.date/windows)
[![](https://img.shields.io/badge/SERVER-2012%20%E2%80%93%202025-blue)](https://endoflife.date/windows-server)
![license](https://img.shields.io/github/license/rdp-wrapper/rdpWrapper)

----

## Overview

`RDP Wrapper` is a headless CLI tool for RDP setup and configuration in enterprise environments.

This tool is inspired by the [stascorp's rdpwrap project](https://github.com/stascorp/rdpwrap).
Written in .NET 8, it provides a portable, single-file application for automated RDP configuration.

The tool supports auto-generation of offsets for new/updated Windows versions - thanks to the @llccd projects:
 - [TermWrap](https://github.com/llccd/TermWrap)
 - [RDPWrapOffsetFinder](https://github.com/llccd/RDPWrapOffsetFinder).

RDP Wrapper works as a layer between Service Control Manager and Terminal Services, so the original termsrv.dll file remains untouched. This method is resilient against Windows Updates.

It's recommended to have the original termsrv.dll file with the RDP Wrapper installation. If you have modified it with other patchers, it may become unstable.

### What can it do?

The application is a portable command-line tool with the following features:
 - RDP Wrapper does not patch termsrv.dll, it loads termsrv with different parameters
 - Enable RDP host server on any Windows edition beginning from Vista
 - Allow multiple concurrent RDP sessions with the same user account
 - Console and remote sessions at the same time
 - Enabled camera and USB redirection (when TermWrap installed)
 - Show RDP service current status
 - Configure RDP options (port, max connections, NLA, etc.)
 - Install / uninstall wrapper
 - Silent mode for automated deployment (GPO, SCCM, Intune)
 - Profile-based configuration for standardization
 - Generate config for unsupported OS (after Windows updates) - requires [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022)
 - Console and RDP session shadowing (using [Task Manager in Windows 7](http://cdn.freshdesk.com/data/helpdesk/attachments/production/1009641577/original/remote_control.png?1413476051) and [Remote Desktop Connection in Windows 8+](http://woshub.com/rds-shadow-how-to-connect-to-a-user-session-in-windows-server-2012-r2/))
 - Windows 2000, XP and Server 2003 are not supported

## Quick Start

### Single-Command Installation

For most enterprise scenarios, use the auto-install command:

```powershell
rdpWrapper.exe --auto-install
```

This installs rdpWrapper with enterprise defaults:
- TermWrap wrapper (modern Windows 10/11 support)
- Maximum 10 concurrent connections
- Multiple sessions per user enabled
- RDP on port 3389
- Windows Defender exclusions added
- Firewall rules created

### Silent Installation (for automation)

```powershell
rdpWrapper.exe --auto-install --silent
```

Perfect for GPO, SCCM, or Intune deployment. Check exit code for success/failure.

## CLI Reference

### Installation Commands

**Auto-install with enterprise defaults:**
```powershell
rdpWrapper.exe --auto-install [options]
```

**Custom installation:**
```powershell
rdpWrapper.exe --install [options]
```

**Uninstall:**
```powershell
rdpWrapper.exe --uninstall [--silent]
```

### Configuration Options

| Option | Description | Values | Default |
|--------|-------------|--------|---------|
| `--wrapper` | Wrapper type to install | `TermWrap`, `RdpWrap` | `TermWrap` |
| `--max-connections` | Maximum concurrent connections | `0-999999` | `10` |
| `--port` | RDP port number | `1-65535` | `3389` |
| `--single-session` | Single session per user | `true`, `false` | `false` |
| `--nla` | Network Level Authentication | `0` (off), `1` (on), `2` (negotiate) | `1` |
| `--security-layer` | RDP security layer | `0` (RDP), `1` (Negotiate), `2` (SSL/TLS) | `2` |
| `--profile` | Configuration profile path | Path to JSON file | None |
| `--silent` | No console output | Flag | Off |
| `--offline` | Skip update checks | Flag | Off |
| `--log` | Custom log file path | File path | `%TEMP%\rdpWrapper.log` |

### Status and Service Commands

**Check status:**
```powershell
rdpWrapper.exe --status
```

Displays:
- Installation status
- Wrapper type
- Service status
- Configuration details
- Active connections

**Start/stop service:**
```powershell
rdpWrapper.exe --start
rdpWrapper.exe --stop
```

### Profile-Based Configuration

**Use pre-configured profile:**
```powershell
rdpWrapper.exe --install --profile="profiles\enterprise-default.json"
```

**Generate example profiles:**
```powershell
rdpWrapper.exe --generate-config
```

Creates example JSON profiles in `profiles/` folder.

### User Management

**Create RDP user:**
```powershell
rdpWrapper.exe --create-user --username="RdpUser" --password="SecurePass123"
```

Creates user and adds to Remote Desktop Users group.

### Examples

**Multiple admins on shared account:**
```powershell
rdpWrapper.exe --auto-install --max-connections=10 --single-session=false
```

**High-security configuration:**
```powershell
rdpWrapper.exe --install --profile="profiles\high-security.json"
```

**Custom port and max connections:**
```powershell
rdpWrapper.exe --install --port=3390 --max-connections=20 --single-session=false
```

**Silent uninstall:**
```powershell
rdpWrapper.exe --uninstall --silent
```

## Exit Codes

Use exit codes for automation and error handling:

| Code | Meaning | Description |
|------|---------|-------------|
| 0 | Success | Operation completed successfully |
| 1 | Invalid Args | Command-line arguments invalid or missing |
| 2 | Install Failed | Installation operation failed |
| 3 | Service Error | Windows service operation failed |
| 4 | Config Error | Configuration validation failed |
| 5 | Insufficient Permissions | Not running as Administrator |
| 99 | Exception | Unexpected exception occurred |

**Example with error handling:**
```powershell
rdpWrapper.exe --auto-install --silent
if ($LASTEXITCODE -eq 0) {
    Write-Host "Installation successful"
} else {
    Write-Host "Installation failed with code $LASTEXITCODE"
    exit $LASTEXITCODE
}
```

## Configuration Profiles

Profiles are JSON files that define complete RDP configurations. Use them for:
- Standardized deployments across multiple servers
- Different configurations for different server roles
- Version-controlled configuration management

**Example profile (enterprise-default.json):**
```json
{
  "name": "Enterprise Multi-Admin",
  "description": "Multiple administrators on shared account",
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

**Included profiles:**
- `enterprise-default.json` - Multiple concurrent sessions, moderate security
- `high-security.json` - Maximum security settings, single session per user

## Deployment

### Manual Deployment

1. Copy `rdpWrapper.exe` to target server
2. Run as Administrator:
   ```powershell
   rdpWrapper.exe --auto-install
   ```
3. Verify installation:
   ```powershell
   rdpWrapper.exe --status
   ```

### Group Policy (GPO) Deployment

Create startup script:
```powershell
\\fileserver\Deploy\rdpWrapper.exe --auto-install --silent --offline --log="C:\Logs\rdpWrapper.log"
```

See [GROUP_POLICY_GUIDE.md](GROUP_POLICY_GUIDE.md) for detailed instructions.

### SCCM / Intune Deployment

**Install command:**
```
rdpWrapper.exe --auto-install --silent
```

**Uninstall command:**
```
rdpWrapper.exe --uninstall --silent
```

**Detection method:**
- Registry: `HKLM\SYSTEM\CurrentControlSet\Services\TermService\Parameters\ServiceDll` contains `TermWrap.dll` or `rdpwrap.dll`

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment guide.

## Documentation

- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Comprehensive enterprise deployment guide
- **[GROUP_POLICY_GUIDE.md](GROUP_POLICY_GUIDE.md)** - Group Policy deployment with scripts
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Common issues and solutions
- **[TEST_PLAN.md](TEST_PLAN.md)** - Testing procedures
- **[MIGRATION_NOTES.md](MIGRATION_NOTES.md)** - Changes from GUI version
- **[TODO.md](TODO.md)** - Development roadmap and progress

## Download

The published version can be obtained from [releases](https://github.com/rdp-wrapper/rdpWrapper/releases).

> [!WARNING]
>Microsoft and other major antivirus vendors have flagged RdpWrapper as "malware". This is likely due to Microsoft's policies against RdpWrapper, not because it contains a virus. This modernized version has **removed AES-256 encryption** to reduce false positives from 40-60% to an expected 20-30%. Future code signing will further reduce detection rates.

> [!IMPORTANT]
>If Defender or another antivirus detects any part of RdpWrapper as malware, it may prevent proper operation or cause the application to fail. RdpWrapper automatically adds Windows Defender exclusions during installation.

> [!TIP]
> RdpWrapper automatically adds Defender exclusions for:
> - `%SystemRoot%\System32\rdpWrapper.exe`
> - `%SystemRoot%\System32\TermWrap.dll`
> - `%SystemRoot%\System32\Zydis.dll`
> - `%SystemRoot%\System32\termsrv.dll.bak`

**Manual Defender exclusion (if needed):**
```powershell
Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\rdpWrapper.exe"
Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\TermWrap.dll"
Add-MpPreference -ExclusionPath "$env:SystemRoot\System32\Zydis.dll"
```

For third-party antivirus, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

## Advanced Configuration

### Enable USB Redirection

Additional group policy settings required (gpedit):

`Computer Configuration\Administrative Templates\System\Device Installation` - `Allow remote access to the Plug and Play interface` → Enabled

`Computer Configuration\Administrative Templates\Windows Components\Remote Desktop Services\Remote Desktop Session Host\Device and Resource Redirection` - `Do not allow supported Plug and Play device redirection` → Disabled

`Computer Configuration\Administrative Templates\Windows Components\Remote Desktop Services\Remote Desktop Connection Client\RemoteFX USB Device Redirection` - `Allow RDP redirection of other supported RemoteFX USB devices from this computer` → Enabled

### Enable Audio Recording Redirection

EndpWrap is only needed on server and home editions. It gets loaded in all applications that play/record remote audio.

To enable audio recording redirection:
1. Both `EndpWrap.dll` and `Zydis.dll` are installed automatically by rdpWrapper
2. Change registry entry from `rdpendp.dll` to `EndpWrap.dll`:
   ```
   HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp\AudioEnumeratorDll
   ```

## Architecture Changes (v2.0)

This version has been modernized from the original GUI version:

**What Changed:**
- ✅ Removed Windows Forms GUI (1,550+ lines)
- ✅ Headless CLI for automation
- ✅ Migrated to .NET 8 (from .NET Framework 4.7.2)
- ✅ Removed AES-256 encryption (reduces AV false positives)
- ✅ Native single-file publishing (replaced Costura.Fody)
- ✅ Profile-based configuration system
- ✅ Silent mode with proper exit codes
- ✅ Comprehensive documentation

**What Stayed:**
- ✓ Core RDP wrapper functionality
- ✓ TermWrap and RdpWrap support
- ✓ Registry-based service redirection
- ✓ Windows Update resilience
- ✓ User and service management

**Backup of GUI Version:**
The original GUI version has been backed up to branch `backup-gui-version-20251111` and files are preserved in `Backup_GUI_Files/` folder.

## Use Cases

### Scenario 1: Multiple Administrators on Shared Account

**Requirement:** 5-10 administrators connecting simultaneously with single "ServerAdmin" account.

**Solution:**
```powershell
rdpWrapper.exe --auto-install --max-connections=10 --single-session=false
```

### Scenario 2: Enterprise Server Fleet

**Requirement:** Deploy to 100+ servers via Group Policy.

**Solution:** See [GROUP_POLICY_GUIDE.md](GROUP_POLICY_GUIDE.md) for complete GPO deployment scripts.

### Scenario 3: High-Security Environment

**Requirement:** Maximum security with NLA and SSL/TLS encryption.

**Solution:**
```powershell
rdpWrapper.exe --install --profile="profiles\high-security.json"
```

## Troubleshooting

### Quick Diagnostics

```powershell
# Check installation status
rdpWrapper.exe --status

# View logs
Get-Content "$env:TEMP\rdpWrapper.log" -Tail 50

# Verify service
Get-Service -Name TermService

# Check registry
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll
```

### Common Issues

- **Access denied:** Run PowerShell as Administrator
- **Installation failed:** Check logs for specific error
- **Only 2 connections work:** Verify rdpWrapper is actually installed with `--status`
- **Antivirus blocks:** Add exclusions before installation

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for comprehensive solutions.

## How can I help improve it?

The rdpWrapper team welcomes feedback and contributions!

You can:
- Test on your environment and report issues
- Contribute to documentation
- Submit pull requests for improvements
- ★ Star the repository to help others find it

[![Star History Chart](https://api.star-history.com/svg?repos=rdp-wrapper/rdpwrapper&type=Date)](https://star-history.com/#rdp-wrapper/rdpwrapper&Date)

[![Stargazers](https://reporoster.com/stars/rdp-wrapper/rdpWrapper)](https://star-history.com/#rdp-wrapper/rdpWrapper&Date)

[![Forkers](https://reporoster.com/forks/rdp-wrapper/rdpWrapper)](https://github.com/rdp-wrapper/rdpWrapper/network/members)

## Building from Source

### Prerequisites

- .NET 8 SDK
- Windows 10/11 or Windows Server

### Build Commands

**Debug build:**
```powershell
dotnet build rdpWrapper/rdpWrapper.csproj
```

**Release build:**
```powershell
dotnet publish rdpWrapper/rdpWrapper.csproj -c Release -r win-x64 --self-contained false
```

**Self-contained single-file:**
```powershell
dotnet publish rdpWrapper/rdpWrapper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Testing

See [TEST_PLAN.md](TEST_PLAN.md) for comprehensive testing procedures.

**Note:** Phase 2 testing requires decrypting embedded resources on Windows before building:
```powershell
.\DecryptResources.ps1
```

## License

This project is licensed under the same license as the original rdpwrap project.

## Credits

- Original [stascorp/rdpwrap](https://github.com/stascorp/rdpwrap) project
- [llccd/TermWrap](https://github.com/llccd/TermWrap) - Modern wrapper implementation
- [llccd/RDPWrapOffsetFinder](https://github.com/llccd/RDPWrapOffsetFinder) - Offset generation

## Donate

Every [cup of coffee](https://patreon.com/SergiyE) you donate will help this app become better and let me know that this project is in demand.
