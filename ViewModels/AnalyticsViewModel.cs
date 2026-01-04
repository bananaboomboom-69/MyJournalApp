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
    /// ViewModel for the Analytics/Insights screen.
    /// Displays statistics, mood trends, and writing patterns.
    /// </summary>
    public class AnalyticsViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly AuthenticationService _authService;

        private string _selectedTimeRange = "Last 7 Days";
        private string _overviewPeriod = DateTime.Now.ToString("MMMM yyyy");
        private int _totalEntries;
        private string _entriesChange = "+12%";
        private double _avgMoodScore = 7.8;
        private string _moodScoreChange = "+0.5";
        private int _totalWordCount;
        private string _wordCountChange = "+1.2k";
        private string _topMood = "Good";
        private string _moodTrend = "Overall trend shows a positive incline.";
        private int _dailyAvgWords = 1700;
        private string _selectedNavItem = "Analytics";
        private string _appName = "My Journal";
        private string _appSubtitle = "Productivity & Reflection";

        #region Properties

        public string SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                if (SetProperty(ref _selectedTimeRange, value))
                {
                    LoadAnalyticsData();
                }
            }
        }

        public string OverviewPeriod
        {
            get => _overviewPeriod;
            set => SetProperty(ref _overviewPeriod, value);
        }

        public int TotalEntries
        {
            get => _totalEntries;
            set => SetProperty(ref _totalEntries, value);
        }

        public string EntriesChange
        {
            get => _entriesChange;
            set => SetProperty(ref _entriesChange, value);
        }

        public double AvgMoodScore
        {
            get => _avgMoodScore;
            set => SetProperty(ref _avgMoodScore, value);
        }

        public string MoodScoreChange
        {
            get => _moodScoreChange;
            set => SetProperty(ref _moodScoreChange, value);
        }

        public int TotalWordCount
        {
            get => _totalWordCount;
            set => SetProperty(ref _totalWordCount, value);
        }

        public string WordCountChange
        {
            get => _wordCountChange;
            set => SetProperty(ref _wordCountChange, value);
        }

        public string TopMood
        {
            get => _topMood;
            set => SetProperty(ref _topMood, value);
        }

        public string MoodTrend
        {
            get => _moodTrend;
            set => SetProperty(ref _moodTrend, value);
        }

        public int DailyAvgWords
        {
            get => _dailyAvgWords;
            set => SetProperty(ref _dailyAvgWords, value);
        }

        public string SelectedNavItem
        {
            get => _selectedNavItem;
            set => SetProperty(ref _selectedNavItem, value);
        }

        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        public string AppSubtitle
        {
            get => _appSubtitle;
            set => SetProperty(ref _appSubtitle, value);
        }

        // Time range selection states
        public bool IsLast7Days => SelectedTimeRange == "Last 7 Days";
        public bool IsThisMonth => SelectedTimeRange == "This Month";
        public bool IsCustomRange => SelectedTimeRange == "Custom Range";

        /// <summary>
        /// Weekly mood fluctuation data points.
        /// </summary>
        public ObservableCollection<MoodFluctuationPoint> MoodFluctuations { get; } = new();

        /// <summary>
        /// Mood breakdown percentages for donut chart.
        /// </summary>
        public ObservableCollection<MoodBreakdownItem> MoodBreakdown { get; } = new();

        /// <summary>
        /// Top context tags with entry counts.
        /// </summary>
        public ObservableCollection<ContextTagItem> TopContextTags { get; } = new();

        /// <summary>
        /// Daily word counts for writing consistency.
        /// </summary>
        public ObservableCollection<DailyWordCount> WritingConsistency { get; } = new();

        #endregion

        #region Commands

        public ICommand SelectTimeRangeCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion

        public AnalyticsViewModel()
        {
            _database = DatabaseService.Instance;
            _authService = AuthenticationService.Instance;

            // Initialize commands
            SelectTimeRangeCommand = new RelayCommand(ExecuteSelectTimeRange);
            ExportDataCommand = new RelayCommand(ExecuteExportData);
            NavigateCommand = new RelayCommand(ExecuteNavigate);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Load analytics data
            LoadAnalyticsData();
        }

        private void LoadAnalyticsData()
        {
            // Load basic stats
            TotalEntries = _database.GetTotalEntriesCount();
            TotalWordCount = _database.GetTotalWordCount();

            // If no real data, use sample data
            if (TotalEntries == 0)
            {
                TotalEntries = 42;
                TotalWordCount = 12450;
            }

            // Load mood fluctuations
            LoadMoodFluctuations();

            // Load mood breakdown
            LoadMoodBreakdown();

            // Load top context tags
            LoadTopContextTags();

            // Load writing consistency
            LoadWritingConsistency();
        }

        private void LoadMoodFluctuations()
        {
            MoodFluctuations.Clear();

            // Generate sample mood fluctuation data for 4 weeks
            var random = new Random(42);
            var baseValue = 5.0;

            for (int week = 1; week <= 4; week++)
            {
                for (int day = 0; day < 7; day++)
                {
                    baseValue += random.NextDouble() * 0.5 - 0.2; // Slight upward trend
                    baseValue = Math.Max(3, Math.Min(10, baseValue)); // Clamp between 3-10

                    MoodFluctuations.Add(new MoodFluctuationPoint
                    {
                        Week = $"WEEK {week}",
                        DayIndex = (week - 1) * 7 + day,
                        Value = baseValue
                    });
                }
            }
        }

        private void LoadMoodBreakdown()
        {
            MoodBreakdown.Clear();
            var distribution = _database.GetMoodDistribution();
            var total = distribution.Values.Sum();

            if (total == 0)
            {
                // Sample data
                MoodBreakdown.Add(new MoodBreakdownItem { Mood = "Happy", Percentage = 45, Color = "#58A6FF" });
                MoodBreakdown.Add(new MoodBreakdownItem { Mood = "Calm", Percentage = 30, Color = "#22C55E" });
                MoodBreakdown.Add(new MoodBreakdownItem { Mood = "Anxious", Percentage = 25, Color = "#A855F7" });
            }
            else
            {
                var moodColors = new Dictionary<MoodType, string>
                {
                    { MoodType.Happy, "#58A6FF" },
                    { MoodType.Calm, "#22C55E" },
                    { MoodType.Anxious, "#A855F7" },
                    { MoodType.Productive, "#F0B429" },
                    { MoodType.Sad, "#F97316" },
                    { MoodType.Neutral, "#8B949E" }
                };

                var topMoods = distribution.OrderByDescending(kv => kv.Value).Take(3);
                foreach (var mood in topMoods)
                {
                    var percentage = (int)Math.Round((double)mood.Value / total * 100);
                    MoodBreakdown.Add(new MoodBreakdownItem
                    {
                        Mood = mood.Key.ToString(),
                        Percentage = percentage,
                        Color = moodColors.GetValueOrDefault(mood.Key, "#8B949E")
                    });
                }

                // Set top mood
                if (topMoods.Any())
                {
                    TopMood = topMoods.First().Key.ToString();
                }
            }
        }

        private void LoadTopContextTags()
        {
            TopContextTags.Clear();
            var tags = _database.GetFrequentTags(5);

            if (tags.Count == 0)
            {
                // Sample data
                TopContextTags.Add(new ContextTagItem { Tag = "#WorkLife", EntryCount = 24, BarWidth = 200, Color = "#58A6FF" });
                TopContextTags.Add(new ContextTagItem { Tag = "#Family", EntryCount = 18, BarWidth = 150, Color = "#F97316" });
                TopContextTags.Add(new ContextTagItem { Tag = "#Health", EntryCount = 12, BarWidth = 100, Color = "#22C55E" });
            }
            else
            {
                var maxCount = tags.Values.Max();
                var colors = new[] { "#58A6FF", "#F97316", "#22C55E", "#A855F7", "#F0B429" };
                int colorIndex = 0;

                foreach (var tag in tags.OrderByDescending(t => t.Value).Take(5))
                {
                    TopContextTags.Add(new ContextTagItem
                    {
                        Tag = $"#{tag.Key}",
                        EntryCount = tag.Value,
                        BarWidth = (int)(200.0 * tag.Value / maxCount),
                        Color = colors[colorIndex % colors.Length]
                    });
                    colorIndex++;
                }
            }
        }

        private void LoadWritingConsistency()
        {
            WritingConsistency.Clear();
            var weeklyWords = _database.GetWeeklyWordCounts();

            if (weeklyWords.All(w => w == 0))
            {
                // Sample data
                var sampleWords = new[] { 1200, 1500, 1800, 2100, 1700, 1900, 1600 };
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                for (int i = 0; i < 7; i++)
                {
                    WritingConsistency.Add(new DailyWordCount
                    {
                        Day = days[i],
                        WordCount = sampleWords[i],
                        BarHeight = (int)(80.0 * sampleWords[i] / 2100)
                    });
                }
                DailyAvgWords = (int)sampleWords.Average();
            }
            else
            {
                var maxWords = weeklyWords.Max();
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                for (int i = 0; i < Math.Min(7, weeklyWords.Count); i++)
                {
                    WritingConsistency.Add(new DailyWordCount
                    {
                        Day = days[i],
                        WordCount = weeklyWords[i],
                        BarHeight = maxWords > 0 ? (int)(80.0 * weeklyWords[i] / maxWords) : 0
                    });
                }
                DailyAvgWords = weeklyWords.Count > 0 ? (int)weeklyWords.Average() : 0;
            }
        }

        private void ExecuteSelectTimeRange(object? param)
        {
            if (param is string range)
            {
                SelectedTimeRange = range;
                OnPropertyChanged(nameof(IsLast7Days));
                OnPropertyChanged(nameof(IsThisMonth));
                OnPropertyChanged(nameof(IsCustomRange));
            }
        }

        private void ExecuteExportData()
        {
            // Export functionality would be implemented here
            System.Windows.MessageBox.Show("Export functionality coming soon!", "Export Data");
        }

        private void ExecuteNavigate(object? param)
        {
            if (param is string navItem)
            {
                SelectedNavItem = navItem;
                switch (navItem)
                {
                    case "Journal":
                        NavigationService.Instance.NavigateTo("JournalEntry");
                        break;
                    case "Calendar":
                        NavigationService.Instance.NavigateTo("Calendar");
                        break;
                    case "Dashboard":
                        NavigationService.Instance.NavigateTo("Dashboard");
                        break;
                }
            }
        }

        private void ExecuteLogout()
        {
            _authService.Logout();
            NavigationService.Instance.NavigateTo("Login");
        }
    }

    #region Helper Classes

    public class MoodFluctuationPoint
    {
        public string Week { get; set; } = string.Empty;
        public int DayIndex { get; set; }
        public double Value { get; set; }
    }

    public class MoodBreakdownItem
    {
        public string Mood { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public string Color { get; set; } = "#58A6FF";
    }

    public class ContextTagItem
    {
        public string Tag { get; set; } = string.Empty;
        public int EntryCount { get; set; }
        public int BarWidth { get; set; }
        public string Color { get; set; } = "#58A6FF";
    }

    public class DailyWordCount
    {
        public string Day { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public int BarHeight { get; set; }
    }

    #endregion
}
