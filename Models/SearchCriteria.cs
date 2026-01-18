using System;

namespace MyJournalApp.Models
{
    /// <summary>
    /// Represents search and filter criteria for journal entries.
    /// All properties are optional - only non-null values are used in filtering.
    /// </summary>
    public class SearchCriteria
    {
        /// <summary>
        /// Keyword to search in title and content.
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Start date for date range filter (inclusive).
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for date range filter (inclusive).
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filter by primary mood.
        /// </summary>
        public MoodType? PrimaryMood { get; set; }

        /// <summary>
        /// When true, also match entries where the mood appears in secondary moods.
        /// </summary>
        public bool IncludeSecondaryMoods { get; set; }

        /// <summary>
        /// Filter by tag (partial match supported).
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Maximum number of results to return (for pagination).
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Number of results to skip (for pagination).
        /// </summary>
        public int? Offset { get; set; }

        /// <summary>
        /// Returns true if any filter criteria is set.
        /// </summary>
        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Keyword) ||
            StartDate.HasValue ||
            EndDate.HasValue ||
            PrimaryMood.HasValue ||
            !string.IsNullOrWhiteSpace(Tag);

        /// <summary>
        /// Creates empty search criteria (returns all entries).
        /// </summary>
        public static SearchCriteria Empty => new();
    }
}
