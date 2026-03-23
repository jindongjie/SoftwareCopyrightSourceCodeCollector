namespace SoftwareCopyrightSourceCodeCollector.CoreLib;

/// <summary>
/// Holds all software metadata used for DOCX / TXT export.
/// All ComboBox-driven fields are represented as plain strings here;
/// the UI layer is responsible for converting ComboBoxItem.Content to string.
/// </summary>
public class SoftwareInfo
{
    // ── Fields used for DOCX source-code export ──────────────────────────────
    public string SoftwareName { get; set; } = string.Empty;
    public string SoftwareVersion { get; set; } = "V1.0";
    public string SoftwareAuthor { get; set; } = string.Empty;

    // ── Fields used for TXT copyright-registration export ────────────────────
    public string SoftwareFullName { get; set; } = string.Empty;
    public string SoftwareShortName { get; set; } = string.Empty;
    public string SoftwareVersionNumber { get; set; } = "V1.0";
    public string SoftwareCategory { get; set; } = string.Empty;
    public string? DevelopmentFinishDate { get; set; }
    public string DevelopmentMethod { get; set; } = string.Empty;
    public string SoftwareDescription { get; set; } = string.Empty;
    public string PublishStatus { get; set; } = string.Empty;
    public string CopyrightOwner { get; set; } = string.Empty;
    public string RightsScope { get; set; } = string.Empty;
    public string RightsAcquisitionMethod { get; set; } = string.Empty;
    public string DevelopmentHardwareEnvironment { get; set; } = string.Empty;
    public string RuntimeHardwareEnvironment { get; set; } = string.Empty;
    public string DevelopmentOS { get; set; } = string.Empty;
    public string DevelopmentTool { get; set; } = string.Empty;
    public string RuntimePlatform { get; set; } = string.Empty;
    public string RuntimeSupportSoftware { get; set; } = string.Empty;
    public string ProgrammingLanguage { get; set; } = string.Empty;
    public string ProgrammingLanguageOther { get; set; } = string.Empty;
    public string SourceCodeAmount { get; set; } = string.Empty;
    public string DevelopmentPurpose { get; set; } = string.Empty;
    public string TargetIndustry { get; set; } = string.Empty;
    public string MainFunctions { get; set; } = string.Empty;
    public string TechnicalFeatures { get; set; } = string.Empty;

    // ── Software-type / technology flags ─────────────────────────────────────
    public bool IsAppSoftware { get; set; }
    public bool IsGameSoftware { get; set; }
    public bool IsEducationSoftware { get; set; }
    public bool IsFinanceSoftware { get; set; }
    public bool IsMedicalSoftware { get; set; }
    public bool IsGISSoftware { get; set; }
    public bool IsCloudSoftware { get; set; }
    public bool IsSecuritySoftware { get; set; }
    public bool IsBigDataSoftware { get; set; }
    public bool IsAISoftware { get; set; }
    public bool IsVRSoftware { get; set; }
    public bool Is5GSoftware { get; set; }
    public bool IsMiniProgramSoftware { get; set; }
    public bool IsSmartCitySoftware { get; set; }
    public bool IsIoTSoftware { get; set; }
    public bool IsIndustrialControlSoftware { get; set; }
}
