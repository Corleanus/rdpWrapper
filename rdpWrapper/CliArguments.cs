using System;
using System.Collections.Generic;
using System.Linq;

namespace rdpWrapper {

  /// <summary>
  /// Command-line argument parser for RDP Wrapper
  /// </summary>
  internal class CliArguments {

    // Commands (mutually exclusive)
    public bool ShowHelp { get; private set; }
    public bool AutoInstall { get; private set; }
    public bool Install { get; private set; }
    public bool Uninstall { get; private set; }
    public bool Status { get; private set; }
    public bool Start { get; private set; }
    public bool Stop { get; private set; }
    public bool GenerateConfig { get; private set; }
    public bool CreateUser { get; private set; }

    // Install parameters
    public SupportedWrappers? Wrapper { get; private set; }
    public int? MaxConnections { get; private set; }
    public int? Port { get; private set; }
    public bool? SingleSession { get; private set; }
    public int? NlaLevel { get; private set; }
    public int? SecurityLayer { get; private set; }
    public int? ShadowOptions { get; private set; }
    public bool? AllowHostAudio { get; private set; }
    public bool? AllowClientVideo { get; private set; }
    public bool? AllowClientAudio { get; private set; }
    public bool? AllowPnp { get; private set; }
    public bool? RestrictUsb { get; private set; }

    // Flags
    public bool DefenderExclusion { get; private set; }
    public bool FirewallRule { get; private set; }
    public bool Silent { get; private set; }
    public bool Offline { get; private set; }

    // Values
    public string ProfilePath { get; private set; }
    public string LogPath { get; private set; }
    public string Username { get; private set; }
    public string Password { get; private set; }

    // Validation
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; }

    private CliArguments() { }

