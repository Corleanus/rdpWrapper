# RDP Wrapper - Detailed Task List

**Project Goal**: Create single .exe for enterprise deployment that doesn't trigger antivirus and allows multiple admins under single user account.

**Current Branch**: `claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1`

**Last Updated**: 2025-11-11

---

## ✅ COMPLETED

### Phase 1: Preparation & Backup
- [x] Create backup branch from current state → `backup-gui-version-20251111`
- [x] Document current GUI functionality for CLI mapping → `MIGRATION_NOTES.md`
- [x] Create test plan for validating headless mode → `TEST_PLAN.md`

### Phase 2: Remove Encryption (Code Complete)
- [x] Document encryption removal process → `ENCRYPTION_REMOVAL.md`
- [x] Create decryption helper script → `DecryptResources.ps1`
- [x] Remove GetAes() method from Wrapper.cs (lines 553-563)
- [x] Remove EncryptResources() method from Wrapper.cs (lines 565-586)
- [x] Remove aes field declaration from Wrapper class
- [x] Refactor ExtractResourceFile() to remove decryption (lines 588-616)
- [x] Remove System.Security.Cryptography using statement
- [x] Commit Phase 2 changes → Commit `5059ac5`
- [x] Push to remote repository

---

## 🔴 NEXT ACTIONS REQUIRED (Windows Machine)

### Phase 2: Testing & Finalization

**⚠️ CRITICAL: Project will not build until these steps are completed!**

#### Step 1: Decrypt Resources
```powershell
cd C:\path\to\rdpWrapper
.\DecryptResources.ps1
```
**Expected output**: 11 files decrypted successfully

#### Step 2: Update Project File
Edit `rdpWrapper\rdpWrapper.csproj`

**Find and replace** (11 occurrences):
```xml
<!-- FIND -->
<EmbeddedResource Include="externals\rdpwrap.ini.cr" />
<EmbeddedResource Include="externals\x86\rdpwrap.dll.cr" />
<EmbeddedResource Include="externals\x86\TermWrap.dll.cr" />
<EmbeddedResource Include="externals\x86\Zydis.dll.cr" />
<EmbeddedResource Include="externals\x86\RDPWrapOffsetFinder.exe.cr" />
<EmbeddedResource Include="externals\x64\rdpwrap.dll.cr" />
<EmbeddedResource Include="externals\x64\TermWrap.dll.cr" />
<EmbeddedResource Include="externals\x64\UmWrap.dll.cr" />
<EmbeddedResource Include="externals\x64\EndpWrap.dll.cr" />
<EmbeddedResource Include="externals\x64\Zydis.dll.cr" />
<EmbeddedResource Include="externals\x64\RDPWrapOffsetFinder.exe.cr" />

<!-- REPLACE WITH (remove .cr extension) -->
<EmbeddedResource Include="externals\rdpwrap.ini" />
<EmbeddedResource Include="externals\x86\rdpwrap.dll" />
<EmbeddedResource Include="externals\x86\TermWrap.dll" />
<EmbeddedResource Include="externals\x86\Zydis.dll" />
<EmbeddedResource Include="externals\x86\RDPWrapOffsetFinder.exe" />
<EmbeddedResource Include="externals\x64\rdpwrap.dll" />
<EmbeddedResource Include="externals\x64\TermWrap.dll" />
<EmbeddedResource Include="externals\x64\UmWrap.dll" />
<EmbeddedResource Include="externals\x64\EndpWrap.dll" />
<EmbeddedResource Include="externals\x64\Zydis.dll" />
<EmbeddedResource Include="externals\x64\RDPWrapOffsetFinder.exe" />
```

#### Step 3: Remove Encrypted Files
```powershell
Remove-Item .\rdpWrapper\externals\**\*.cr -Force
```

#### Step 4: Build Project
```powershell
dotnet build rdpWrapper.sln -c Release
# OR
msbuild rdpWrapper.sln /p:Configuration=Release
```
**Expected**: Build succeeds, executable ~2.3 MB

#### Step 5: Test Installation (Clean Windows VM Recommended)
```powershell
.\rdpWrapper\bin\Release\rdpWrapper.exe --install --wrapper=TermWrap --defender-exclusion
```

