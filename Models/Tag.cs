using SQLite;

namespace myjournal.Models;

/// <summary>
/// Represents a tag for categorizing journal entries
/// </summary>
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";

    public bool IsPreBuilt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Pre-built tags that come with the application
    /// </summary>
    public static List<Tag> GetPreBuiltTags() => new()
    {
        new Tag { Name = "Personal", Color = "#EC4899", IsPreBuilt = true },
        new Tag { Name = "Work", Color = "#3B82F6", IsPreBuilt = true },
        new Tag { Name = "Health", Color = "#10B981", IsPreBuilt = true },
        new Tag { Name = "Travel", Color = "#F59E0B", IsPreBuilt = true },
        new Tag { Name = "Family", Color = "#8B5CF6", IsPreBuilt = true },
        new Tag { Name = "Goals", Color = "#EF4444", IsPreBuilt = true },
        new Tag { Name = "Reflection", Color = "#06B6D4", IsPreBuilt = true },
        new Tag { Name = "Gratitude", Color = "#84CC16", IsPreBuilt = true },
        new Tag { Name = "Dreams", Color = "#A855F7", IsPreBuilt = true },
        new Tag { Name = "Ideas", Color = "#F97316", IsPreBuilt = true }
    };
}

/// <summary>
/// Junction table for many-to-many relationship between entries and tags
/// </summary>
public class EntryTag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int EntryId { get; set; }

    [Indexed]
    public int TagId { get; set; }
}