    /// <summary>
    /// Parse command-line arguments
    /// </summary>
    public static CliArguments Parse(string[] args) {
      var result = new CliArguments();

      if (args == null || args.Length == 0) {
        // No arguments = show GUI (legacy behavior)
        result.IsValid = true;
        return result;
      }

      try {
        for (int i = 0; i < args.Length; i++) {
          var arg = args[i].ToLower();

          // Commands
          if (arg == "--help" || arg == "-help" || arg == "-h" || arg == "-?") {
            result.ShowHelp = true;
          }
          else if (arg == "--auto-install") {
            result.AutoInstall = true;
          }
          else if (arg == "--install") {
            result.Install = true;
          }
          else if (arg == "--uninstall") {
            result.Uninstall = true;
          }
          else if (arg == "--status") {
            result.Status = true;
          }
          else if (arg == "--start") {
            result.Start = true;
          }
          else if (arg == "--stop") {
            result.Stop = true;
          }
          else if (arg == "--generate" || arg == "-generate") {
            result.GenerateConfig = true;
          }
          else if (arg == "--create-user") {
            result.CreateUser = true;
          }

          // Parameters with values
          else if (arg.StartsWith("--wrapper=")) {
            var value = arg.Substring("--wrapper=".Length);
            if (Enum.TryParse<SupportedWrappers>(value, true, out var wrapper)) {
              result.Wrapper = wrapper;
            }
            else {
              result.IsValid = false;
              result.ErrorMessage = $"Invalid wrapper type: {value}. Must be TermWrap or RdpWrap.";
              return result;
            }
          }
          else if (arg.StartsWith("--max-connections=")) {
            if (int.TryParse(arg.Substring("--max-connections=".Length), out var value)) {
              if (value >= 0 && value <= 999999) {
                result.MaxConnections = value;
              }
              else {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid max connections: {value}. Must be between 0 and 999999.";
                return result;
              }
            }
          }
          else if (arg.StartsWith("--port=")) {
            if (int.TryParse(arg.Substring("--port=".Length), out var value)) {
              if (value >= 1 && value <= 65535) {
                result.Port = value;
              }
              else {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid port: {value}. Must be between 1 and 65535.";
                return result;
              }
            }
          }
          else if (arg.StartsWith("--single-session=")) {
            var value = arg.Substring("--single-session=".Length).ToLower();
            result.SingleSession = value == "true" || value == "1" || value == "yes";
          }
          else if (arg.StartsWith("--nla=")) {
            if (int.TryParse(arg.Substring("--nla=".Length), out var value)) {
              if (value >= 0 && value <= 2) {
                result.NlaLevel = value;
              }
              else {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid NLA level: {value}. Must be 0, 1, or 2.";
                return result;
              }
            }
          }
          else if (arg.StartsWith("--security-layer=")) {
            if (int.TryParse(arg.Substring("--security-layer=".Length), out var value)) {
              if (value >= 0 && value <= 2) {
                result.SecurityLayer = value;
              }
              else {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid security layer: {value}. Must be 0, 1, or 2.";
                return result;
              }
            }
          }
          else if (arg.StartsWith("--shadow=")) {
            if (int.TryParse(arg.Substring("--shadow=".Length), out var value)) {
              if (value >= 0 && value <= 4) {
                result.ShadowOptions = value;
              }
              else {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid shadow options: {value}. Must be between 0 and 4.";
                return result;
              }
            }
          }
          else if (arg.StartsWith("--allow-host-audio=")) {
            var value = arg.Substring("--allow-host-audio=".Length).ToLower();
            result.AllowHostAudio = value == "true" || value == "1" || value == "yes";
          }
          else if (arg.StartsWith("--allow-client-video=")) {
            var value = arg.Substring("--allow-client-video=".Length).ToLower();
            result.AllowClientVideo = value == "true" || value == "1" || value == "yes";
          }
          else if (arg.StartsWith("--allow-client-audio=")) {
            var value = arg.Substring("--allow-client-audio=".Length).ToLower();
            result.AllowClientAudio = value == "true" || value == "1" || value == "yes";
          }
          else if (arg.StartsWith("--allow-pnp=")) {
            var value = arg.Substring("--allow-pnp=".Length).ToLower();
            result.AllowPnp = value == "true" || value == "1" || value == "yes";
          }
          else if (arg.StartsWith("--restrict-usb=")) {
            var value = arg.Substring("--restrict-usb=".Length).ToLower();
            result.RestrictUsb = value == "true" || value == "1" || value == "yes";
          }
          else if (arg.StartsWith("--profile=")) {
            result.ProfilePath = arg.Substring("--profile=".Length);
          }
          else if (arg.StartsWith("--log=")) {
            result.LogPath = arg.Substring("--log=".Length);
          }
          else if (arg.StartsWith("--username=") || arg.StartsWith("--user=")) {
            result.Username = arg.Contains("--username=")
              ? arg.Substring("--username=".Length)
              : arg.Substring("--user=".Length);
          }
          else if (arg.StartsWith("--password=") || arg.StartsWith("--pass=")) {
            result.Password = arg.Contains("--password=")
              ? arg.Substring("--password=".Length)
              : arg.Substring("--pass=".Length);
          }

          // Flags
          else if (arg == "--defender-exclusion" || arg == "--defender") {
            result.DefenderExclusion = true;
          }
          else if (arg == "--firewall-rule" || arg == "--firewall") {
            result.FirewallRule = true;
          }
          else if (arg == "--silent" || arg == "-s") {
            result.Silent = true;
          }
          else if (arg == "--offline" || arg == "-offline") {
            result.Offline = true;
          }

          // Unknown argument
          else if (arg.StartsWith("-")) {
            result.IsValid = false;
            result.ErrorMessage = $"Unknown argument: {arg}";
            return result;
          }
        }

        // Validate command combinations
        var commands = new List<bool> {
          result.ShowHelp, result.AutoInstall, result.Install, result.Uninstall,
          result.Status, result.Start, result.Stop, result.GenerateConfig,
          result.CreateUser
        };
        var commandCount = commands.Count(c => c);

        if (commandCount > 1) {
          result.IsValid = false;
          result.ErrorMessage = "Only one command can be specified at a time.";
          return result;
        }

        // Validate user creation
        if (result.CreateUser && string.IsNullOrWhiteSpace(result.Username)) {
          result.IsValid = false;
          result.ErrorMessage = "--create-user requires --username parameter.";
          return result;
        }

        result.IsValid = true;
        return result;
      }
      catch (Exception ex) {
        result.IsValid = false;
        result.ErrorMessage = $"Error parsing arguments: {ex.Message}";
        return result;
      }
    }

