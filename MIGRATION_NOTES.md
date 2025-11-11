# GUI to CLI Migration Documentation

## Current GUI Functionality Mapping

This document maps the current Windows Forms GUI features to the planned CLI interface.

### Core Features from MainForm.cs

#### 1. Installation/Uninstallation
- **GUI**: Install/Uninstall buttons
- **CLI**: `--install`, `--uninstall`, `--auto-install`
- **Code**: Wrapper.Install(), Wrapper.Uninstall()

#### 2. Wrapper Selection
- **GUI**: Menu > Wrapper to Install > (TermWrap/RdpWrap)
- **CLI**: `--wrapper=TermWrap` or `--wrapper=RdpWrap`
- **Default**: TermWrap (preferred)
- **Code**: preferredWrapper setting (lines 60-73)

#### 3. RDP Configuration Settings

##### Single Session Per User
- **GUI**: Checkbox "Single session per user"
- **CLI**: `--single-session=true|false`
- **Property**: Wrapper.SingleSessionPerUser
- **Default**: false (allow multiple sessions per user)
- **Registry**: fSingleSessionPerUser

##### Maximum Connections
- **GUI**: Numeric input "Maximum connections allowed"
- **CLI**: `--max-connections=N`
- **Property**: Wrapper.MaximumConnectionsAllowed
- **Default**: 10
- **Registry**: MaxInstanceCount

##### RDP Port
- **GUI**: Numeric input "RDP Port"
- **CLI**: `--port=N`
- **Property**: Wrapper.RdpPort
- **Default**: 3389
- **Registry**: PortNumber

##### Network Level Authentication (NLA)
- **GUI**: Radio buttons (3 options)
  - "GUI Authentication Only" = 0
  - "Default RDP Authentication" = 1
  - "Network Level Authentication" = 2
- **CLI**: `--nla=0|1|2`
- **Property**: Wrapper.UserAuthentication
- **Default**: 1
- **Registry**: UserAuthentication

##### Security Layer
- **GUI**: Related to NLA
- **CLI**: `--security-layer=0|1|2`
- **Property**: Wrapper.SecurityLayer
- **Default**: 2
- **Registry**: SecurityLayer

##### Shadow Options
- **GUI**: Radio buttons (5 options)
  - "Disable Shadowing" = 0
  - "Full access with user's permission" = 1
  - "Full access without permission" = 2
  - "View only with user's permission" = 3
  - "View only without permission" = 4
- **CLI**: `--shadow=0|1|2|3|4`
- **Property**: Wrapper.ShadowOptions
- **Default**: 0 (disabled)
- **Registry**: Shadow

##### USB Redirection
- **GUI**: Checkbox "Restrict USB redirection"
- **CLI**: `--restrict-usb=true|false`
- **Property**: Wrapper.RestrictUsbRedirection
- **Default**: null (not set)
- **Registry**: fUsbRedirectionEnableMode

##### Audio/Video Settings
- **GUI**: Checkboxes for:
  - Allow host playback redirect
  - Allow client video capture
  - Allow client audio capture
- **CLI**:
  - `--allow-host-audio=true|false`
  - `--allow-client-video=true|false`
  - `--allow-client-audio=true|false`
- **Properties**:
  - Wrapper.AllowHostPlaybackRedirect
  - Wrapper.AllowClientVideoCapture
  - Wrapper.AllowClientAudioCapture
- **Default**: false for all
- **Registry**: fDisableCam, fDisableCameraRedir, fDisableAudioCapture

##### PnP Redirection
- **GUI**: Checkbox "Allow PnP redirect"
- **CLI**: `--allow-pnp=true|false`
- **Property**: Wrapper.AllowPnpRedirect
- **Default**: false
- **Registry**: fDisablePNPRedir

#### 4. Service Control
- **GUI**: Start/Stop service buttons
- **CLI**: `--start`, `--stop`
- **Methods**: Wrapper.StartService(), Wrapper.StopService()

#### 5. Status Display
- **GUI**: Real-time status panel with colored indicators
- **CLI**: `--status` (one-time check and report)
- **Method**: Wrapper.CheckWrapperInstalled(), Wrapper.GetServiceState()

#### 6. Optional Features

