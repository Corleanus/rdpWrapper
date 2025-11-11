using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace rdpWrapper {

  /// <summary>
  /// Configuration profile for RDP Wrapper settings.
  /// Can be loaded from JSON file or created programmatically.
  /// </summary>
  public class ConfigurationProfile {

    /// <summary>
    /// Profile name/description
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Default Profile";

    /// <summary>
    /// Profile description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Wrapper type to install (TermWrap or RdpWrap)
    /// </summary>
    [JsonPropertyName("wrapper")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SupportedWrappers Wrapper { get; set; } = SupportedWrappers.TermWrap;

    /// <summary>
    /// RDP port number (default: 3389)
    /// </summary>
    [JsonPropertyName("rdpPort")]
    public int RdpPort { get; set; } = 3389;

    /// <summary>
    /// Maximum concurrent connections allowed (0 = unlimited/default)
    /// </summary>
    [JsonPropertyName("maxConnections")]
    public int MaxConnections { get; set; } = 10;

    /// <summary>
    /// Allow only single session per user
    /// </summary>
    [JsonPropertyName("singleSessionPerUser")]
    public bool SingleSessionPerUser { get; set; } = false;

    /// <summary>
    /// Allow Terminal Services connections
    /// </summary>
    [JsonPropertyName("allowTsConnections")]
    public bool AllowTsConnections { get; set; } = true;

    /// <summary>
    /// Honor legacy settings
    /// </summary>
    [JsonPropertyName("honorLegacy")]
    public bool HonorLegacy { get; set; } = false;

    /// <summary>
    /// Network Level Authentication level (0=GUI only, 1=Default RDP, 2=NLA required)
    /// </summary>
    [JsonPropertyName("nlaLevel")]
    public int NlaLevel { get; set; } = 1;

    /// <summary>
    /// Security layer (0=RDP, 1=Negotiate, 2=TLS)
    /// </summary>
    [JsonPropertyName("securityLayer")]
    public int SecurityLayer { get; set; } = 2;

    /// <summary>
    /// Shadow options (0=Disabled, 1=Full with permission, 2=Full without, 3=View with permission, 4=View without)
    /// </summary>
    [JsonPropertyName("shadowOptions")]
    public int ShadowOptions { get; set; } = 0;

    /// <summary>
    /// Don't display last logged in username
    /// </summary>
    [JsonPropertyName("dontDisplayLastUser")]
    public bool DontDisplayLastUser { get; set; } = false;

    /// <summary>
    /// Allow host audio playback redirection
    /// </summary>
    [JsonPropertyName("allowHostAudioPlayback")]
    public bool AllowHostAudioPlayback { get; set; } = false;

    /// <summary>
    /// Allow client video capture redirection
    /// </summary>
    [JsonPropertyName("allowClientVideoCapture")]
    public bool AllowClientVideoCapture { get; set; } = false;

    /// <summary>
    /// Allow client audio capture redirection
    /// </summary>
    [JsonPropertyName("allowClientAudioCapture")]
    public bool AllowClientAudioCapture { get; set; } = false;

    /// <summary>
    /// Allow PnP device redirection
    /// </summary>
    [JsonPropertyName("allowPnpRedirect")]
    public bool AllowPnpRedirect { get; set; } = false;

    /// <summary>
    /// Restrict USB redirection to administrators only (null = not set)
    /// </summary>
    [JsonPropertyName("restrictUsbToAdmins")]
    public bool? RestrictUsbToAdmins { get; set; } = null;

    /// <summary>
    /// Add Windows Defender exclusion during installation
    /// </summary>
    [JsonPropertyName("addDefenderExclusion")]
    public bool AddDefenderExclusion { get; set; } = true;

    /// <summary>
    /// Add firewall rule when port is changed
    /// </summary>
    [JsonPropertyName("addFirewallRule")]
    public bool AddFirewallRule { get; set; } = true;

    /// <summary>
    /// Create default enterprise profile with sensible defaults for multiple admin access
    /// </summary>
    public static ConfigurationProfile CreateEnterpriseDefault() {
      return new ConfigurationProfile {
        Name = "Enterprise Multi-Admin",
        Description = "Configuration for multiple administrators accessing servers with single shared account",
        Wrapper = SupportedWrappers.TermWrap,
        RdpPort = 3389,
        MaxConnections = 10,
        SingleSessionPerUser = false,  // CRITICAL: Allow multiple sessions per user
        AllowTsConnections = true,
        NlaLevel = 1,  // Default RDP authentication
        SecurityLayer = 2,  // TLS encryption
        ShadowOptions = 0,  // Disabled for security
        AllowHostAudioPlayback = false,  // Not needed for server admin
        AllowClientVideoCapture = false,  // Not needed
        AllowClientAudioCapture = false,  // Not needed
        AllowPnpRedirect = false,  // Not needed
        RestrictUsbToAdmins = true,  // Security: admins only
        AddDefenderExclusion = true,
        AddFirewallRule = true
      };
    }

    /// <summary>
    /// Create high-security profile with strict settings
    /// </summary>
    public static ConfigurationProfile CreateHighSecurityProfile() {
      return new ConfigurationProfile {
        Name = "High Security",
        Description = "Maximum security settings with NLA and minimal features",
        Wrapper = SupportedWrappers.TermWrap,
        RdpPort = 3389,
        MaxConnections = 5,
        SingleSessionPerUser = true,  // One session per user
        AllowTsConnections = true,
        NlaLevel = 2,  // NLA required
        SecurityLayer = 2,  // TLS required
        ShadowOptions = 1,  // Full access with permission only
        DontDisplayLastUser = true,  // Security: hide usernames
        AllowHostAudioPlayback = false,
        AllowClientVideoCapture = false,
        AllowClientAudioCapture = false,
        AllowPnpRedirect = false,
        RestrictUsbToAdmins = true,
        AddDefenderExclusion = true,
        AddFirewallRule = true
      };
    }

    /// <summary>
    /// Validate profile settings
    /// </summary>
    public bool Validate(out string errorMessage) {
      errorMessage = string.Empty;

      // Validate port range
      if (RdpPort < 1 || RdpPort > 65535) {
        errorMessage = $"Invalid RDP port: {RdpPort}. Must be between 1 and 65535.";
        return false;
      }

      // Validate max connections
      if (MaxConnections < 0 || MaxConnections > 999999) {
        errorMessage = $"Invalid max connections: {MaxConnections}. Must be between 0 and 999999.";
        return false;
      }

      // Validate NLA level
      if (NlaLevel < 0 || NlaLevel > 2) {
        errorMessage = $"Invalid NLA level: {NlaLevel}. Must be 0, 1, or 2.";
        return false;
      }

      // Validate security layer
      if (SecurityLayer < 0 || SecurityLayer > 2) {
        errorMessage = $"Invalid security layer: {SecurityLayer}. Must be 0, 1, or 2.";
        return false;
      }

      // Validate shadow options
      if (ShadowOptions < 0 || ShadowOptions > 4) {
        errorMessage = $"Invalid shadow options: {ShadowOptions}. Must be between 0 and 4.";
        return false;
      }

      return true;
    }

    /// <summary>
    /// Apply this profile to a Wrapper instance
    /// </summary>
    public void ApplyToWrapper(Wrapper wrapper) {
      if (wrapper == null)
        throw new ArgumentNullException(nameof(wrapper));

      wrapper.RdpPort = RdpPort;
      wrapper.MaximumConnectionsAllowed = MaxConnections;
      wrapper.SingleSessionPerUser = SingleSessionPerUser;
      wrapper.AllowTsConnections = AllowTsConnections;
      wrapper.HonorLegacy = HonorLegacy;
      wrapper.UserAuthentication = NlaLevel;
      wrapper.SecurityLayer = SecurityLayer;
      wrapper.ShadowOptions = ShadowOptions;
      wrapper.DontDisplayLastUser = DontDisplayLastUser;
      wrapper.AllowHostPlaybackRedirect = AllowHostAudioPlayback;
      wrapper.AllowClientVideoCapture = AllowClientVideoCapture;
      wrapper.AllowClientAudioCapture = AllowClientAudioCapture;
      wrapper.AllowPnpRedirect = AllowPnpRedirect;
      wrapper.RestrictUsbRedirection = RestrictUsbToAdmins;
    }

    /// <summary>
    /// Load profile from JSON file
    /// </summary>
    public static ConfigurationProfile LoadFromFile(string filePath) {
      if (!File.Exists(filePath))
        throw new FileNotFoundException($"Configuration file not found: {filePath}");

      var json = File.ReadAllText(filePath);
      var options = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
      };

      var profile = JsonSerializer.Deserialize<ConfigurationProfile>(json, options);
      if (profile == null)
        throw new InvalidOperationException("Failed to deserialize configuration profile.");

      return profile;
    }

    /// <summary>
    /// Save profile to JSON file
    /// </summary>
    public void SaveToFile(string filePath) {
      var options = new JsonSerializerOptions {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
      };

      var json = JsonSerializer.Serialize(this, options);
      File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Load profile from JSON string
    /// </summary>
    public static ConfigurationProfile LoadFromJson(string json) {
      var options = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
      };

      var profile = JsonSerializer.Deserialize<ConfigurationProfile>(json, options);
      if (profile == null)
        throw new InvalidOperationException("Failed to deserialize configuration profile.");

      return profile;
    }

    /// <summary>
    /// Convert profile to JSON string
    /// </summary>
    public string ToJson(bool indented = true) {
      var options = new JsonSerializerOptions {
        WriteIndented = indented,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
      };

      return JsonSerializer.Serialize(this, options);
    }
  }
}
