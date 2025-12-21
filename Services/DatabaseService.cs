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
        /// Creates the database tables if they don't exist.
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

            // Create JournalEntries table
            var createEntriesTable = connection.CreateCommand();
            createEntriesTable.CommandText = @"
                CREATE TABLE IF NOT EXISTS JournalEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Mood TEXT NOT NULL,
                    Tags TEXT,
                    WordCount INTEGER NOT NULL,
                    EntryDate TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                )";
            createEntriesTable.ExecuteNonQuery();
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
                SELECT Id, Title, Content, Mood, Tags, WordCount, EntryDate, CreatedAt 
                FROM JournalEntries 
                ORDER BY EntryDate DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new JournalEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    Mood = Enum.Parse<MoodType>(reader.GetString(3)),
                    Tags = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    WordCount = reader.GetInt32(5),
                    EntryDate = DateTime.Parse(reader.GetString(6)),
                    CreatedAt = DateTime.Parse(reader.GetString(7))
                });
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
                SELECT Id, Title, Content, Mood, Tags, WordCount, EntryDate, CreatedAt 
                FROM JournalEntries 
                ORDER BY EntryDate DESC 
                LIMIT @Count";
            command.Parameters.AddWithValue("@Count", count);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new JournalEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    Mood = Enum.Parse<MoodType>(reader.GetString(3)),
                    Tags = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    WordCount = reader.GetInt32(5),
                    EntryDate = DateTime.Parse(reader.GetString(6)),
                    CreatedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return entries;
        }

        /// <summary>
        /// Creates a new journal entry.
        /// </summary>
        public void CreateEntry(JournalEntry entry)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO JournalEntries (Title, Content, Mood, Tags, WordCount, EntryDate, CreatedAt)
                VALUES (@Title, @Content, @Mood, @Tags, @WordCount, @EntryDate, @CreatedAt)";
            
            command.Parameters.AddWithValue("@Title", entry.Title);
            command.Parameters.AddWithValue("@Content", entry.Content);
            command.Parameters.AddWithValue("@Mood", entry.Mood.ToString());
            command.Parameters.AddWithValue("@Tags", entry.Tags ?? string.Empty);
            command.Parameters.AddWithValue("@WordCount", entry.WordCount);
            command.Parameters.AddWithValue("@EntryDate", entry.EntryDate.ToString("o"));
            command.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt.ToString("o"));

            command.ExecuteNonQuery();
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
        /// Gets mood distribution counts for the last 30 days.
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
