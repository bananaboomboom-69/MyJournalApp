using SQLite;

namespace myjournal.Models;

/// <summary>
/// User settings including theme, security, and preferences
/// </summary>
public class UserSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // Single row table

    /// <summary>
    /// Current theme (light, dark, or custom name)
    /// </summary>
    [MaxLength(50)]
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// Whether PIN protection is enabled
    /// </summary>
    public bool IsPinEnabled { get; set; }

    /// <summary>
    /// Hashed PIN for security
    /// </summary>
    [MaxLength(256)]
    public string? PinHash { get; set; }

    /// <summary>
    /// Salt used for PIN hashing
    /// </summary>
    [MaxLength(256)]
    public string? PinSalt { get; set; }

    /// <summary>
    /// Whether to show streak notifications
    /// </summary>
    public bool ShowStreakNotifications { get; set; } = true;

    /// <summary>
    /// Default primary mood to suggest
    /// </summary>
    public MoodType DefaultMood { get; set; } = MoodType.Neutral;

    /// <summary>
    /// Number of entries per page in journal view
    /// </summary>
    public int EntriesPerPage { get; set; } = 10;

    /// <summary>
    /// Whether to auto-save entries
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    /// Auto-save interval in seconds
    /// </summary>
    public int AutoSaveInterval { get; set; } = 30;
}

/// <summary>
/// Streak tracking information
/// </summary>
public class StreakInfo
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // Single row table

    /// <summary>
    /// Current consecutive days streak
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Longest streak ever achieved
    /// </summary>
    public int LongestStreak { get; set; }

    /// <summary>
    /// Total number of entries
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Total number of days with entries
    /// </summary>
    public int TotalDaysWithEntries { get; set; }

    /// <summary>
    /// Number of missed days (gaps in entries)
    /// </summary>
    public int MissedDays { get; set; }

    /// <summary>
    /// Date of the last entry
    /// </summary>
    public DateTime? LastEntryDate { get; set; }

    /// <summary>
    /// Date when the current streak started
    /// </summary>
    public DateTime? StreakStartDate { get; set; }
}
