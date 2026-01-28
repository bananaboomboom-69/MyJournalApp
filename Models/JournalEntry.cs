using SQLite;

namespace myjournal.Models;

/// <summary>
/// Represents a journal entry with mood and tag support
/// </summary>
public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content stored in Markdown format
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The date this entry is for (only one entry per day allowed)
    /// </summary>
    [Indexed]
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// System timestamp when created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// System timestamp when last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Primary mood for the entry (required)
    /// </summary>
    public MoodType PrimaryMood { get; set; } = MoodType.Neutral;

    /// <summary>
    /// First secondary mood (optional)
    /// </summary>
    public MoodType? SecondaryMood1 { get; set; }

    /// <summary>
    /// Second secondary mood (optional)
    /// </summary>
    public MoodType? SecondaryMood2 { get; set; }

    /// <summary>
    /// Word count of the content
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Indicates if this entry is marked as favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Tags associated with this entry (not stored directly, loaded via junction table)
    /// </summary>
    [Ignore]
    public List<Tag> Tags { get; set; } = new();

    /// <summary>
    /// Calculate word count from content
    /// </summary>
    public void UpdateWordCount()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            WordCount = 0;
            return;
        }

        WordCount = Content.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Get a preview of the content (first 150 characters)
    /// </summary>
    public string GetPreview(int maxLength = 150)
    {
        if (string.IsNullOrWhiteSpace(Content))
            return string.Empty;

        var plainText = Content
            .Replace("#", "")
            .Replace("*", "")
            .Replace("_", "")
            .Replace("`", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("(", "")
            .Replace(")", "")
            .Trim();

        return plainText.Length <= maxLength
            ? plainText
            : plainText.Substring(0, maxLength) + "...";
    }
}
