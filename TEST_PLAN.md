# RDP Wrapper CLI - Test Plan

## Test Environment Requirements

### Test Systems
- **Windows 10 Pro** (clean VM)
- **Windows 11 Pro** (clean VM)
- **Windows Server 2019** (clean VM)
- **Windows Server 2022** (clean VM)

### Prerequisites
- Administrator access
- Clean systems (no existing RDP modifications)
- Network connectivity for multiple RDP client tests
- RDP clients for testing (Remote Desktop Connection)

---

## Phase 2 Testing: Encryption Removal

### Test 2.1: Build Verification
**Objective**: Verify project builds with plain resources

**Steps**:
1. Run `dotnet build` or `msbuild`
2. Check for compilation errors
3. Verify output executable size

**Expected Results**:
- ✅ Build succeeds with no errors
- ✅ Executable size ~2.3 MB (slightly smaller than 2.5 MB)
- ✅ No encryption-related warnings

**Pass/Fail**: ___________

---

### Test 2.2: Resource Extraction
**Objective**: Verify DLLs extract correctly without encryption

**Steps**:
1. Run `rdpWrapper.exe --install`
2. Check `C:\Program Files\RDP Wrapper\` for extracted files
3. Verify file integrity

**Expected Results**:
- ✅ TermWrap.dll extracted
- ✅ Zydis.dll extracted
- ✅ UmWrap.dll extracted (x64 only)
- ✅ EndpWrap.dll extracted (x64 only)
- ✅ Files are valid DLLs (not corrupted)

**Verification Command**:
```powershell
Get-ChildItem "C:\Program Files\RDP Wrapper\" | Select-Object Name, Length
```

**Pass/Fail**: ___________

---

### Test 2.3: Installation Success
**Objective**: Verify wrapper installs correctly

**Steps**:
1. Run `rdpWrapper.exe --install --wrapper=TermWrap --defender-exclusion`
2. Check registry: `HKLM\SYSTEM\CurrentControlSet\Services\TermService\Parameters\ServiceDll`
3. Check service status

**Expected Results**:
- ✅ Registry points to `C:\Program Files\RDP Wrapper\TermWrap.dll`
- ✅ TermService is running
- ✅ No errors in event log

**Verification Command**:
```powershell
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll
Get-Service TermService
```

**Pass/Fail**: ___________

---

### Test 2.4: RDP Functionality
**Objective**: Verify RDP works after installation

**Steps**:
1. From another computer, connect via RDP
2. Authenticate with valid user
3. Verify session establishes

**Expected Results**:
- ✅ RDP connection succeeds
- ✅ Desktop loads properly
- ✅ No authentication errors

**Pass/Fail**: ___________

---

### Test 2.5: Multiple Concurrent Sessions
**Objective**: Verify multiple admins can connect to same user account

**Steps**:
1. Create test user: `net user testadmin Password123! /add`
2. Add to RDP group: `net localgroup "Remote Desktop Users" testadmin /add`
3. Configure: `rdpWrapper.exe --install --single-session=false --max-connections=5`
4. From 3 different computers, connect as `testadmin`

**Expected Results**:
- ✅ All 3 connections succeed simultaneously
- ✅ Each gets separate session
- ✅ No "session already in use" errors

**Verification Command**:
```powershell
qwinsta  # Should show 3+ sessions
```

**Pass/Fail**: ___________

---

### Test 2.6: Uninstallation
**Objective**: Verify clean uninstallation

**Steps**:
1. Run `rdpWrapper.exe --uninstall`
2. Check registry restored
3. Check folder removed
4. Check RDP still works (native)

**Expected Results**:
- ✅ Registry points back to `%SystemRoot%\System32\termsrv.dll`
- ✅ `C:\Program Files\RDP Wrapper\` deleted
- ✅ TermService restarts successfully
- ✅ RDP works (single session mode)

**Verification Command**:
```powershell
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\TermService\Parameters" -Name ServiceDll
Test-Path "C:\Program Files\RDP Wrapper\"  # Should return False
```

**Pass/Fail**: ___________

---

### Test 2.7: Antivirus Scanning
**Objective**: Measure improvement in AV detection

**Steps**:
1. Upload executable to VirusTotal
2. Count detections
3. Compare to original version

**Expected Results**:
- ✅ Fewer detections than encrypted version
- ✅ Target: <30% detection rate (down from 40-60%)

**VirusTotal URL**: ___________

**Detection Count**: _____ / 70+

**Pass/Fail**: ___________

---

## Phase 3 Testing: Headless CLI

### Test 3.1: Auto-Install Command
**Objective**: Test one-command installation

**Steps**:
1. Clean system (no wrapper installed)
2. Run `rdpWrapper.exe --auto-install`
3. Verify all defaults applied

**Expected Results**:
- ✅ Installs TermWrap
- ✅ Sets max connections = 10
- ✅ Sets single-session = false
- ✅ Port = 3389
- ✅ Adds Defender exclusion
- ✅ Exit code = 0

**Pass/Fail**: ___________

---

### Test 3.2: Custom Parameters
**Objective**: Test custom configuration

**Command**:
```bash
rdpWrapper.exe --install --wrapper=TermWrap --max-connections=20 --port=3390 --single-session=false --nla=2 --silent
```

**Expected Results**:
- ✅ Max connections = 20
- ✅ Port = 3390
- ✅ Single session = false
- ✅ NLA = 2
- ✅ No output (silent mode)
- ✅ Exit code = 0

**Verification Command**:
```powershell
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp" -Name PortNumber,MaxInstanceCount
```

**Pass/Fail**: ___________

---

### Test 3.3: Status Command
**Objective**: Test status reporting

**Command**:
```bash
rdpWrapper.exe --status
```

**Expected Output**:
```
RDP Wrapper Status:
  Installation: TermWrap v2.8.x
  Service: Running
  Wrapper: Active
  Port: 3389
  Max Connections: 10
  Single Session: Disabled
