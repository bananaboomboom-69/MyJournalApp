using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MyJournalApp.Models;
using MyJournalApp.Services;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Timeline/Journal entries list screen.
    /// Displays journal entries in a chronological list with search and filtering.
    /// </summary>
    public class TimelineViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly AuthenticationService _authService;

        private string _searchQuery = string.Empty;
        private string _selectedDateRange = "All Time";
        private string _selectedMoodFilter = "All";
        private string _selectedTagFilter = string.Empty;
        private string _selectedNavItem = "Timeline";
        private string _userName = "Jane Doe";
        private string _userPlan = "Free Plan";
        private bool _isLoading;
        private int _pageSize = 10;
        private int _currentPage = 0;
        private DateTime _exportStartDate = DateTime.Today.AddDays(-30);
        private DateTime _exportEndDate = DateTime.Today;

        #region Properties

        /// <summary>
        /// Search query for filtering entries.
        /// </summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterEntries();
                }
            }
        }

        /// <summary>
        /// Selected date range filter.
        /// </summary>
        public string SelectedDateRange
        {
            get => _selectedDateRange;
            set
            {
                if (SetProperty(ref _selectedDateRange, value))
                {
                    FilterEntries();
                }
            }
        }

        /// <summary>
        /// Selected mood filter.
        /// </summary>
        public string SelectedMoodFilter
        {
            get => _selectedMoodFilter;
            set
            {
                if (SetProperty(ref _selectedMoodFilter, value))
                {
                    FilterEntries();
                }
            }
        }

        /// <summary>
        /// Selected tag filter.
        /// </summary>
        public string SelectedTagFilter
        {
            get => _selectedTagFilter;
            set
            {
                if (SetProperty(ref _selectedTagFilter, value))
                {
                    OnPropertyChanged(nameof(HasTagFilter));
                    FilterEntries();
                }
            }
        }

        /// <summary>
        /// Whether a tag filter is active.
        /// </summary>
        public bool HasTagFilter => !string.IsNullOrEmpty(SelectedTagFilter);

        /// <summary>
        /// Currently selected navigation item.
        /// </summary>
        public string SelectedNavItem
        {
            get => _selectedNavItem;
            set => SetProperty(ref _selectedNavItem, value);
        }

        /// <summary>
        /// User's display name.
        /// </summary>
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>
        /// User's subscription plan.
        /// </summary>
        public string UserPlan
        {
            get => _userPlan;
            set => SetProperty(ref _userPlan, value);
        }

        /// <summary>
        /// Whether entries are being loaded.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// All journal entries.
        /// </summary>
        public ObservableCollection<TimelineEntry> Entries { get; } = new();

        /// <summary>
        /// Active tag filters.
        /// </summary>
        public ObservableCollection<string> ActiveFilters { get; } = new();

        /// <summary>
        /// Available date ranges.
        /// </summary>
        public ObservableCollection<string> DateRanges { get; } = new()
        {
            "All Time",
            "Today",
            "This Week",
            "This Month",
            "Last 30 Days",
            "Last 90 Days"
        };

        /// <summary>
        /// Available mood filters.
        /// </summary>
        public ObservableCollection<string> MoodOptions { get; } = new()
        {
            "All",
            "Happy",
            "Calm",
            "Anxious",
            "Productive",
            "Sad",
            "Neutral"
        };

        #endregion

        #region Commands

        public ICommand NewEntryCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand LoadMoreCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RemoveTagFilterCommand { get; }
        public ICommand OpenEntryCommand { get; }
        public ICommand ExportToPdfCommand { get; }

        #endregion

        #region Export Properties

        /// <summary>
        /// Start date for PDF export.
        /// </summary>
        public DateTime ExportStartDate
        {
            get => _exportStartDate;
            set => SetProperty(ref _exportStartDate, value);
        }

        /// <summary>
        /// End date for PDF export.
        /// </summary>
        public DateTime ExportEndDate
        {
            get => _exportEndDate;
            set => SetProperty(ref _exportEndDate, value);
        }

        #endregion

        public TimelineViewModel()
        {
            _database = DatabaseService.Instance;
            _authService = AuthenticationService.Instance;

            // Set user info
            if (_authService.CurrentUser != null)
            {
                UserName = _authService.CurrentUser.Username;
            }

            // Initialize commands
            NewEntryCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo("JournalEntry"));
            NavigateCommand = new RelayCommand(ExecuteNavigate);
            LoadMoreCommand = new RelayCommand(ExecuteLoadMore);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            RemoveTagFilterCommand = new RelayCommand(ExecuteRemoveTagFilter);
            OpenEntryCommand = new RelayCommand(ExecuteOpenEntry);
            ExportToPdfCommand = new RelayCommand(ExecuteExportToPdf);

            // Load entries
            LoadEntries();
        }

        private void LoadEntries()
        {
            Entries.Clear();
            var entries = _database.GetAllEntries();

            foreach (var entry in entries)
            {
                Entries.Add(CreateTimelineEntry(entry));
            }

            // If no entries, add sample data for demonstration
            if (Entries.Count == 0)
            {
                AddSampleEntries();
            }
        }

        private void AddSampleEntries()
        {
            Entries.Add(new TimelineEntry
            {
                Id = 1,
                Title = "Reflecting on quarterly goals",
                ContentPreview = "Today I realized that I've been pushing too hard on the productivity front without taking enough breaks. The team meeting went well, but I felt drained afterwards. I need to prioritize my mental health and maybe take a short...",
                Month = "OCT",
                Day = "24",
                Mood = MoodType.Productive,
                MoodIcon = "☀️",
                MoodColor = "#22C55E",
                Tags = new ObservableCollection<string> { "#Reflection", "#Work", "#Goals" }
            });

            Entries.Add(new TimelineEntry
            {
                Id = 2,
                Title = "Evening walk by the river",
                ContentPreview = "The sunset was absolutely stunning today. Orange and purple hues reflected on the water surface. It was a moment of pure tranquility. Captured a few photos for inspiration later.",
                Month = "OCT",
                Day = "22",
                Mood = MoodType.Calm,
                MoodIcon = "💧",
                MoodColor = "#58A6FF",
                Tags = new ObservableCollection<string> { "#Nature", "#Peace" },
                HasImage = true
            });

            Entries.Add(new TimelineEntry
            {
                Id = 3,
                Title = "Project deadline approaching",
                ContentPreview = "Feeling a bit overwhelmed with the new project scope. Need to break it down into smaller tasks. Wrote down a quick checklist to manage my anxiety.",
                Month = "OCT",
                Day = "20",
                Mood = MoodType.Anxious,
                MoodIcon = "⚡",
                MoodColor = "#F97316",
                Tags = new ObservableCollection<string> { "#Work", "#Anxiety" }
            });
        }

        private TimelineEntry CreateTimelineEntry(JournalEntry entry)
        {
            var moodIcons = new System.Collections.Generic.Dictionary<MoodType, string>
            {
                { MoodType.Happy, "☀️" },
                { MoodType.Calm, "💧" },
                { MoodType.Anxious, "⚡" },
                { MoodType.Productive, "🎯" },
                { MoodType.Sad, "🌧️" },
                { MoodType.Neutral, "☁️" }
            };

            var moodColors = new System.Collections.Generic.Dictionary<MoodType, string>
            {
                { MoodType.Happy, "#F0B429" },
                { MoodType.Calm, "#58A6FF" },
                { MoodType.Anxious, "#F97316" },
                { MoodType.Productive, "#22C55E" },
                { MoodType.Sad, "#8B5CF6" },
                { MoodType.Neutral, "#8B949E" }
            };

            return new TimelineEntry
            {
                Id = entry.Id,
                Title = entry.Title,
                ContentPreview = entry.Content.Length > 200 ? entry.Content.Substring(0, 200) + "..." : entry.Content,
                Month = entry.EntryDate.ToString("MMM").ToUpper(),
                Day = entry.EntryDate.Day.ToString(),
                Mood = entry.PrimaryMood,
                MoodIcon = moodIcons.GetValueOrDefault(entry.PrimaryMood, "📝"),
                MoodColor = moodColors.GetValueOrDefault(entry.PrimaryMood, "#8B949E"),
                Tags = new ObservableCollection<string>(entry.GetTagsList().Select(t => $"#{t}"))
            };
        }

        private void FilterEntries()
        {
            // Build search criteria from current filter values
            var criteria = BuildSearchCriteria();

            // Execute database-level search
            var allEntries = _database.SearchEntries(criteria);

            Entries.Clear();
            foreach (var entry in allEntries)
            {
                Entries.Add(CreateTimelineEntry(entry));
            }

            // If no entries after filtering and no filters applied, show sample data
            if (Entries.Count == 0 && !criteria.HasFilters)
            {
                AddSampleEntries();
            }
        }

        /// <summary>
        /// Builds a SearchCriteria object from current filter property values.
        /// </summary>
        private SearchCriteria BuildSearchCriteria()
        {
            var criteria = new SearchCriteria();

            // Keyword search
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                criteria.Keyword = SearchQuery;
            }

            // Date range
            var (startDate, endDate) = GetDateRangeFromSelection(SelectedDateRange);
            criteria.StartDate = startDate;
            criteria.EndDate = endDate;

            // Mood filter
            if (SelectedMoodFilter != "All" && Enum.TryParse<MoodType>(SelectedMoodFilter, out var moodType))
            {
                criteria.PrimaryMood = moodType;
                criteria.IncludeSecondaryMoods = true; // Also match secondary moods
            }

            // Tag filter
            if (!string.IsNullOrEmpty(SelectedTagFilter))
            {
                criteria.Tag = SelectedTagFilter;
            }

            return criteria;
        }

        /// <summary>
        /// Converts date range selection string to start/end dates.
        /// </summary>
        private (DateTime? StartDate, DateTime? EndDate) GetDateRangeFromSelection(string dateRange)
        {
            var today = DateTime.Today;

            return dateRange switch
            {
                "Today" => (today, today),
                "This Week" => (today.AddDays(-7), today),
                "This Month" => (new DateTime(today.Year, today.Month, 1), today),
                "Last 30 Days" => (today.AddDays(-30), today),
                "Last 90 Days" => (today.AddDays(-90), today),
                _ => (null, null) // "All Time" - no date filter
            };
        }

        private void ExecuteNavigate(object? param)
        {
            if (param is string navItem)
            {
                SelectedNavItem = navItem;
                switch (navItem)
                {
                    case "Write":
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

        private void ExecuteLoadMore()
        {
            _currentPage++;
            // Load more entries (pagination would be implemented here)
        }

        private void ExecuteClearFilters()
        {
            SearchQuery = string.Empty;
            SelectedDateRange = "All Time";
            SelectedMoodFilter = "All";
            SelectedTagFilter = string.Empty;
            ActiveFilters.Clear();
            LoadEntries();
        }

        private void ExecuteRemoveTagFilter(object? param)
        {
            SelectedTagFilter = string.Empty;
        }

        private void ExecuteOpenEntry(object? param)
        {
            if (param is int entryId)
            {
                // Navigate to edit entry
                NavigationService.Instance.NavigateTo("JournalEntry");
            }
        }

        /// <summary>
        /// Exports journal entries to PDF based on selected date range.
        /// </summary>
        private void ExecuteExportToPdf()
        {
            PdfExportService.Instance.ExportToPdf(ExportStartDate, ExportEndDate);
        }
    }

    #region Helper Classes

    /// <summary>
    /// Represents a journal entry in the timeline view.
    /// </summary>
    public class TimelineEntry : BaseViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Day { get; set; } = string.Empty;
        public MoodType Mood { get; set; }
        public string MoodIcon { get; set; } = "📝";
        public string MoodColor { get; set; } = "#8B949E";
        public ObservableCollection<string> Tags { get; set; } = new();
        public bool HasImage { get; set; }
    }

    #endregion
}