##### Defender Exclusion
- **GUI**: Menu checkbox "Add Defender exclusion when installing"
- **CLI**: `--defender-exclusion` (flag)
- **Setting**: addDefenderExclusion
- **Default**: true
- **Code**: Wrapper.Install() parameter

##### Firewall Rule
- **GUI**: Menu checkbox "Add firewall rule when port changed"
- **CLI**: `--firewall-rule` (flag)
- **Setting**: setFirewallRule
- **Default**: true
- **Code**: MainForm firewall management

##### Show Antivirus Warning
- **GUI**: Menu checkbox "Show antivirus warning"
- **CLI**: Not needed (informational only)
- **Setting**: showAntivirusWarn

##### Config File Generation (RdpWrap only)
- **GUI**: "Generate config" button
- **CLI**: `--generate-config`
- **Method**: Wrapper.GenerateIniFile()
- **Note**: Only for RdpWrap, not TermWrap

#### 7. User Management (LocalUsersManager.cs)
- **GUI**: Menu > Create user
- **CLI**: `--create-user=USERNAME --password=PASSWORD`
- **Class**: LocalUsersManager
- **Note**: Creates user and adds to "Remote Desktop Users" group

#### 8. Settings Storage
- **GUI**: Menu checkbox "Store settings in file"
- **CLI**: Not needed (always use defaults or profiles)
- **Code**: PersistentSettings (portable mode)

#### 9. Logging
- **GUI**: Log panel (toggle visibility)
- **CLI**: `--log=FILE` or stdout by default
- **Class**: Logger

#### 10. Auto-Update
- **GUI**: Menu > Check for updates
- **CLI**: Not needed in enterprise context
- **Class**: Updater

---

## CLI Command Structure

### Auto-Install (Recommended for Enterprise)
```bash
rdpWrapper.exe --auto-install
```
**Defaults:**
- Wrapper: TermWrap
- Single session: false
- Max connections: 10
- Port: 3389
- NLA: 1 (Default RDP Authentication)
- Security layer: 2 (TLS)
- Shadow: 0 (disabled)
- Defender exclusion: true
- Firewall rule: true

### Custom Install
```bash
rdpWrapper.exe --install \
  --wrapper=TermWrap \
  --max-connections=20 \
  --port=3390 \
  --single-session=false \
  --nla=2 \
  --defender-exclusion \
  --firewall-rule \
  --silent
```

### Status Check
```bash
rdpWrapper.exe --status
```

### Uninstall
```bash
rdpWrapper.exe --uninstall --silent
```

### Service Control
```bash
rdpWrapper.exe --start
rdpWrapper.exe --stop
```

### User Management
```bash
rdpWrapper.exe --create-user=admin1 --password=SecurePass123
```

### Profile-Based
```bash
rdpWrapper.exe --install --profile=enterprise.json
```

---

## Features NOT Being Migrated

1. **Real-time status updates** - CLI will be one-time check only
2. **Log viewer panel** - Use `--log=file.txt` instead
3. **Theme support** - CLI doesn't need themes
4. **Settings persistence** - Use profiles or command-line args
5. **Auto-update** - Enterprise deployment handles updates
6. **Interactive UI** - Fully automated CLI

---

## Exit Codes

- **0**: Success
- **1**: General error
- **2**: Installation failed
- **3**: Service error
- **4**: Configuration error
- **5**: Insufficient permissions
- **6**: Unsupported OS

---

## Test Scenarios

### Test 1: Fresh Install
```bash
rdpWrapper.exe --auto-install
# Expected: Exit 0, RDP enabled, TermWrap installed
```

### Test 2: Custom Configuration
```bash
rdpWrapper.exe --install --max-connections=5 --port=3390
# Expected: Exit 0, custom settings applied
```

### Test 3: Multiple Concurrent Sessions
```bash
# After installation, test from 3 different clients connecting as same user
# Expected: All 3 sessions work simultaneously
```

### Test 4: Uninstall
```bash
rdpWrapper.exe --uninstall
# Expected: Exit 0, wrapper removed, RDP still functional
```

### Test 5: Status Check
```bash
rdpWrapper.exe --status
# Expected: Clear output showing installation state
```

---

## Notes

- All registry operations remain the same (Wrapper.cs)
- Core business logic unchanged (Wrapper.cs)
- Only UI layer removed (MainForm, InputForm)
- Logger adapted for console output
- No Windows Forms dependencies after migration
