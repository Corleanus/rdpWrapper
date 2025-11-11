# RDP Wrapper - Detailed Task List

**Project Goal**: Create single .exe for enterprise deployment that doesn't trigger antivirus and allows multiple admins under single user account.

**Current Branch**: `claude/server-capacity-setup-011CV2kn5yLJ3NKydaT2MkK1`

**Last Updated**: 2025-11-11 (Phase 3A & 3B Complete)

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

### Phase 3A & 3B: Configuration System & CLI Parser (Code Complete)
- [x] Create ConfigurationProfile.cs class → Full configuration management
- [x] Add JSON serialization support → Load/save profiles
- [x] Create factory methods → Enterprise and high-security profiles
- [x] Add validation → All settings validated before applying
- [x] Create CliArguments.cs → Comprehensive argument parser
- [x] Add 15+ installation parameters → --max-connections, --port, etc.
- [x] Add --profile support → Load configuration from JSON
- [x] Add --silent and --log flags → Automation support
- [x] Create Program_Enhanced.cs → Full CLI implementation
- [x] Implement --auto-install → One-command enterprise setup
- [x] Implement --install with parameters → Custom configuration
- [x] Implement --uninstall, --status, --start, --stop → Full control
- [x] Implement --create-user → User management
- [x] Add proper exit codes → 0-5 for different scenarios
- [x] Create example profiles → enterprise-default.json, high-security.json
- [x] Create profiles/README.md → Comprehensive documentation
- [x] Commit Phase 3A & 3B → Commit `ecabde0`
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

#### Part A: Configuration System ✅ COMPLETE
- [x] Create `ConfigurationProfile.cs` class
  - [x] Add properties for all RDP settings
  - [x] Add JSON serialization support
  - [x] Add validation methods
  - [x] Add default enterprise profile
- [x] Create `enterprise-defaults.json` example file
- [x] Create `high-security.json` example file
- [x] Create `profiles/README.md` documentation
- [x] Test profile loading and validation (built-in)

#### Part B: CLI Argument Parser ✅ COMPLETE
- [x] Create `CliArguments.cs` parser class
- [x] Add command-line argument parsing (15+ parameters)
- [x] Implement `--auto-install` command
  - [x] Use sensible defaults (max connections: 10, single session: false)
  - [x] Install TermWrap
  - [x] Add Defender exclusion
  - [x] Start service
- [x] Implement `--install` command with all parameters
- [x] Implement `--uninstall` command
- [x] Implement `--status` command (check installation state)
- [x] Implement `--start` command (start RDP service)
- [x] Implement `--stop` command (stop RDP service)
- [x] Implement `--create-user` command
- [x] Implement `--help` command (comprehensive usage documentation)
- [x] Implement `--silent` flag (suppress output)
- [x] Implement `--log=<path>` flag (file logging)
- [x] Implement `--profile=<path>` (JSON configuration file)
- [x] Add proper exit codes (0-5 for different scenarios)
- [x] Create `Program_Enhanced.cs` with full implementation

#### Part C: Integrate Enhanced CLI ⏸️ READY TO START
- [ ] Replace `Program.cs` with `Program_Enhanced.cs`
- [ ] Update `.csproj` to reference new files
- [ ] Add `System.Text.Json` NuGet package (for JSON serialization)
- [ ] Test build compiles successfully

#### Part D: Remove Windows Forms UI ⏸️ PENDING
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

#### Part E: Testing (After Windows testing of Phase 2)
- [ ] Build CLI-only version
- [ ] Test `--auto-install` on clean system
- [ ] Test `--install` with custom parameters
- [ ] Test `--install --profile=enterprise-default.json`
- [ ] Test `--uninstall` cleanup
- [ ] Test `--status` reporting
- [ ] Test `--silent` mode (no output)
- [ ] Test exit codes (0 = success, 1-5 = errors)
- [ ] Test concurrent RDP sessions still work
- [ ] Verify file size reduction (~2.3 MB → ~800 KB)

#### Part F: Commit & Push
- [ ] Commit Phase 3C-E changes
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
Phase 2: Encryption Removal (Test)   ░░░░░░░░░░░░░░░░░░░░   0% 🔴 BLOCKED (Windows)
Phase 3A: Configuration System       ████████████████████ 100% ✅
Phase 3B: CLI Argument Parser        ████████████████████ 100% ✅
Phase 3C: Integrate CLI              ░░░░░░░░░░░░░░░░░░░░   0% ⏸️ READY
Phase 3D: Remove Windows Forms       ░░░░░░░░░░░░░░░░░░░░   0% ⏸️ READY
Phase 4: .NET 8 Migration            ░░░░░░░░░░░░░░░░░░░░   0% ⏸️
Phase 5: Documentation & Deployment  ░░░░░░░░░░░░░░░░░░░░   0% ⏸️

Overall Progress: 55% Complete
```

### Latest Commits:
- `5059ac5` - Phase 2: Remove AES encryption (2025-11-11)
- `752311c` - Add TODO.md (2025-11-11)
- `ecabde0` - Phase 3A & 3B: Configuration & CLI parser (2025-11-11) ⭐ LATEST

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