**Verify**:
- [ ] DLLs extracted to `C:\Program Files\RDP Wrapper\`
- [ ] Registry points to wrapper: `HKLM\SYSTEM\CurrentControlSet\Services\TermService\Parameters\ServiceDll`
- [ ] TermService running
- [ ] RDP connection works
- [ ] Multiple concurrent sessions work (test with 2-3 clients)

#### Step 6: Test Uninstallation
```powershell
.\rdpWrapper\bin\Release\rdpWrapper.exe --uninstall
```

**Verify**:
- [ ] Registry restored to `%SystemRoot%\System32\termsrv.dll`
- [ ] Folder `C:\Program Files\RDP Wrapper\` removed
- [ ] RDP still works (single session mode)

#### Step 7: Commit Finalized Files
```bash
git add rdpWrapper/rdpWrapper.csproj
git add rdpWrapper/externals/
git commit -m "Phase 2: Add decrypted resources and update project references"
git push
```

---

## 📋 PENDING WORK

### Phase 3: Create Headless CLI Application

**Goal**: Remove GUI, create command-line only tool for automated deployment

#### Part A: Configuration System
- [ ] Create `ConfigurationProfile.cs` class
  - [ ] Add properties for all RDP settings
  - [ ] Add JSON serialization support
  - [ ] Add validation methods
  - [ ] Add default enterprise profile
- [ ] Create `enterprise-defaults.json` example file
- [ ] Test profile loading and validation

#### Part B: CLI Argument Parser
- [ ] Refactor `Program.cs` Main() method
- [ ] Add command-line argument parsing
- [ ] Implement `--auto-install` command
  - [ ] Use sensible defaults (max connections: 10, single session: false)
  - [ ] Install TermWrap
  - [ ] Add Defender exclusion
  - [ ] Start service
- [ ] Implement `--install` command with parameters
  - [ ] `--wrapper=TermWrap|RdpWrap`
  - [ ] `--max-connections=N`
  - [ ] `--port=N`
  - [ ] `--single-session=true|false`
  - [ ] `--nla=0|1|2`
  - [ ] `--security-layer=0|1|2`
  - [ ] `--shadow=0|1|2|3|4`
  - [ ] `--defender-exclusion` (flag)
  - [ ] `--firewall-rule` (flag)
- [ ] Implement `--uninstall` command
- [ ] Implement `--status` command (check installation state)
- [ ] Implement `--start` command (start RDP service)
- [ ] Implement `--stop` command (stop RDP service)
- [ ] Implement `--help` command (usage documentation)
- [ ] Implement `--silent` flag (suppress output)
- [ ] Implement `--log=<path>` flag (file logging)
- [ ] Implement `--profile=<path>` (JSON configuration file)
- [ ] Add proper exit codes (0=success, 1=error, etc.)

#### Part C: Remove Windows Forms UI
- [ ] Remove `MainForm.cs` from project
- [ ] Remove `MainForm.Designer.cs` from project
- [ ] Remove `InputForm.cs` from project
- [ ] Remove `InputForm.Designer.cs` from project
- [ ] Remove Windows Forms references from `.csproj`
  - [ ] `System.Windows.Forms`
  - [ ] `System.Drawing`
- [ ] Change `OutputType` from `WinExe` to `Exe` in `.csproj`
- [ ] Remove `SergiyE.Common.UI` NuGet package
- [ ] Keep only `SergiyE.Common` (core utilities)
- [ ] Update `Program.cs` to remove GUI initialization
- [ ] Adapt `Logger` class for console output only

#### Part D: Testing
- [ ] Build CLI-only version
- [ ] Test `--auto-install` on clean system
- [ ] Test `--install` with custom parameters
- [ ] Test `--uninstall` cleanup
- [ ] Test `--status` reporting
- [ ] Test `--profile=enterprise.json`
- [ ] Test `--silent` mode (no output)
- [ ] Test exit codes
- [ ] Test concurrent RDP sessions still work
- [ ] Verify file size reduction (~2.3 MB → ~800 KB)

#### Part E: Commit & Push
- [ ] Commit Phase 3 changes
- [ ] Push to branch
- [ ] Test deployment via PowerShell script

---

### Phase 4: .NET 8 Migration

**Goal**: Modernize to latest .NET runtime for better performance

#### Part A: Project File Updates
- [ ] Update `TargetFramework` to `net8.0-windows` in `.csproj`
- [ ] Update NuGet packages to .NET 8 compatible versions
  - [ ] `Microsoft.CSharp`
  - [ ] `System.Data.DataSetExtensions`
  - [ ] `SergiyE.Common`
- [ ] Remove `Fody` and `Costura.Fody` (use native single-file publish)
- [ ] Add `PublishSingleFile=true` property
- [ ] Add `SelfContained=true` property
- [ ] Add `RuntimeIdentifier=win-x64` (or win-x86)

#### Part B: Code Modernization
- [ ] Fix any .NET 8 API compatibility issues
- [ ] Add `async`/`await` to service operations
  - [ ] `StartService()` → `StartServiceAsync()`
  - [ ] `StopService()` → `StopServiceAsync()`
- [ ] Add `async`/`await` to file operations
  - [ ] `ExtractResourceFile()` → `ExtractResourceFileAsync()`
- [ ] Update `Main()` to `async Task<int> Main(string[] args)`
- [ ] Replace synchronous I/O with async where appropriate

#### Part C: Build & Test
- [ ] Build with .NET 8 SDK
- [ ] Test single-file publish:
  ```bash
  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
  ```
- [ ] Verify file size (~500 KB target)
- [ ] Test on system without .NET 8 installed (self-contained)
- [ ] Run full test suite from Phase 2 & 3
- [ ] Measure startup time improvement
- [ ] Compare memory usage

#### Part D: Commit & Push
- [ ] Commit Phase 4 changes
- [ ] Push to branch

---

### Phase 5: Documentation & Deployment

**Goal**: Prepare for enterprise production deployment

#### Part A: Documentation
- [ ] Update `README.md` with CLI usage examples
- [ ] Create `ENTERPRISE_DEPLOYMENT.md`
  - [ ] Group Policy deployment
  - [ ] SCCM/Intune deployment
  - [ ] PowerShell DSC configuration
  - [ ] Batch deployment script examples
- [ ] Create `CODE_SIGNING.md`
  - [ ] How to obtain certificate
  - [ ] Signing process
  - [ ] SmartScreen reputation building
- [ ] Create `ROLLBACK.md`
  - [ ] Uninstall procedures
  - [ ] Restore original RDP functionality
  - [ ] Emergency recovery steps
- [ ] Create `FAQ.md`
  - [ ] Common issues
  - [ ] Troubleshooting
  - [ ] Windows Update compatibility

#### Part B: Deployment Scripts
- [ ] Create Group Policy startup script
- [ ] Create SCCM deployment package
- [ ] Create Intune deployment package
- [ ] Create standalone PowerShell deployment script
- [ ] Create verification script (check if installed correctly)

#### Part C: Code Signing (Optional but Recommended)
- [ ] Purchase code signing certificate (EV recommended)
- [ ] Set up signing process
- [ ] Sign all executables
- [ ] Test SmartScreen behavior
- [ ] Document certificate renewal process

#### Part D: Final Testing
- [ ] Deploy to 3-5 test servers
- [ ] Monitor for 1 week
- [ ] Collect feedback
- [ ] Fix any issues
- [ ] Upload to VirusTotal (verify AV detection rate)

#### Part E: Release
- [ ] Create release branch
- [ ] Tag version (e.g., v3.0.0-enterprise)
- [ ] Create GitHub release with binaries
- [ ] Update documentation
- [ ] Create deployment guide

---

## 📊 Progress Tracking

```
Phase 1: Preparation & Backup        ████████████████████ 100% ✅
Phase 2: Encryption Removal (Code)   ████████████████████ 100% ✅
Phase 2: Encryption Removal (Test)   ░░░░░░░░░░░░░░░░░░░░   0% 🔴 BLOCKED
Phase 3: Headless CLI                ░░░░░░░░░░░░░░░░░░░░   0% ⏸️
Phase 4: .NET 8 Migration            ░░░░░░░░░░░░░░░░░░░░   0% ⏸️
Phase 5: Documentation & Deployment  ░░░░░░░░░░░░░░░░░░░░   0% ⏸️

