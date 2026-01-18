using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Primary mood associated with this entry (required).
        /// </summary>
        public MoodType PrimaryMood { get; set; } = MoodType.Neutral;

        /// <summary>
        /// Secondary moods stored as comma-separated values (max 2).
        /// </summary>
        public string SecondaryMoodsJson { get; set; } = string.Empty;

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
        /// Timestamp when the entry was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets the list of secondary moods.
        /// </summary>
        public List<MoodType> GetSecondaryMoods()
        {
            if (string.IsNullOrWhiteSpace(SecondaryMoodsJson))
                return new List<MoodType>();

            return SecondaryMoodsJson
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => Enum.TryParse<MoodType>(s, out _))
                .Select(s => Enum.Parse<MoodType>(s))
                .Take(2)
                .ToList();
        }

        /// <summary>
        /// Sets the secondary moods (max 2).
        /// </summary>
        public void SetSecondaryMoods(IEnumerable<MoodType> moods)
        {
            SecondaryMoodsJson = string.Join(",", moods.Take(2).Select(m => m.ToString()));
        }

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
