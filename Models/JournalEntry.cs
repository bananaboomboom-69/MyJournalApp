using System;
using System.Collections.Generic;

namespace MyJournalApp.Models
{
    /// <summary>
    /// Represents a single journal entry with mood and tag tracking.
    /// </summary>
    public class JournalEntry
    {
        /// <summary>
        /// Unique identifier for the journal entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Title of the journal entry.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Main content/body of the journal entry.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Mood associated with this entry.
        /// </summary>
        public MoodType Mood { get; set; } = MoodType.Neutral;

        /// <summary>
        /// Comma-separated list of tags for this entry.
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Number of words in the content.
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// The date this entry is for (one entry per day).
        /// </summary>
        public DateTime EntryDate { get; set; }

        /// <summary>
        /// Timestamp when the entry was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets the list of tags as a collection.
        /// </summary>
        public List<string> GetTagsList()
        {
            if (string.IsNullOrWhiteSpace(Tags))
                return new List<string>();
            
            return new List<string>(Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
    }
}
