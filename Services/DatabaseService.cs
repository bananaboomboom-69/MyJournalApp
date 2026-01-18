using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using MyJournalApp.Models;

namespace MyJournalApp.Services
{
    /// <summary>
    /// Service for managing SQLite database operations.
    /// Handles all CRUD operations for Users and JournalEntries.
    /// </summary>
    public class DatabaseService
    {
        private static DatabaseService? _instance;
        private readonly string _connectionString;
        private readonly string _databasePath;

        /// <summary>
        /// Singleton instance of the database service.
        /// </summary>
        public static DatabaseService Instance => _instance ??= new DatabaseService();

        private DatabaseService()
        {
            // Store database in AppData folder
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyJournalApp"
            );

            Directory.CreateDirectory(appDataPath);
            _databasePath = Path.Combine(appDataPath, "journal.db");
            _connectionString = $"Data Source={_databasePath}";

            InitializeDatabase();
        }

        /// <summary>
        /// Creates the database tables if they don't exist and applies migrations.
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Create Users table
            var createUsersTable = connection.CreateCommand();
            createUsersTable.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL DEFAULT 'User',
                    PasswordHash TEXT NOT NULL,
                    UsePin INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL
                )";
            createUsersTable.ExecuteNonQuery();

            // Create JournalEntries table with new schema
            var createEntriesTable = connection.CreateCommand();
            createEntriesTable.CommandText = @"
                CREATE TABLE IF NOT EXISTS JournalEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Mood TEXT NOT NULL,
                    SecondaryMoods TEXT,
                    Tags TEXT,
                    WordCount INTEGER NOT NULL,
                    EntryDate TEXT NOT NULL UNIQUE,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT
                )";
            createEntriesTable.ExecuteNonQuery();

            // Migration: Add new columns if they don't exist (for existing databases)
            MigrateDatabase(connection);
        }

        /// <summary>
        /// Applies schema migrations for existing databases.
        /// </summary>
        private void MigrateDatabase(SqliteConnection connection)
        {
            // Check if SecondaryMoods column exists
            var checkColumn = connection.CreateCommand();
            checkColumn.CommandText = "PRAGMA table_info(JournalEntries)";

            bool hasSecondaryMoods = false;
            bool hasUpdatedAt = false;

            using (var reader = checkColumn.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader.GetString(1);
                    if (columnName == "SecondaryMoods") hasSecondaryMoods = true;
                    if (columnName == "UpdatedAt") hasUpdatedAt = true;
                }
            }

            // Add SecondaryMoods column if missing
            if (!hasSecondaryMoods)
            {
                var addColumn = connection.CreateCommand();
                addColumn.CommandText = "ALTER TABLE JournalEntries ADD COLUMN SecondaryMoods TEXT";
                addColumn.ExecuteNonQuery();
            }

            // Add UpdatedAt column if missing
            if (!hasUpdatedAt)
            {
                var addColumn = connection.CreateCommand();
                addColumn.CommandText = "ALTER TABLE JournalEntries ADD COLUMN UpdatedAt TEXT";
                addColumn.ExecuteNonQuery();
            }
        }

        #region User Operations

        /// <summary>
        /// Gets the first (and only) user from the database.
        /// </summary>
        public User? GetUser()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Username, PasswordHash, UsePin, CreatedAt FROM Users LIMIT 1";

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = reader.GetString(2),
                    UsePin = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4))
                };
            }

            return null;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        public void CreateUser(User user)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, UsePin, CreatedAt)
                VALUES (@Username, @PasswordHash, @UsePin, @CreatedAt)";

            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@UsePin", user.UsePin ? 1 : 0);
            command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt.ToString("o"));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Checks if a user exists in the database.
        /// </summary>
        public bool UserExists()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users";

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Updates the user's password hash.
        /// </summary>
        public bool UpdateUserPassword(int userId, string newPasswordHash)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @UserId";
            command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
            command.Parameters.AddWithValue("@UserId", userId);

            return command.ExecuteNonQuery() > 0;
        }

        #endregion

        #region Journal Entry Operations

        /// <summary>
        /// Gets all journal entries ordered by date descending.
        /// </summary>
        public List<JournalEntry> GetAllEntries()
        {
            var entries = new List<JournalEntry>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Title, Content, Mood, SecondaryMoods, Tags, WordCount, EntryDate, CreatedAt, UpdatedAt 
                FROM JournalEntries 
                ORDER BY EntryDate DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(ReadJournalEntry(reader));
            }

            return entries;
        }

        /// <summary>
        /// Gets the most recent entries, limited by count.
        /// </summary>
        public List<JournalEntry> GetRecentEntries(int count = 5)
        {
            var entries = new List<JournalEntry>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Title, Content, Mood, SecondaryMoods, Tags, WordCount, EntryDate, CreatedAt, UpdatedAt 
                FROM JournalEntries 
                ORDER BY EntryDate DESC 
                LIMIT @Count";
            command.Parameters.AddWithValue("@Count", count);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(ReadJournalEntry(reader));
            }

            return entries;
        }

        /// <summary>
        /// Gets a journal entry by specific date.
        /// </summary>
        public JournalEntry? GetEntryByDate(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Title, Content, Mood, SecondaryMoods, Tags, WordCount, EntryDate, CreatedAt, UpdatedAt 
                FROM JournalEntries 
                WHERE date(EntryDate) = date(@EntryDate)";
            command.Parameters.AddWithValue("@EntryDate", date.Date.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadJournalEntry(reader);
            }

            return null;
        }

        /// <summary>
        /// Checks if an entry exists for the specified date.
        /// </summary>
        public bool EntryExistsForDate(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM JournalEntries 
                WHERE date(EntryDate) = date(@EntryDate)";
            command.Parameters.AddWithValue("@EntryDate", date.Date.ToString("yyyy-MM-dd"));

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Creates a new journal entry. Throws if entry already exists for that date.
        /// </summary>
        public void CreateEntry(JournalEntry entry)
        {
            // Service-level check for one entry per day
            if (EntryExistsForDate(entry.EntryDate))
            {
                throw new InvalidOperationException($"An entry already exists for {entry.EntryDate:yyyy-MM-dd}. Use UpdateEntry instead.");
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO JournalEntries (Title, Content, Mood, SecondaryMoods, Tags, WordCount, EntryDate, CreatedAt)
                VALUES (@Title, @Content, @Mood, @SecondaryMoods, @Tags, @WordCount, @EntryDate, @CreatedAt)";

            command.Parameters.AddWithValue("@Title", entry.Title);
            command.Parameters.AddWithValue("@Content", entry.Content);
            command.Parameters.AddWithValue("@Mood", entry.PrimaryMood.ToString());
            command.Parameters.AddWithValue("@SecondaryMoods", entry.SecondaryMoodsJson ?? string.Empty);
            command.Parameters.AddWithValue("@Tags", entry.Tags ?? string.Empty);
            command.Parameters.AddWithValue("@WordCount", entry.WordCount);
            command.Parameters.AddWithValue("@EntryDate", entry.EntryDate.Date.ToString("o"));
            command.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt.ToString("o"));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates an existing journal entry. Preserves CreatedAt, refreshes UpdatedAt.
        /// </summary>
        public bool UpdateEntry(JournalEntry entry)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE JournalEntries 
                SET Title = @Title, 
                    Content = @Content, 
                    Mood = @Mood, 
                    SecondaryMoods = @SecondaryMoods,
                    Tags = @Tags, 
                    WordCount = @WordCount,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Title", entry.Title);
            command.Parameters.AddWithValue("@Content", entry.Content);
            command.Parameters.AddWithValue("@Mood", entry.PrimaryMood.ToString());
            command.Parameters.AddWithValue("@SecondaryMoods", entry.SecondaryMoodsJson ?? string.Empty);
            command.Parameters.AddWithValue("@Tags", entry.Tags ?? string.Empty);
            command.Parameters.AddWithValue("@WordCount", entry.WordCount);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));
            command.Parameters.AddWithValue("@Id", entry.Id);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Deletes a journal entry by ID.
        /// </summary>
        public bool DeleteEntry(int entryId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM JournalEntries WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", entryId);

            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Helper method to read a JournalEntry from a data reader.
        /// </summary>
        private static JournalEntry ReadJournalEntry(SqliteDataReader reader)
        {
            return new JournalEntry
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = reader.GetString(2),
                PrimaryMood = Enum.Parse<MoodType>(reader.GetString(3)),
                SecondaryMoodsJson = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Tags = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                WordCount = reader.GetInt32(6),
                EntryDate = DateTime.Parse(reader.GetString(7)),
                CreatedAt = DateTime.Parse(reader.GetString(8)),
                UpdatedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
            };
        }

        /// <summary>
        /// Gets the total count of journal entries.
        /// </summary>
        public int GetTotalEntriesCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM JournalEntries";

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Gets the count of entries created this week.
        /// </summary>
        public int GetEntriesThisWeek()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM JournalEntries 
                WHERE date(EntryDate) >= date(@StartOfWeek)";
            command.Parameters.AddWithValue("@StartOfWeek", startOfWeek.ToString("yyyy-MM-dd"));

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Gets the total word count for the last 30 days.
        /// </summary>
        public int GetTotalWordCount(int days = 30)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var startDate = DateTime.Today.AddDays(-days);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COALESCE(SUM(WordCount), 0) FROM JournalEntries 
                WHERE date(EntryDate) >= date(@StartDate)";
            command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Searches journal entries with combined filters. All filters are applied at the database level.
        /// </summary>
        /// <param name="criteria">Search criteria with optional filters.</param>
        /// <returns>List of matching journal entries ordered by date descending.</returns>
        public List<JournalEntry> SearchEntries(SearchCriteria criteria)
        {
            var entries = new List<JournalEntry>();
            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Build WHERE conditions dynamically

            // Keyword search (title OR content)
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                conditions.Add("(Title LIKE @Keyword OR Content LIKE @Keyword)");
                parameters["@Keyword"] = $"%{criteria.Keyword}%";
            }

            // Date range filter
            if (criteria.StartDate.HasValue)
            {
                conditions.Add("date(EntryDate) >= date(@StartDate)");
                parameters["@StartDate"] = criteria.StartDate.Value.ToString("yyyy-MM-dd");
            }

            if (criteria.EndDate.HasValue)
            {
                conditions.Add("date(EntryDate) <= date(@EndDate)");
                parameters["@EndDate"] = criteria.EndDate.Value.ToString("yyyy-MM-dd");
            }

            // Mood filter (primary + optional secondary)
            if (criteria.PrimaryMood.HasValue)
            {
                var moodStr = criteria.PrimaryMood.Value.ToString();
                if (criteria.IncludeSecondaryMoods)
                {
                    // Match primary OR secondary moods
                    conditions.Add("(Mood = @Mood OR SecondaryMoods LIKE @MoodPattern)");
                    parameters["@Mood"] = moodStr;
                    parameters["@MoodPattern"] = $"%{moodStr}%";
                }
                else
                {
                    // Match primary mood only
                    conditions.Add("Mood = @Mood");
                    parameters["@Mood"] = moodStr;
                }
            }

            // Tag filter
            if (!string.IsNullOrWhiteSpace(criteria.Tag))
            {
                var tagToFind = criteria.Tag.TrimStart('#').Trim();
                conditions.Add("Tags LIKE @Tag");
                parameters["@Tag"] = $"%{tagToFind}%";
            }

            // Build the SQL query
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT Id, Title, Content, Mood, SecondaryMoods, Tags, WordCount, EntryDate, CreatedAt, UpdatedAt FROM JournalEntries");

            if (conditions.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", conditions));
            }

            sql.Append(" ORDER BY EntryDate DESC");

            // Pagination
            if (criteria.Limit.HasValue)
            {
                sql.Append(" LIMIT @Limit");
                parameters["@Limit"] = criteria.Limit.Value;
            }

            if (criteria.Offset.HasValue)
            {
                sql.Append(" OFFSET @Offset");
                parameters["@Offset"] = criteria.Offset.Value;
            }

            // Execute query
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = sql.ToString();

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(ReadJournalEntry(reader));
            }

            return entries;
        }

        /// <summary>
        /// Gets the count of entries matching the search criteria.
        /// </summary>
        public int GetSearchResultsCount(SearchCriteria criteria)
        {
            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Build WHERE conditions (same logic as SearchEntries)
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                conditions.Add("(Title LIKE @Keyword OR Content LIKE @Keyword)");
                parameters["@Keyword"] = $"%{criteria.Keyword}%";
            }

            if (criteria.StartDate.HasValue)
            {
                conditions.Add("date(EntryDate) >= date(@StartDate)");
                parameters["@StartDate"] = criteria.StartDate.Value.ToString("yyyy-MM-dd");
            }

            if (criteria.EndDate.HasValue)
            {
                conditions.Add("date(EntryDate) <= date(@EndDate)");
                parameters["@EndDate"] = criteria.EndDate.Value.ToString("yyyy-MM-dd");
            }

            if (criteria.PrimaryMood.HasValue)
            {
                var moodStr = criteria.PrimaryMood.Value.ToString();
                if (criteria.IncludeSecondaryMoods)
                {
                    conditions.Add("(Mood = @Mood OR SecondaryMoods LIKE @MoodPattern)");
                    parameters["@Mood"] = moodStr;
                    parameters["@MoodPattern"] = $"%{moodStr}%";
                }
                else
                {
                    conditions.Add("Mood = @Mood");
                    parameters["@Mood"] = moodStr;
                }
            }

            if (!string.IsNullOrWhiteSpace(criteria.Tag))
            {
                var tagToFind = criteria.Tag.TrimStart('#').Trim();
                conditions.Add("Tags LIKE @Tag");
                parameters["@Tag"] = $"%{tagToFind}%";
            }

            // Build count query
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT COUNT(*) FROM JournalEntries");

            if (conditions.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", conditions));
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = sql.ToString();

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

            return Convert.ToInt32(command.ExecuteScalar());
        }

        #endregion

        #region Analytics Operations

        /// <summary>
        /// Calculates the current streak of consecutive daily entries.
        /// </summary>
        public int GetCurrentStreak()
        {
            var entries = GetAllEntries();
            if (entries.Count == 0) return 0;

            var sortedDates = entries
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            // Check if there's an entry for today or yesterday
            var today = DateTime.Today;
            if (sortedDates[0] < today.AddDays(-1))
                return 0;

            int streak = 1;
            for (int i = 1; i < sortedDates.Count; i++)
            {
                if ((sortedDates[i - 1] - sortedDates[i]).Days == 1)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        /// <summary>
        /// Calculates the longest streak of consecutive daily entries.
        /// </summary>
        public int GetLongestStreak()
        {
            var entries = GetAllEntries();
            if (entries.Count == 0) return 0;

            var sortedDates = entries
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            int longestStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < sortedDates.Count; i++)
            {
                if ((sortedDates[i] - sortedDates[i - 1]).Days == 1)
                {
                    currentStreak++;
                    longestStreak = Math.Max(longestStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }

            return longestStreak;
        }

        /// <summary>
        /// Gets PRIMARY mood distribution counts for the last N days.
        /// Used for mood breakdown analytics (unchanged behavior - only primary moods).
        /// </summary>
        public Dictionary<MoodType, int> GetMoodDistribution(int days = 30)
        {
            var distribution = new Dictionary<MoodType, int>();
            foreach (MoodType mood in Enum.GetValues<MoodType>())
            {
                distribution[mood] = 0;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var startDate = DateTime.Today.AddDays(-days);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Mood, COUNT(*) as Count 
                FROM JournalEntries 
                WHERE date(EntryDate) >= date(@StartDate)
                GROUP BY Mood";
            command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var mood = Enum.Parse<MoodType>(reader.GetString(0));
                distribution[mood] = reader.GetInt32(1);
            }

            return distribution;
        }

        /// <summary>
        /// Gets frequency counts for ALL moods (primary + secondary) for the last N days.
        /// Used for overall mood frequency statistics.
        /// </summary>
        public Dictionary<MoodType, int> GetAllMoodFrequency(int days = 30)
        {
            var frequency = new Dictionary<MoodType, int>();
            foreach (MoodType mood in Enum.GetValues<MoodType>())
            {
                frequency[mood] = 0;
            }

            var startDate = DateTime.Today.AddDays(-days);
            var entries = GetAllEntries().Where(e => e.EntryDate.Date >= startDate).ToList();

            foreach (var entry in entries)
            {
                // Count primary mood
                frequency[entry.PrimaryMood]++;

                // Count secondary moods
                foreach (var secondaryMood in entry.GetSecondaryMoods())
                {
                    frequency[secondaryMood]++;
                }
            }

            return frequency;
        }

        /// <summary>
        /// Gets the most frequently used tags.
        /// </summary>
        public Dictionary<string, int> GetFrequentTags(int limit = 10)
        {
            var tagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var entries = GetAllEntries();
            foreach (var entry in entries)
            {
                foreach (var tag in entry.GetTagsList())
                {
                    var cleanTag = tag.Trim();
                    if (!string.IsNullOrEmpty(cleanTag))
                    {
                        tagCounts[cleanTag] = tagCounts.GetValueOrDefault(cleanTag, 0) + 1;
                    }
                }
            }

            return tagCounts
                .OrderByDescending(kv => kv.Value)
                .Take(limit)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// Gets weekly word counts for the last 4 weeks.
        /// </summary>
        public List<int> GetWeeklyWordCounts()
        {
            var weeklyTotals = new List<int>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            for (int week = 3; week >= 0; week--)
            {
                var weekStart = DateTime.Today.AddDays(-7 * (week + 1));
                var weekEnd = DateTime.Today.AddDays(-7 * week);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COALESCE(SUM(WordCount), 0) FROM JournalEntries 
                    WHERE date(EntryDate) >= date(@WeekStart) AND date(EntryDate) < date(@WeekEnd)";
                command.Parameters.AddWithValue("@WeekStart", weekStart.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@WeekEnd", weekEnd.ToString("yyyy-MM-dd"));

                weeklyTotals.Add(Convert.ToInt32(command.ExecuteScalar()));
            }

            return weeklyTotals;
        }

        #endregion
    }
}
