using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MyJournalApp.Models;
using MyJournalApp.Services;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Dashboard screen.
    /// Displays statistics, recent activity, mood distribution, and frequent tags.
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly AuthenticationService _authService;

        private string _welcomeMessage = "Welcome back";
        private string _currentDate = DateTime.Now.ToString("dddd, MMMM d");
        private int _currentStreak;
        private int _longestStreak;
        private int _totalEntries;
        private int _entriesThisWeek;
        private int _totalWordCount;
        private string _mostFrequentMood = "Calm";
        private string _selectedNavItem = "Dashboard";

        #region Properties

        /// <summary>
        /// Welcome message with user's name.
        /// </summary>
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        /// <summary>
        /// Current date formatted for display.
        /// </summary>
        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        /// <summary>
        /// Current consecutive day streak.
        /// </summary>
        public int CurrentStreak
        {
            get => _currentStreak;
            set => SetProperty(ref _currentStreak, value);
        }

        /// <summary>
        /// Longest streak ever achieved.
        /// </summary>
        public int LongestStreak
        {
            get => _longestStreak;
            set => SetProperty(ref _longestStreak, value);
        }

        /// <summary>
        /// Total number of journal entries.
        /// </summary>
        public int TotalEntries
        {
            get => _totalEntries;
            set => SetProperty(ref _totalEntries, value);
        }

        /// <summary>
        /// Number of entries created this week.
        /// </summary>
        public int EntriesThisWeek
        {
            get => _entriesThisWeek;
            set => SetProperty(ref _entriesThisWeek, value);
        }

        /// <summary>
        /// Total word count for the last 30 days.
        /// </summary>
        public int TotalWordCount
        {
            get => _totalWordCount;
            set => SetProperty(ref _totalWordCount, value);
        }

        /// <summary>
        /// The most frequent mood in the last 30 days.
        /// </summary>
        public string MostFrequentMood
        {
            get => _mostFrequentMood;
            set => SetProperty(ref _mostFrequentMood, value);
        }

        /// <summary>
        /// Currently selected navigation item.
        /// </summary>
        public string SelectedNavItem
        {
            get => _selectedNavItem;
            set => SetProperty(ref _selectedNavItem, value);
        }

        /// <summary>
        /// Mood distribution data for the chart.
        /// </summary>
        public ObservableCollection<MoodData> MoodDistribution { get; } = new();

        /// <summary>
        /// Recent journal entries for display.
        /// </summary>
        public ObservableCollection<RecentEntry> RecentEntries { get; } = new();

        /// <summary>
        /// Frequently used tags.
        /// </summary>
        public ObservableCollection<TagData> FrequentTags { get; } = new();

        /// <summary>
        /// Weekly word counts for the chart.
        /// </summary>
        public ObservableCollection<int> WeeklyWordCounts { get; } = new();

        #endregion

        #region Commands

        public ICommand NewEntryCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ViewAllEntriesCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion

        public DashboardViewModel()
        {
            _database = DatabaseService.Instance;
            _authService = AuthenticationService.Instance;

            // Commands
            NewEntryCommand = new RelayCommand(ExecuteNewEntry);
            NavigateCommand = new RelayCommand((param) => ExecuteNavigate(param as string));
            ViewAllEntriesCommand = new RelayCommand(ExecuteViewAllEntries);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Load data
            LoadDashboardData();
        }

        /// <summary>
        /// Loads all dashboard data from the database.
        /// </summary>
        private void LoadDashboardData()
        {
            // Set welcome message
            var user = _authService.CurrentUser;
            WelcomeMessage = $"Welcome back, {user?.Username ?? "User"}";

            // Load statistics
            CurrentStreak = _database.GetCurrentStreak();
            LongestStreak = _database.GetLongestStreak();
            TotalEntries = _database.GetTotalEntriesCount();
            EntriesThisWeek = _database.GetEntriesThisWeek();
            TotalWordCount = _database.GetTotalWordCount();

            // Load mood distribution
            LoadMoodDistribution();

            // Load recent entries
            LoadRecentEntries();

            // Load frequent tags
            LoadFrequentTags();

            // Load weekly word counts
            LoadWeeklyWordCounts();
        }

        private void LoadMoodDistribution()
        {
            MoodDistribution.Clear();
            var distribution = _database.GetMoodDistribution();
            var total = distribution.Values.Sum();

            // Find most frequent mood
            if (total > 0)
            {
                var maxMood = distribution.OrderByDescending(kv => kv.Value).First();
                MostFrequentMood = maxMood.Key.ToString();
            }

            // Add mood data with percentages
            var moodColors = new Dictionary<MoodType, string>
            {
                { MoodType.Happy, "#F0B429" },      // Yellow/Gold
                { MoodType.Calm, "#58A6FF" },       // Blue
                { MoodType.Anxious, "#F97316" },    // Orange
                { MoodType.Neutral, "#6B7280" },    // Gray
                { MoodType.Productive, "#22C55E" }, // Green
                { MoodType.Sad, "#8B5CF6" }         // Purple
            };

            var moodIcons = new Dictionary<MoodType, string>
            {
                { MoodType.Happy, "😊" },
                { MoodType.Calm, "🍃" },
                { MoodType.Anxious, "😰" },
                { MoodType.Neutral, "😐" },
                { MoodType.Productive, "💪" },
                { MoodType.Sad, "😢" }
            };

            foreach (var mood in new[] { MoodType.Happy, MoodType.Calm, MoodType.Anxious, MoodType.Neutral })
            {
                var count = distribution.GetValueOrDefault(mood, 0);
                var percentage = total > 0 ? (double)count / total * 100 : 0;

                MoodDistribution.Add(new MoodData
                {
                    Mood = mood.ToString(),
                    Icon = moodIcons.GetValueOrDefault(mood, "😐"),
                    Count = count,
                    Percentage = percentage,
                    Color = moodColors.GetValueOrDefault(mood, "#58A6FF")
                });
            }
        }

        private void LoadRecentEntries()
        {
            RecentEntries.Clear();
            var entries = _database.GetRecentEntries(3);

            foreach (var entry in entries)
            {
                var moodColors = new Dictionary<MoodType, string>
                {
                    { MoodType.Happy, "#F0B429" },
                    { MoodType.Calm, "#58A6FF" },
                    { MoodType.Anxious, "#F97316" },
                    { MoodType.Neutral, "#6B7280" },
                    { MoodType.Productive, "#22C55E" },
                    { MoodType.Sad, "#8B5CF6" }
                };

                var moodIcons = new Dictionary<MoodType, string>
                {
                    { MoodType.Happy, "☀️" },
                    { MoodType.Calm, "🌙" },
                    { MoodType.Anxious, "😰" },
                    { MoodType.Neutral, "☁️" },
                    { MoodType.Productive, "⚡" },
                    { MoodType.Sad, "🌧️" }
                };

                RecentEntries.Add(new RecentEntry
                {
                    Title = entry.Title,
                    Date = FormatEntryDate(entry.EntryDate),
                    Mood = entry.Mood.ToString(),
                    MoodIcon = moodIcons.GetValueOrDefault(entry.Mood, "📝"),
                    MoodColor = moodColors.GetValueOrDefault(entry.Mood, "#58A6FF"),
                    IconBackground = GetIconBackground(entry.Mood)
                });
            }

            // Add sample data if no entries exist
            if (RecentEntries.Count == 0)
            {
                RecentEntries.Add(new RecentEntry
                {
                    Title = "Reflecting on the quarterly goals",
                    Date = "Today, 10:30 AM",
                    Mood = "Productive",
                    MoodIcon = "⚡",
                    MoodColor = "#22C55E",
                    IconBackground = "#1C2B1F"
                });
                RecentEntries.Add(new RecentEntry
                {
                    Title = "Why I felt anxious yesterday",
                    Date = "Yesterday, 9:15 PM",
                    Mood = "Anxious",
                    MoodIcon = "🌙",
                    MoodColor = "#F97316",
                    IconBackground = "#2D1F1A"
                });
                RecentEntries.Add(new RecentEntry
                {
                    Title = "Morning gratitude practice",
                    Date = "Oct 22, 7:00 AM",
                    Mood = "Happy",
                    MoodIcon = "☀️",
                    MoodColor = "#F0B429",
                    IconBackground = "#2D2A1A"
                });
            }
        }

        private string GetIconBackground(MoodType mood) => mood switch
        {
            MoodType.Happy => "#2D2A1A",
            MoodType.Calm => "#1A2D3D",
            MoodType.Anxious => "#2D1F1A",
            MoodType.Neutral => "#1F2428",
            MoodType.Productive => "#1C2B1F",
            MoodType.Sad => "#251F2D",
            _ => "#1F2428"
        };

        private string FormatEntryDate(DateTime date)
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            if (date.Date == today)
                return $"Today, {date:h:mm tt}";
            if (date.Date == yesterday)
                return $"Yesterday, {date:h:mm tt}";
            return date.ToString("MMM d, h:mm tt");
        }

        private void LoadFrequentTags()
        {
            FrequentTags.Clear();
            var tags = _database.GetFrequentTags(8);

            foreach (var tag in tags)
            {
                FrequentTags.Add(new TagData
                {
                    Name = $"#{tag.Key}",
                    Count = tag.Value,
                    IsHighlighted = tag.Value >= tags.Values.Max() * 0.7
                });
            }

            // Add sample tags if none exist
            if (FrequentTags.Count == 0)
            {
                var sampleTags = new[] { "Gratitude", "WorkLife", "Family", "MentalHealth", "Ideas", "Running", "Goals", "Reading" };
                foreach (var tag in sampleTags)
                {
                    FrequentTags.Add(new TagData
                    {
                        Name = $"#{tag}",
                        Count = 0,
                        IsHighlighted = tag == "Gratitude" || tag == "WorkLife"
                    });
                }
            }
        }

        private void LoadWeeklyWordCounts()
        {
            WeeklyWordCounts.Clear();
            var counts = _database.GetWeeklyWordCounts();
            foreach (var count in counts)
            {
                WeeklyWordCounts.Add(count);
            }

            // Add sample data if empty
            if (WeeklyWordCounts.All(c => c == 0))
            {
                WeeklyWordCounts.Clear();
                WeeklyWordCounts.Add(2500);
                WeeklyWordCounts.Add(3200);
                WeeklyWordCounts.Add(4100);
                WeeklyWordCounts.Add(2650);
            }
        }

        private void ExecuteNewEntry()
        {
            // Navigate to Journal Entry screen
            NavigationService.Instance.NavigateTo("JournalEntry");
        }

        private void ExecuteNavigate(string? navItem)
        {
            if (!string.IsNullOrEmpty(navItem))
            {
                SelectedNavItem = navItem;
            }
        }

        private void ExecuteViewAllEntries()
        {
            SelectedNavItem = "Entries";
        }

        private void ExecuteLogout()
        {
            _authService.Logout();
            NavigationService.Instance.NavigateTo("Login");
        }
    }

    #region Helper Classes

    /// <summary>
    /// Data class for mood distribution display.
    /// </summary>
    public class MoodData
    {
        public string Mood { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = "#58A6FF";
    }

    /// <summary>
    /// Data class for recent entry display.
    /// </summary>
    public class RecentEntry
    {
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Mood { get; set; } = string.Empty;
        public string MoodIcon { get; set; } = string.Empty;
        public string MoodColor { get; set; } = "#58A6FF";
        public string IconBackground { get; set; } = "#1F2428";
    }

    /// <summary>
    /// Data class for tag display.
    /// </summary>
    public class TagData
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsHighlighted { get; set; }
    }

    #endregion
}
