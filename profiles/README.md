# RDP Wrapper Configuration Profiles

This directory contains example configuration profiles for RDP Wrapper CLI.

## Usage

Use profiles with the `--profile` parameter:

```bash
rdpWrapper.exe --install --profile=enterprise-default.json
```

Or with full path:

```bash
rdpWrapper.exe --install --profile=C:\path\to\custom-profile.json
```

## Available Profiles

### enterprise-default.json
**Purpose**: Multiple administrators accessing servers with shared account

**Key Settings**:
- Multiple concurrent sessions per user (10 max)
- Standard RDP authentication
- TLS encryption
- Minimal redirection features (security)
- Defender exclusion enabled

**Use Case**: Company servers where multiple IT admins need simultaneous access using shared admin account.

### high-security.json
**Purpose**: Maximum security for sensitive environments

**Key Settings**:
- Single session per user
- Network Level Authentication required
- TLS encryption required
- Shadow only with user permission
- Hide last logged in username
- All redirection features disabled
- USB restricted to administrators only

**Use Case**: High-security environments, compliance requirements, production servers.

## Profile Settings Reference

### Wrapper Types
- `"TermWrap"` - Modern wrapper with auto-detection (recommended)
- `"RdpWrap"` - Legacy wrapper (requires rdpwrap.ini)

### NLA Levels
- `0` - GUI Authentication Only
- `1` - Default RDP Authentication (recommended)
- `2` - Network Level Authentication (most secure)

### Security Layers
- `0` - RDP Security
- `1` - Negotiate
- `2` - TLS (recommended)

### Shadow Options
- `0` - Disabled (most secure)
- `1` - Full access with user's permission
- `2` - Full access without permission
- `3` - View only with user's permission
- `4` - View only without permission

## Creating Custom Profiles

### Example: Custom Port with High Connection Limit

```json
{
  "name": "Custom High Capacity",
  "description": "Custom RDP port with 50 concurrent connections",
  "wrapper": "TermWrap",
  "rdpPort": 3390,
  "maxConnections": 50,
  "singleSessionPerUser": false,
  "allowTsConnections": true,
  "nlaLevel": 1,
  "securityLayer": 2,
  "shadowOptions": 0,
  "addDefenderExclusion": true,
  "addFirewallRule": true
}
```

### Example: Development/Test Environment

```json
{
  "name": "Development",
  "description": "Relaxed settings for development servers",
  "wrapper": "TermWrap",
  "rdpPort": 3389,
  "maxConnections": 20,
  "singleSessionPerUser": false,
  "allowTsConnections": true,
  "nlaLevel": 0,
  "securityLayer": 0,
  "shadowOptions": 2,
  "allowHostAudioPlayback": true,
  "allowClientVideoCapture": true,
  "allowClientAudioCapture": true,
  "allowPnpRedirect": true,
  "restrictUsbToAdmins": false,
  "addDefenderExclusion": true,
  "addFirewallRule": true
}
```

## Profile Validation

Profiles are validated on load. Common errors:

- **Invalid port**: Must be 1-65535
- **Invalid maxConnections**: Must be 0-999999
- **Invalid nlaLevel**: Must be 0, 1, or 2
- **Invalid securityLayer**: Must be 0, 1, or 2
- **Invalid shadowOptions**: Must be 0-4

## Profile Priority

When using `--install` with both profile and command-line parameters:

1. **Command-line parameters override profile settings**
   ```bash
   # Profile has port=3389, but command line overrides to 3390
   rdpWrapper.exe --install --profile=enterprise.json --port=3390
   ```

2. **Profile provides defaults for unspecified parameters**
   ```bash
   # Only port specified, other settings from profile
   rdpWrapper.exe --install --profile=enterprise.json --port=3390
   ```

## Best Practices

### Production Servers
- Use `high-security.json` as baseline
- Enable NLA (nlaLevel: 2)
- Enable TLS (securityLayer: 2)
- Limit connections (maxConnections: 5-10)
- Disable unnecessary redirection features

### Development Servers
- Use `enterprise-default.json` as baseline
- Higher connection limits OK (maxConnections: 20+)
- Can relax NLA if needed
- Audio/video redirection as needed

### Shared Admin Access (Your Use Case)
- Use `enterprise-default.json`
- **Critical**: `singleSessionPerUser: false`
- Set appropriate `maxConnections` for team size
- Keep security features enabled (NLA, TLS)

## Deployment with Group Policy

Store profiles on network share:

```powershell
# Deploy via GPO startup script
\\fileserver\scripts\rdpWrapper.exe --install --profile=\\fileserver\profiles\enterprise-default.json --silent
```

## Version Control

Store custom profiles in version control:

```
company-infrastructure/
├── rdp-profiles/
│   ├── production.json
│   ├── staging.json
│   └── development.json
└── deployment-scripts/
    └── deploy-rdp.ps1
```

## Support

For profile issues:
1. Validate JSON syntax (use jsonlint.com)
2. Check validation error messages
3. Refer to TEST_PLAN.md for testing procedures
4. See MIGRATION_NOTES.md for setting details