    /// <summary>
    /// Get help text
    /// </summary>
    public static string GetHelpText() {
      return @"
RDP Wrapper - Enterprise CLI for Remote Desktop Services
Version: " + typeof(Program).Assembly.GetName().Version.ToString(3) + @"

USAGE:
  rdpWrapper.exe [COMMAND] [OPTIONS]

COMMANDS:
  --auto-install              Install with enterprise defaults (recommended)
  --install                   Install wrapper with custom options
  --uninstall                 Uninstall wrapper and restore original RDP
  --status                    Show installation and service status
  --start                     Start RDP service
  --stop                      Stop RDP service
  --generate                  Generate rdpwrap.ini (RdpWrap only)
  --create-user               Create RDP user account
  --help                      Show this help message

INSTALL OPTIONS:
  --wrapper=TYPE              Wrapper type: TermWrap or RdpWrap (default: TermWrap)
  --max-connections=N         Maximum concurrent connections (default: 10)
  --port=N                    RDP port number (default: 3389)
  --single-session=BOOL       Single session per user (default: false)
  --nla=N                     NLA level: 0=GUI, 1=RDP, 2=NLA (default: 1)
  --security-layer=N          Security: 0=RDP, 1=Negotiate, 2=TLS (default: 2)
  --shadow=N                  Shadow: 0=Disabled, 1-4=Various levels (default: 0)
  --allow-host-audio=BOOL     Allow host audio playback redirect (default: false)
  --allow-client-video=BOOL   Allow client video capture (default: false)
  --allow-client-audio=BOOL   Allow client audio capture (default: false)
  --allow-pnp=BOOL            Allow PnP device redirection (default: false)
  --restrict-usb=BOOL         Restrict USB to admins only (default: true)
  --profile=PATH              Load settings from JSON file
  --defender-exclusion        Add Windows Defender exclusion
  --firewall-rule             Add firewall rule for RDP port

USER MANAGEMENT:
  --username=NAME             Username for --create-user
  --password=PASS             Password for --create-user

GLOBAL OPTIONS:
  --silent                    Suppress output (exit codes only)
  --log=PATH                  Write log to file
  --offline                   Skip update check

EXIT CODES:
  0   Success
  1   Invalid arguments
  2   Installation failed
  3   Service error
  4   Configuration error
  5   Insufficient permissions

EXAMPLES:

  Auto-install with defaults:
    rdpWrapper.exe --auto-install

  Install with custom port and connections:
    rdpWrapper.exe --install --port=3390 --max-connections=20

  Install using profile:
    rdpWrapper.exe --install --profile=enterprise-default.json

  Silent deployment:
    rdpWrapper.exe --auto-install --silent --log=C:\Logs\rdp-setup.log

  Check installation status:
    rdpWrapper.exe --status

  Create RDP user:
    rdpWrapper.exe --create-user --username=admin1 --password=SecurePass123

  Uninstall:
    rdpWrapper.exe --uninstall

PROFILES:
  Example profiles available in 'profiles/' directory:
  - enterprise-default.json   Multiple admins, shared account
  - high-security.json        Maximum security settings

  Command-line parameters override profile settings.

DOCUMENTATION:
  See README.md, MIGRATION_NOTES.md, and TEST_PLAN.md for details.

SUPPORT:
  https://github.com/rdp-wrapper/rdpWrapper
";
    }

    /// <summary>
    /// Check if any command was specified
    /// </summary>
    public bool HasCommand() {
      return ShowHelp || AutoInstall || Install || Uninstall || Status ||
             Start || Stop || GenerateConfig || CreateUser;
    }
  }
}
