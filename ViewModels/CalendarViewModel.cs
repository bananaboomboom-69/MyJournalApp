using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MyJournalApp.Models;
using MyJournalApp.Services;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Calendar screen.
    /// Displays journal entries in a monthly calendar grid with mood filtering.
    /// </summary>
    public class CalendarViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly AuthenticationService _authService;

        private DateTime _currentMonth;
        private string _selectedView = "Week";
        private string _selectedMoodFilter = "All Moods";
        private string _searchQuery = string.Empty;
        private string _monthSubtitle = "Focus on gratitude and calmness this month.";
        private string _selectedNavItem = "Calendar";
        private string _userName = "Sarah Jenkins";
        private string _userPlan = "Premium Member";

        #region Properties

        /// <summary>
        /// Currently displayed month.
        /// </summary>
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                if (SetProperty(ref _currentMonth, value))
                {
                    OnPropertyChanged(nameof(MonthYearDisplay));
                    LoadCalendarData();
                }
            }
        }

        /// <summary>
        /// Month and year display text.
        /// </summary>
        public string MonthYearDisplay => CurrentMonth.ToString("MMMM yyyy");

        /// <summary>
        /// Monthly motivational subtitle.
        /// </summary>
        public string MonthSubtitle
        {
            get => _monthSubtitle;
            set => SetProperty(ref _monthSubtitle, value);
        }

        /// <summary>
        /// Selected view mode (Month, Week, Day).
        /// </summary>
        public string SelectedView
        {
            get => _selectedView;
            set => SetProperty(ref _selectedView, value);
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
                    LoadCalendarData();
                }
            }
        }

        /// <summary>
        /// Search query for filtering entries.
        /// </summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
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

        // View mode flags
        public bool IsMonthView => SelectedView == "Month";
        public bool IsWeekView => SelectedView == "Week";
        public bool IsDayView => SelectedView == "Day";

        /// <summary>
        /// Calendar days for the current month view.
        /// </summary>
        public ObservableCollection<CalendarDay> CalendarDays { get; } = new();

        /// <summary>
        /// Available mood filters.
        /// </summary>
        public ObservableCollection<MoodFilter> MoodFilters { get; } = new();

        #endregion

        #region Commands

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand SelectViewCommand { get; }
        public ICommand SelectMoodFilterCommand { get; }
        public ICommand NewEntryCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand OpenEntryCommand { get; }

        #endregion

        public CalendarViewModel()
        {
            _database = DatabaseService.Instance;
            _authService = AuthenticationService.Instance;
            _currentMonth = DateTime.Today;

            // Set user info
            if (_authService.CurrentUser != null)
            {
                UserName = _authService.CurrentUser.Username;
            }

            // Initialize mood filters
            InitializeMoodFilters();

            // Initialize commands
            PreviousMonthCommand = new RelayCommand(() => CurrentMonth = CurrentMonth.AddMonths(-1));
            NextMonthCommand = new RelayCommand(() => CurrentMonth = CurrentMonth.AddMonths(1));
            SelectViewCommand = new RelayCommand(ExecuteSelectView);
            SelectMoodFilterCommand = new RelayCommand(ExecuteSelectMoodFilter);
            NewEntryCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo("JournalEntry"));
            NavigateCommand = new RelayCommand(ExecuteNavigate);
            OpenEntryCommand = new RelayCommand(ExecuteOpenEntry);

            // Load calendar data
            LoadCalendarData();
        }

        private void InitializeMoodFilters()
        {
            MoodFilters.Add(new MoodFilter { Name = "All Moods", Color = "#30363D", IsSelected = true });
            MoodFilters.Add(new MoodFilter { Name = "Happy", Color = "#F0B429", IsSelected = false });
            MoodFilters.Add(new MoodFilter { Name = "Calm", Color = "#58A6FF", IsSelected = false });
            MoodFilters.Add(new MoodFilter { Name = "Anxious", Color = "#F97316", IsSelected = false });
            MoodFilters.Add(new MoodFilter { Name = "Productive", Color = "#22C55E", IsSelected = false });
        }

        private void LoadCalendarData()
        {
            CalendarDays.Clear();

            // Get the first day of the month
            var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Calculate the start date (Monday of the week containing the first day)
            int daysFromMonday = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
            var startDate = firstDayOfMonth.AddDays(-daysFromMonday);

            // Get all entries for the visible range
            var entries = _database.GetAllEntries()
                .Where(e => e.EntryDate >= startDate && e.EntryDate <= lastDayOfMonth.AddDays(7))
                .ToList();

            // Apply mood filter
            if (SelectedMoodFilter != "All Moods")
            {
                var moodType = Enum.Parse<MoodType>(SelectedMoodFilter);
                entries = entries.Where(e => e.PrimaryMood == moodType).ToList();
            }

            // Generate 6 weeks (42 days)
            for (int i = 0; i < 42; i++)
            {
                var date = startDate.AddDays(i);
                var dayEntries = entries.Where(e => e.EntryDate.Date == date.Date).ToList();

                var calendarDay = new CalendarDay
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsCurrentMonth = date.Month == CurrentMonth.Month,
                    IsToday = date.Date == DateTime.Today,
                    Entries = new ObservableCollection<CalendarEntry>()
                };

                foreach (var entry in dayEntries)
                {
                    calendarDay.Entries.Add(new CalendarEntry
                    {
                        Id = entry.Id,
                        Title = entry.Title,
                        Mood = entry.PrimaryMood.ToString().ToUpper(),
                        MoodColor = GetMoodColor(entry.PrimaryMood),
                        MoodBackground = GetMoodBackground(entry.PrimaryMood)
                    });
                }

                CalendarDays.Add(calendarDay);
            }
        }

        private string GetMoodColor(MoodType mood) => mood switch
        {
            MoodType.Happy => "#F0B429",
            MoodType.Calm => "#58A6FF",
            MoodType.Anxious => "#F97316",
            MoodType.Productive => "#22C55E",
            MoodType.Sad => "#8B5CF6",
            MoodType.Neutral => "#8B949E",
            _ => "#8B949E"
        };

        private string GetMoodBackground(MoodType mood) => mood switch
        {
            MoodType.Happy => "#3D3520",
            MoodType.Calm => "#1A2D3D",
            MoodType.Anxious => "#3D2A1A",
            MoodType.Productive => "#1A3D20",
            MoodType.Sad => "#2D1F3D",
            MoodType.Neutral => "#21262D",
            _ => "#21262D"
        };

        private void ExecuteSelectView(object? param)
        {
            if (param is string view)
            {
                SelectedView = view;
                OnPropertyChanged(nameof(IsMonthView));
                OnPropertyChanged(nameof(IsWeekView));
                OnPropertyChanged(nameof(IsDayView));
            }
        }

        private void ExecuteSelectMoodFilter(object? param)
        {
            if (param is string filterName)
            {
                SelectedMoodFilter = filterName;
                foreach (var filter in MoodFilters)
                {
                    filter.IsSelected = filter.Name == filterName;
                }
            }
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
                    case "Dashboard":
                        NavigationService.Instance.NavigateTo("Dashboard");
                        break;
                }
            }
        }

        private void ExecuteOpenEntry(object? param)
        {
            if (param is int entryId)
            {
                // Navigate to edit entry (future feature)
                NavigationService.Instance.NavigateTo("JournalEntry");
            }
        }
    }

    #region Helper Classes

    /// <summary>
    /// Represents a day in the calendar grid.
    /// </summary>
    public class CalendarDay : BaseViewModel
    {
        public DateTime Date { get; set; }
        public int DayNumber { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public ObservableCollection<CalendarEntry> Entries { get; set; } = new();
        public bool HasEntries => Entries.Count > 0;
    }

    /// <summary>
    /// Represents a journal entry in the calendar.
    /// </summary>
    public class CalendarEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Mood { get; set; } = string.Empty;
        public string MoodColor { get; set; } = "#8B949E";
        public string MoodBackground { get; set; } = "#21262D";
    }

    /// <summary>
    /// Represents a mood filter option.
    /// </summary>
    public class MoodFilter : BaseViewModel
    {
        private bool _isSelected;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#8B949E";
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    #endregion
}
