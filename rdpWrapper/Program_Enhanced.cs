using sergiye.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;

namespace rdpWrapper {
  internal static class ProgramEnhanced {

    // Exit codes
    private const int CodeSuccess = 0;
    private const int CodeInvalidArgs = 1;
    private const int CodeInstallFailed = 2;
    private const int CodeServiceError = 3;
    private const int CodeConfigError = 4;
    private const int CodeInsufficientPermissions = 5;
    private const int CodeException = 99;

    private static int exitCode = CodeSuccess;
    private static FileLogger logger;
    private static bool isSilent = false;

    [STAThread]
    private static void Main(string[] args) {

      Crasher.Listen();

      // Parse command-line arguments
      var cliArgs = CliArguments.Parse(args);

      // Check if silent mode
      isSilent = cliArgs.Silent;

      // Initialize logger
      if (cliArgs.HasCommand() || args.Length > 0) {
        logger = string.IsNullOrWhiteSpace(cliArgs.LogPath)
          ? new FileLogger()
          : new FileLogger(cliArgs.LogPath);

        if (!isSilent) {
          logger.OnNewLogEvent += AddToLog;
        }
      }

      // Validate arguments
      if (!cliArgs.IsValid) {
        LogError(cliArgs.ErrorMessage);
        if (!isSilent) {
          Console.WriteLine("\nUse --help for usage information.");
        }
        Environment.Exit(CodeInvalidArgs);
      }

      // Show help
      if (cliArgs.ShowHelp) {
        Console.WriteLine(CliArguments.GetHelpText());
        Environment.Exit(CodeSuccess);
      }

      // Check OS compatibility
      if (!OperatingSystemHelper.IsCompatible(true, out var errorMessage, out var fixAction)) {
        if (cliArgs.HasCommand()) {
          LogError(errorMessage);
        }
        else {
          if (fixAction != null) {
            if (MessageBox.Show(errorMessage, Updater.ApplicationName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
              fixAction?.Invoke();
            }
          }
          else {
            MessageBox.Show(errorMessage, Updater.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
          }
        }
        Environment.Exit(CodeInsufficientPermissions);
      }

      // Check architecture compatibility
      if (Environment.Is64BitOperatingSystem != Environment.Is64BitProcess) {
        if (cliArgs.HasCommand()) {
          LogError($"Architecture mismatch: Running {(Environment.Is64BitProcess ? "x64" : "x86")} on {(Environment.Is64BitOperatingSystem ? "x64" : "x86")} OS.");
        }
        else {
          if (MessageBox.Show($"You are running an application build made for a different OS architecture.\nIt is not compatible!\nWould you like to download correct version?",
              Updater.ApplicationName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
            Updater.VisitAppSite("releases");
          }
        }
        Environment.Exit(CodeConfigError);
      }

      // Check for updates (unless offline mode)
      if (cliArgs.HasCommand() && !cliArgs.Offline) {
        try {
          Updater.CheckForUpdates(Updater.CheckUpdatesMode.NotifyOnNewVersion);
        }
        catch {
          // Ignore update check failures
        }
      }

      // Execute command
      if (cliArgs.HasCommand()) {
        try {
          ExecuteCommand(cliArgs);
        }
        catch (UnauthorizedAccessException) {
          LogError("Access denied. Please run as Administrator.");
          exitCode = CodeInsufficientPermissions;
        }
        catch (Exception ex) {
          LogError($"Error: {ex.Message}");
          exitCode = CodeException;
        }
        finally {
          logger?.Dispose();
        }
      }
      else {
        // No command = Start GUI (legacy behavior)
        // Check single instance
        if (WinApiHelper.CheckRunningInstances(true, true)) {
          MessageBox.Show($"{Updater.ApplicationName} is already running.", Updater.ApplicationName,
            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
          Environment.Exit(CodeSuccess);
        }

        StartWinForms();
      }

      Environment.Exit(exitCode);
    }

    private static void ExecuteCommand(CliArguments args) {

      LogInfo($"{Updater.ApplicationTitle} v{typeof(Program).Assembly.GetName().Version.ToString(3)} {(Environment.Is64BitProcess ? "x64" : "x86")}");

      if (args.AutoInstall) {
        ExecuteAutoInstall(args);
      }
      else if (args.Install) {
        ExecuteInstall(args);
      }
      else if (args.Uninstall) {
        ExecuteUninstall();
      }
      else if (args.Status) {
        ExecuteStatus();
      }
      else if (args.Start) {
        ExecuteStartService();
      }
      else if (args.Stop) {
        ExecuteStopService();
      }
      else if (args.GenerateConfig) {
        ExecuteGenerateConfig();
      }
      else if (args.CreateUser) {
        ExecuteCreateUser(args);
      }
      else if (args.StartUI) {
        ExecuteStartUI();
      }
    }

    private static void ExecuteAutoInstall(CliArguments args) {
      LogInfo("Starting auto-install with enterprise defaults...");

      var wrapper = new Wrapper(logger);
      var profile = ConfigurationProfile.CreateEnterpriseDefault();

      // Apply command-line overrides to profile
      ApplyCommandLineOverrides(args, profile);

      // Validate profile
      if (!profile.Validate(out var validationError)) {
        LogError($"Configuration validation failed: {validationError}");
        exitCode = CodeConfigError;
        return;
      }

      // Install wrapper
      try {
        LogInfo($"Installing {profile.Wrapper} wrapper...");
        wrapper.Install(profile.Wrapper, profile.AddDefenderExclusion);

        // Apply configuration
        LogInfo("Applying configuration...");
        profile.ApplyToWrapper(wrapper);

        LogInfo("Installation completed successfully!");
        LogInfo($"\nRDP Configuration:");
        LogInfo($"  Port: {profile.RdpPort}");
        LogInfo($"  Max Connections: {profile.MaxConnections}");
        LogInfo($"  Single Session: {profile.SingleSessionPerUser}");
        LogInfo($"  NLA Level: {profile.NlaLevel}");
        LogInfo($"\n✓ Multiple administrators can now connect simultaneously");

        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Installation failed: {ex.Message}");
        exitCode = CodeInstallFailed;
      }
    }

    private static void ExecuteInstall(CliArguments args) {
      LogInfo("Starting installation...");

      var wrapper = new Wrapper(logger);
      ConfigurationProfile profile;

      // Load profile if specified
      if (!string.IsNullOrWhiteSpace(args.ProfilePath)) {
        try {
          LogInfo($"Loading profile: {args.ProfilePath}");
          profile = ConfigurationProfile.LoadFromFile(args.ProfilePath);
        }
        catch (Exception ex) {
          LogError($"Failed to load profile: {ex.Message}");
          exitCode = CodeConfigError;
          return;
        }
      }
      else {
        // Use default profile
        profile = ConfigurationProfile.CreateEnterpriseDefault();
      }

      // Apply command-line overrides
      ApplyCommandLineOverrides(args, profile);

      // Validate
      if (!profile.Validate(out var validationError)) {
        LogError($"Configuration validation failed: {validationError}");
        exitCode = CodeConfigError;
        return;
      }

      // Install
      try {
        wrapper.Install(profile.Wrapper, args.DefenderExclusion || profile.AddDefenderExclusion);
        profile.ApplyToWrapper(wrapper);

        LogInfo("Installation completed successfully!");
        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Installation failed: {ex.Message}");
        exitCode = CodeInstallFailed;
      }
    }

    private static void ExecuteUninstall() {
      LogInfo("Starting uninstallation...");

      try {
        var wrapper = new Wrapper(logger);
        wrapper.Uninstall();
        LogInfo("Uninstallation completed successfully!");
        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Uninstallation failed: {ex.Message}");
        exitCode = CodeInstallFailed;
      }
    }

    private static void ExecuteStatus() {
      try {
        var wrapper = new Wrapper(logger);
        var state = wrapper.CheckWrapperInstalled();
        var serviceState = wrapper.GetServiceState();

        Console.WriteLine("\nRDP Wrapper Status:");
        Console.WriteLine("==================");
        Console.WriteLine($"Installation: {state}");

        if (state != WrapperInstalledState.NotInstalled && state != WrapperInstalledState.Unknown) {
          Console.WriteLine($"Wrapper Path: {wrapper.WrapperPath}");
        }

        Console.WriteLine($"Service: {serviceState?.ToString() ?? "Unknown"}");

        if (state == WrapperInstalledState.TermWrap || state == WrapperInstalledState.RdpWrap) {
          Console.WriteLine($"RDP Port: {wrapper.RdpPort}");
          Console.WriteLine($"Max Connections: {wrapper.MaximumConnectionsAllowed}");
          Console.WriteLine($"Single Session: {wrapper.SingleSessionPerUser}");
          Console.WriteLine($"NLA Level: {wrapper.UserAuthentication}");
          Console.WriteLine($"Security Layer: {wrapper.SecurityLayer}");
        }

        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Failed to get status: {ex.Message}");
        exitCode = CodeException;
      }
    }

    private static void ExecuteStartService() {
      try {
        var wrapper = new Wrapper(logger);
        LogInfo("Starting RDP service...");
        wrapper.StartService(TimeSpan.FromSeconds(30));
        LogInfo("Service started successfully!");
        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Failed to start service: {ex.Message}");
        exitCode = CodeServiceError;
      }
    }

    private static void ExecuteStopService() {
      try {
        var wrapper = new Wrapper(logger);
        LogInfo("Stopping RDP service...");
        wrapper.StopService(TimeSpan.FromSeconds(30));
        LogInfo("Service stopped successfully!");
        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Failed to stop service: {ex.Message}");
        exitCode = CodeServiceError;
      }
    }

    private static void ExecuteGenerateConfig() {
#if LITEVERSION
      LogInfo("Config generation not needed with TermWrap.");
#else
      try {
        var wrapper = new Wrapper(logger);
        string iniPath = Path.Combine(wrapper.WrapperFolderPath, Wrapper.RdpWrapIniName);
        wrapper.GenerateIniFile(iniPath, true);
        LogInfo("Config generated successfully!");
        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Failed to generate config: {ex.Message}");
        exitCode = CodeException;
      }
#endif
    }

    private static void ExecuteCreateUser(CliArguments args) {
      try {
        LogInfo($"Creating user: {args.Username}");

        var manager = new LocalUsersManager();
        var password = args.Password ?? GenerateRandomPassword();

        manager.CreateUser(args.Username, password);

        LogInfo($"User created successfully!");
        if (string.IsNullOrWhiteSpace(args.Password)) {
          LogInfo($"Generated password: {password}");
        }

        exitCode = CodeSuccess;
      }
      catch (Exception ex) {
        LogError($"Failed to create user: {ex.Message}");
        exitCode = CodeException;
      }
    }

    private static void ExecuteStartUI() {
      LogInfo("Starting UI in new process...");
      Process.Start(typeof(Program).Assembly.Location);
      exitCode = CodeSuccess;
    }

    private static void ApplyCommandLineOverrides(CliArguments args, ConfigurationProfile profile) {
      if (args.Wrapper.HasValue) profile.Wrapper = args.Wrapper.Value;
      if (args.MaxConnections.HasValue) profile.MaxConnections = args.MaxConnections.Value;
      if (args.Port.HasValue) profile.RdpPort = args.Port.Value;
      if (args.SingleSession.HasValue) profile.SingleSessionPerUser = args.SingleSession.Value;
      if (args.NlaLevel.HasValue) profile.NlaLevel = args.NlaLevel.Value;
      if (args.SecurityLayer.HasValue) profile.SecurityLayer = args.SecurityLayer.Value;
      if (args.ShadowOptions.HasValue) profile.ShadowOptions = args.ShadowOptions.Value;
      if (args.AllowHostAudio.HasValue) profile.AllowHostAudioPlayback = args.AllowHostAudio.Value;
      if (args.AllowClientVideo.HasValue) profile.AllowClientVideoCapture = args.AllowClientVideo.Value;
      if (args.AllowClientAudio.HasValue) profile.AllowClientAudioCapture = args.AllowClientAudio.Value;
      if (args.AllowPnp.HasValue) profile.AllowPnpRedirect = args.AllowPnp.Value;
      if (args.RestrictUsb.HasValue) profile.RestrictUsbToAdmins = args.RestrictUsb.Value;
      if (args.DefenderExclusion) profile.AddDefenderExclusion = true;
      if (args.FirewallRule) profile.AddFirewallRule = true;
    }

    private static string GenerateRandomPassword() {
      const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
      var random = new Random();
      return new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static void StartWinForms() {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      using var form = new MainForm();
      form.FormClosed += delegate {
        Application.Exit();
      };
      Application.Run(form);
    }

    private static void LogInfo(string message) {
      if (isSilent) return;
      logger?.Log(message, Logger.StateKind.Info);
    }

    private static void LogError(string message) {
      if (isSilent) return;
      logger?.Log(message, Logger.StateKind.Error);
    }

    private static void AddToLog(string message, Logger.StateKind state, bool newLine) {
      if (newLine) {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"\n{DateTime.Now:T} - ");
      }

      switch (state) {
        case Logger.StateKind.Error:
          Console.ForegroundColor = ConsoleColor.Red;
          Console.Write(message);
          break;
        case Logger.StateKind.Info:
          Console.ForegroundColor = ConsoleColor.White;
          Console.Write(message);
          break;
        default:
          Console.ForegroundColor = ConsoleColor.Gray;
          Console.Write(message);
          break;
      }
      Console.ResetColor();
    }
  }
}