Overall Progress: 40% Complete
```

---

## 🎯 Expected Results (When Complete)

### Current Baseline
- **File Size**: 2.5 MB
- **AV Detection**: 40-60% (28-42 of 70+ engines)
- **Deployment**: Manual, requires user interaction
- **Configuration**: GUI-based

### After All Phases
- **File Size**: ~500 KB (80% reduction)
- **AV Detection**: ~5-10% with code signing (90% reduction)
- **Deployment**: Fully automated, one command
- **Configuration**: CLI parameters or JSON profiles
- **Runtime**: .NET 8 (modern, faster, more secure)
- **Maintenance**: Easier updates, better testing

---

## 🚨 Blockers

1. **Phase 2 Testing**: Requires Windows machine to decrypt resources
2. **Code Signing**: Requires certificate purchase (~$100-300/year)
3. **Enterprise Testing**: Requires test environment with multiple servers

---

## 📞 Questions for User

1. **Do you have Windows available** for Phase 2 testing?
2. **What's your timeline** for deployment?
3. **Do you want to proceed with Phase 3** while Phase 2 is being tested?
4. **Will you purchase code signing certificate**? (Highly recommended for enterprise)
5. **What deployment method** will you use? (GPO, SCCM, Intune, PowerShell)

---

## 🔗 Related Documentation

- `MIGRATION_NOTES.md` - GUI to CLI feature mapping
- `ENCRYPTION_REMOVAL.md` - Technical details of encryption removal
- `TEST_PLAN.md` - Comprehensive testing procedures (30+ tests)
- `NEXT_STEPS.md` - Detailed Phase 2 completion instructions
- `DecryptResources.ps1` - PowerShell decryption tool

---

## 💾 Git Information

- **Main Branch**: (check repo for default)
- **Working Branch**: `claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1`
- **Backup Branch**: `backup-gui-version-20251111`
- **Latest Commit**: `5059ac5` - "Phase 2: Remove AES encryption from embedded resources"
- **Remote**: `origin/claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1`

---

## 🔄 How to Resume Work

```bash
# Clone and checkout working branch
git clone https://github.com/Corleanus/rdpWrapper.git
cd rdpWrapper
git checkout claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1

# Continue with next pending task (see NEXT ACTIONS REQUIRED above)
```

---

**Last Updated**: 2025-11-11
**Status**: Awaiting Phase 2 Windows testing before proceeding to Phase 3
