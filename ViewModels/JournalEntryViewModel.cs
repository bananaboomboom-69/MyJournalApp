using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MyJournalApp.Models;
using MyJournalApp.Services;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Journal Entry screen.
    /// Handles creating and editing journal entries with mood, category, and tags.
    /// </summary>
    public class JournalEntryViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly AuthenticationService _authService;

        private string _title = string.Empty;
        private string _content = string.Empty;
        private DateTime _entryDate = DateTime.Today;
        private MoodType _selectedMood = MoodType.Calm;
        private string _selectedCategory = "Daily Reflection";
        private string _newTag = string.Empty;
        private string _lastSavedText = string.Empty;
        private bool _hasUnsavedChanges;
        private string _selectedNavItem = "Journal";
        private string _userName = "Alex Morgan";
        private string _userMotto = "Keep growing";

        #region Properties

        /// <summary>
        /// Title of the journal entry.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                    HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// Main content of the journal entry.
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                    HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// Date of the entry.
        /// </summary>
        public DateTime EntryDate
        {
            get => _entryDate;
            set
            {
                if (SetProperty(ref _entryDate, value))
                {
                    OnPropertyChanged(nameof(EntryDateFormatted));
                    OnPropertyChanged(nameof(TodayLabel));
                }
            }
        }

        /// <summary>
        /// Formatted entry date for display.
        /// </summary>
        public string EntryDateFormatted => EntryDate.ToString("dddd, MMMM d");

        /// <summary>
        /// Label showing if this is today's entry.
        /// </summary>
        public string TodayLabel => EntryDate.Date == DateTime.Today ? "TODAY'S ENTRY" : "PAST ENTRY";

        /// <summary>
        /// Currently selected mood.
        /// </summary>
        public MoodType SelectedMood
        {
            get => _selectedMood;
            set
            {
                if (SetProperty(ref _selectedMood, value))
                {
                    HasUnsavedChanges = true;
                    UpdateMoodSelections();
                }
            }
        }

        /// <summary>
        /// Selected entry category.
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// New tag being entered.
        /// </summary>
        public string NewTag
        {
            get => _newTag;
            set => SetProperty(ref _newTag, value);
        }

        /// <summary>
        /// Last saved timestamp text.
        /// </summary>
        public string LastSavedText
        {
            get => _lastSavedText;
            set => SetProperty(ref _lastSavedText, value);
        }

        /// <summary>
        /// Whether there are unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
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
        /// User's motto/tagline.
        /// </summary>
        public string UserMotto
        {
            get => _userMotto;
            set => SetProperty(ref _userMotto, value);
        }

        // Mood selection states
        public bool IsHappySelected => SelectedMood == MoodType.Happy;
        public bool IsCalmSelected => SelectedMood == MoodType.Calm;
        public bool IsProductiveSelected => SelectedMood == MoodType.Productive;
        public bool IsAnxiousSelected => SelectedMood == MoodType.Anxious;
        public bool IsSadSelected => SelectedMood == MoodType.Sad;

        /// <summary>
        /// List of tags for this entry.
        /// </summary>
        public ObservableCollection<string> Tags { get; } = new();

        /// <summary>
        /// Available categories.
        /// </summary>
        public ObservableCollection<string> Categories { get; } = new()
        {
            "Daily Reflection",
            "Gratitude",
            "Goals",
            "Ideas",
            "Work",
            "Personal"
        };

        #endregion

        #region Commands

        public ICommand SaveEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand SelectMoodCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion

        public JournalEntryViewModel()
        {
            _database = DatabaseService.Instance;
            _authService = AuthenticationService.Instance;

            // Set user info
            if (_authService.CurrentUser != null)
            {
                UserName = _authService.CurrentUser.Username;
            }

            // Initialize commands
            SaveEntryCommand = new RelayCommand(ExecuteSaveEntry, CanSaveEntry);
            DeleteEntryCommand = new RelayCommand(ExecuteDeleteEntry);
            AddTagCommand = new RelayCommand(ExecuteAddTag, CanAddTag);
            RemoveTagCommand = new RelayCommand(ExecuteRemoveTag);
            SelectMoodCommand = new RelayCommand(ExecuteSelectMood);
            PreviousDayCommand = new RelayCommand(() => EntryDate = EntryDate.AddDays(-1));
            NextDayCommand = new RelayCommand(() => EntryDate = EntryDate.AddDays(1));
            NavigateCommand = new RelayCommand((param) => ExecuteNavigate(param as string));
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Add sample tags
            Tags.Add("#productivity");
            Tags.Add("#mental-health");
        }

        private void UpdateMoodSelections()
        {
            OnPropertyChanged(nameof(IsHappySelected));
            OnPropertyChanged(nameof(IsCalmSelected));
            OnPropertyChanged(nameof(IsProductiveSelected));
            OnPropertyChanged(nameof(IsAnxiousSelected));
            OnPropertyChanged(nameof(IsSadSelected));
        }

        private bool CanSaveEntry() => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Content);

        private void ExecuteSaveEntry()
        {
            var wordCount = string.IsNullOrWhiteSpace(Content) ? 0 : 
                Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            var entry = new JournalEntry
            {
                Title = Title,
                Content = Content,
                Mood = SelectedMood,
                Tags = string.Join(",", Tags.Select(t => t.TrimStart('#'))),
                WordCount = wordCount,
                EntryDate = EntryDate
            };

            _database.CreateEntry(entry);
            HasUnsavedChanges = false;
            LastSavedText = "Last saved just now";

            // Navigate back to dashboard
            NavigationService.Instance.NavigateTo("Dashboard");
        }

        private void ExecuteDeleteEntry()
        {
            // Clear the form
            Title = string.Empty;
            Content = string.Empty;
            Tags.Clear();
            SelectedMood = MoodType.Calm;
            HasUnsavedChanges = false;
        }

        private bool CanAddTag() => !string.IsNullOrWhiteSpace(NewTag);

        private void ExecuteAddTag()
        {
            var tag = NewTag.Trim();
            if (!tag.StartsWith("#"))
                tag = "#" + tag;

            if (!Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                Tags.Add(tag);
                HasUnsavedChanges = true;
            }
            NewTag = string.Empty;
        }

        private void ExecuteRemoveTag(object? param)
        {
            if (param is string tag && Tags.Contains(tag))
            {
                Tags.Remove(tag);
                HasUnsavedChanges = true;
            }
        }

        private void ExecuteSelectMood(object? param)
        {
            if (param is string moodStr && Enum.TryParse<MoodType>(moodStr, out var mood))
            {
                SelectedMood = mood;
            }
        }

        private void ExecuteNavigate(string? navItem)
        {
            if (string.IsNullOrEmpty(navItem)) return;

            SelectedNavItem = navItem;
            if (navItem == "Dashboard" || navItem == "Calendar" || navItem == "Analytics")
            {
                NavigationService.Instance.NavigateTo("Dashboard");
            }
        }

        private void ExecuteLogout()
        {
            _authService.Logout();
            NavigationService.Instance.NavigateTo("Login");
        }
    }
}