```

**Pass/Fail**: ___________

---

### Test 3.4: Profile-Based Install
**Objective**: Test JSON configuration profile

**Steps**:
1. Create `enterprise.json`:
```json
{
  "wrapper": "TermWrap",
  "maxConnections": 15,
  "rdpPort": 3389,
  "singleSessionPerUser": false,
  "nlaLevel": 1,
  "addDefenderExclusion": true
}
```
2. Run `rdpWrapper.exe --install --profile=enterprise.json`
3. Verify settings

**Expected Results**:
- ✅ All settings from JSON applied
- ✅ Exit code = 0

**Pass/Fail**: ___________

---

### Test 3.5: Silent Deployment
**Objective**: Test scripted deployment

**PowerShell Script**:
```powershell
# Deploy to multiple servers
$servers = @("SERVER01", "SERVER02", "SERVER03")
foreach ($srv in $servers) {
    Copy-Item rdpWrapper.exe "\\$srv\C$\Temp\"
    Invoke-Command -ComputerName $srv -ScriptBlock {
        C:\Temp\rdpWrapper.exe --auto-install --silent --log=C:\Temp\rdp-install.log
    }
}
```

**Expected Results**:
- ✅ Deploys to all 3 servers
- ✅ No errors
- ✅ All servers have wrapper installed
- ✅ Log files created

**Pass/Fail**: ___________

---

### Test 3.6: Error Handling
**Objective**: Test graceful failure scenarios

**Test 3.6a: Already Installed**
```bash
rdpWrapper.exe --install  # Run twice
```
**Expected**: Warning message, exit code 0 (idempotent)

**Test 3.6b: Non-Admin User**
```bash
# Run as standard user
rdpWrapper.exe --install
```
**Expected**: Error message "Administrator required", exit code 5

**Test 3.6c: Invalid Parameters**
```bash
rdpWrapper.exe --install --max-connections=999999
```
**Expected**: Error message "Invalid value", exit code 4

**Pass/Fail**: ___________

---

## Phase 4 Testing: .NET 8 Migration

### Test 4.1: Build on .NET 8
**Objective**: Verify .NET 8 compilation

**Steps**:
1. Run `dotnet build -c Release`
2. Check for errors
3. Verify executable size

**Expected Results**:
- ✅ Build succeeds
- ✅ Size ~500 KB (significantly smaller)
- ✅ No framework warnings

**Pass/Fail**: ___________

---

### Test 4.2: Single-File Publish
**Objective**: Test self-contained deployment

**Command**:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Expected Results**:
- ✅ Single .exe produced
- ✅ No external dependencies
- ✅ Runs on system without .NET 8 installed

**Pass/Fail**: ___________

---

### Test 4.3: Performance Comparison
**Objective**: Measure startup time improvement

**Test**:
```powershell
Measure-Command { .\rdpWrapper.exe --status }
```

**Expected Results**:
- ✅ Startup time < 1 second
- ✅ Faster than .NET Framework version

**.NET Framework 4.7.2 Time**: _____ ms
**.NET 8 Time**: _____ ms

**Pass/Fail**: ___________

---

### Test 4.4: Compatibility Check
**Objective**: Verify all features work on .NET 8

**Steps**:
1. Run full test suite from Phase 2 & 3
2. Verify no regressions

**Expected Results**:
- ✅ All Phase 2 tests pass
- ✅ All Phase 3 tests pass
- ✅ No new issues

**Pass/Fail**: ___________

---

## Integration Testing

### Integration Test 1: Enterprise Deployment Scenario
**Objective**: Simulate real-world company deployment

**Scenario**:
```
Company has 100 Windows servers
Need RDP with multiple admin concurrent access
Deploy via Group Policy startup script
```

**Steps**:
1. Create GPO with startup script
2. Deploy to test OU with 3 servers
3. Reboot servers
4. Verify installation on all
5. Test concurrent admin access

**Expected Results**:
- ✅ All servers get wrapper installed
- ✅ Consistent configuration
- ✅ Multiple admins can connect
- ✅ No service interruptions

**Pass/Fail**: ___________

---

### Integration Test 2: Windows Update Survival
**Objective**: Verify wrapper survives Windows updates

**Steps**:
1. Install wrapper
2. Run Windows Update (simulate monthly patching)
3. Verify wrapper still works

**Expected Results**:
- ✅ Wrapper remains active after update
- ✅ Registry still points to wrapper
- ✅ RDP continues working
- ✅ Multiple sessions still possible

**Pass/Fail**: ___________

---

## Security Testing

### Security Test 1: File Permissions
**Objective**: Verify proper security on installed files

**Command**:
```powershell
Get-Acl "C:\Program Files\RDP Wrapper" | Format-List
```

**Expected Results**:
- ✅ SYSTEM has full control
- ✅ Administrators have full control
- ✅ Users have read/execute only
- ✅ No world-writable permissions

**Pass/Fail**: ___________

---

### Security Test 2: Registry Permissions
**Objective**: Verify registry modifications are secure

**Expected Results**:
- ✅ Only admins can modify ServiceDll key
- ✅ Standard users can't tamper with settings

**Pass/Fail**: ___________

---

## Performance Testing

### Performance Test 1: Session Limit
**Objective**: Test maximum concurrent connections

**Steps**:
1. Configure `--max-connections=50`
2. Simulate 50 concurrent RDP sessions
3. Monitor system resources

**Expected Results**:
- ✅ All 50 sessions establish
- ✅ No crashes or hangs
- ✅ System remains responsive

**Pass/Fail**: ___________

---

## Rollback Testing

### Rollback Test 1: Uninstall After Failure
**Objective**: Verify clean uninstall even if install failed

**Steps**:
1. Simulate installation failure (kill process mid-install)
2. Run `rdpWrapper.exe --uninstall`
3. Verify system restored

**Expected Results**:
- ✅ Uninstall completes
- ✅ Registry restored
- ✅ Partial files removed
- ✅ RDP works (native mode)

**Pass/Fail**: ___________

---

## Final Sign-Off

### Phase 2 (Encryption Removal): ☐ PASS ☐ FAIL
**Sign-off**: _____________ Date: _______

### Phase 3 (Headless CLI): ☐ PASS ☐ FAIL
**Sign-off**: _____________ Date: _______

### Phase 4 (.NET 8 Migration): ☐ PASS ☐ FAIL
**Sign-off**: _____________ Date: _______

### Ready for Production: ☐ YES ☐ NO
**Sign-off**: _____________ Date: _______

---

## Notes / Issues Found

_______________________________________________
_______________________________________________
_______________________________________________
