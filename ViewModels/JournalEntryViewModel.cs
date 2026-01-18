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
        private MoodType _primaryMood = MoodType.Calm;
        private string _selectedCategory = "Daily Reflection";
        private string _newTag = string.Empty;
        private string _lastSavedText = string.Empty;
        private bool _hasUnsavedChanges;
        private string _selectedNavItem = "Journal";
        private string _userName = "Alex Morgan";
        private string _userMotto = "Keep growing";

        // Edit mode tracking
        private bool _isEditMode;
        private int _currentEntryId;

        #region Properties

        /// <summary>
        /// Whether we're editing an existing entry vs creating new.
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(DeleteButtonVisible));
                }
            }
        }

        /// <summary>
        /// ID of the current entry being edited.
        /// </summary>
        public int CurrentEntryId
        {
            get => _currentEntryId;
            set => SetProperty(ref _currentEntryId, value);
        }

        /// <summary>
        /// Button text changes based on mode.
        /// </summary>
        public string SaveButtonText => IsEditMode ? "Update Entry" : "Save Entry";

        /// <summary>
        /// Delete button only visible in edit mode.
        /// </summary>
        public bool DeleteButtonVisible => IsEditMode;

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
                    // Reload entry for the new date
                    LoadExistingEntry();
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
        /// Primary mood (required).
        /// </summary>
        public MoodType PrimaryMood
        {
            get => _primaryMood;
            set
            {
                if (SetProperty(ref _primaryMood, value))
                {
                    HasUnsavedChanges = true;
                    // Remove from secondary if selected as primary
                    if (SecondaryMoods.Contains(value))
                    {
                        SecondaryMoods.Remove(value);
                    }
                    UpdateMoodSelections();
                }
            }
        }

        /// <summary>
        /// Secondary moods collection (max 2).
        /// </summary>
        public ObservableCollection<MoodType> SecondaryMoods { get; } = new();

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

        // Primary mood selection states
        public bool IsHappySelected => PrimaryMood == MoodType.Happy;
        public bool IsCalmSelected => PrimaryMood == MoodType.Calm;
        public bool IsProductiveSelected => PrimaryMood == MoodType.Productive;
        public bool IsAnxiousSelected => PrimaryMood == MoodType.Anxious;
        public bool IsSadSelected => PrimaryMood == MoodType.Sad;

        // Secondary mood selection states
        public bool IsHappySecondary => SecondaryMoods.Contains(MoodType.Happy);
        public bool IsCalmSecondary => SecondaryMoods.Contains(MoodType.Calm);
        public bool IsProductiveSecondary => SecondaryMoods.Contains(MoodType.Productive);
        public bool IsAnxiousSecondary => SecondaryMoods.Contains(MoodType.Anxious);
        public bool IsSadSecondary => SecondaryMoods.Contains(MoodType.Sad);

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
        public ICommand SelectPrimaryMoodCommand { get; }
        public ICommand ToggleSecondaryMoodCommand { get; }
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
            DeleteEntryCommand = new RelayCommand(ExecuteDeleteEntry, () => IsEditMode);
            AddTagCommand = new RelayCommand(ExecuteAddTag, CanAddTag);
            RemoveTagCommand = new RelayCommand(ExecuteRemoveTag);
            SelectPrimaryMoodCommand = new RelayCommand(ExecuteSelectPrimaryMood);
            ToggleSecondaryMoodCommand = new RelayCommand(ExecuteToggleSecondaryMood);
            PreviousDayCommand = new RelayCommand(() => EntryDate = EntryDate.AddDays(-1));
            NextDayCommand = new RelayCommand(() => EntryDate = EntryDate.AddDays(1));
            NavigateCommand = new RelayCommand((param) => ExecuteNavigate(param as string));
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Subscribe to secondary moods changes
            SecondaryMoods.CollectionChanged += (s, e) =>
            {
                HasUnsavedChanges = true;
                UpdateSecondaryMoodSelections();
            };

            // Load existing entry for today if any
            LoadExistingEntry();
        }

        /// <summary>
        /// Loads an existing entry for the current EntryDate if one exists.
        /// </summary>
        private void LoadExistingEntry()
        {
            var existingEntry = _database.GetEntryByDate(EntryDate);

            if (existingEntry != null)
            {
                // Edit mode - populate form with existing data
                IsEditMode = true;
                CurrentEntryId = existingEntry.Id;
                Title = existingEntry.Title;
                Content = existingEntry.Content;
                _primaryMood = existingEntry.PrimaryMood; // Use field to avoid triggering change
                OnPropertyChanged(nameof(PrimaryMood));

                // Load secondary moods
                SecondaryMoods.Clear();
                foreach (var mood in existingEntry.GetSecondaryMoods())
                {
                    SecondaryMoods.Add(mood);
                }

                // Load tags
                Tags.Clear();
                foreach (var tag in existingEntry.GetTagsList())
                {
                    Tags.Add(tag.StartsWith("#") ? tag : "#" + tag);
                }

                LastSavedText = existingEntry.UpdatedAt.HasValue
                    ? $"Last updated {existingEntry.UpdatedAt:g}"
                    : $"Created {existingEntry.CreatedAt:g}";
            }
            else
            {
                // Create mode - reset form
                IsEditMode = false;
                CurrentEntryId = 0;
                Title = string.Empty;
                Content = string.Empty;
                _primaryMood = MoodType.Calm;
                OnPropertyChanged(nameof(PrimaryMood));
                SecondaryMoods.Clear();
                Tags.Clear();
                LastSavedText = string.Empty;
            }

            HasUnsavedChanges = false;
            UpdateMoodSelections();
            UpdateSecondaryMoodSelections();
        }

        /// <summary>
        /// Resets the form to create mode.
        /// </summary>
        private void ResetForm()
        {
            IsEditMode = false;
            CurrentEntryId = 0;
            Title = string.Empty;
            Content = string.Empty;
            PrimaryMood = MoodType.Calm;
            SecondaryMoods.Clear();
            Tags.Clear();
            LastSavedText = string.Empty;
            HasUnsavedChanges = false;
        }

        private void UpdateMoodSelections()
        {
            OnPropertyChanged(nameof(IsHappySelected));
            OnPropertyChanged(nameof(IsCalmSelected));
            OnPropertyChanged(nameof(IsProductiveSelected));
            OnPropertyChanged(nameof(IsAnxiousSelected));
            OnPropertyChanged(nameof(IsSadSelected));
        }

        private void UpdateSecondaryMoodSelections()
        {
            OnPropertyChanged(nameof(IsHappySecondary));
            OnPropertyChanged(nameof(IsCalmSecondary));
            OnPropertyChanged(nameof(IsProductiveSecondary));
            OnPropertyChanged(nameof(IsAnxiousSecondary));
            OnPropertyChanged(nameof(IsSadSecondary));
        }

        private bool CanSaveEntry() => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Content);

        private void ExecuteSaveEntry()
        {
            var wordCount = string.IsNullOrWhiteSpace(Content) ? 0 :
                Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if (IsEditMode)
            {
                // Update existing entry
                var entry = new JournalEntry
                {
                    Id = CurrentEntryId,
                    Title = Title,
                    Content = Content,
                    PrimaryMood = PrimaryMood,
                    Tags = string.Join(",", Tags.Select(t => t.TrimStart('#'))),
                    WordCount = wordCount,
                    EntryDate = EntryDate
                };
                entry.SetSecondaryMoods(SecondaryMoods);

                _database.UpdateEntry(entry);
                LastSavedText = "Updated just now";
            }
            else
            {
                // Create new entry
                var entry = new JournalEntry
                {
                    Title = Title,
                    Content = Content,
                    PrimaryMood = PrimaryMood,
                    Tags = string.Join(",", Tags.Select(t => t.TrimStart('#'))),
                    WordCount = wordCount,
                    EntryDate = EntryDate,
                    CreatedAt = DateTime.Now
                };
                entry.SetSecondaryMoods(SecondaryMoods);

                try
                {
                    _database.CreateEntry(entry);
                    LastSavedText = "Saved just now";
                    // Switch to edit mode after first save
                    LoadExistingEntry();
                }
                catch (InvalidOperationException ex)
                {
                    // Entry already exists for this date - should not happen with proper UI flow
                    System.Windows.MessageBox.Show(ex.Message, "Cannot Create Entry");
                    return;
                }
            }

            HasUnsavedChanges = false;

            // Navigate back to dashboard
            NavigationService.Instance.NavigateTo("Dashboard");
        }

        private void ExecuteDeleteEntry()
        {
            if (!IsEditMode || CurrentEntryId == 0)
                return;

            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to delete this entry? This action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _database.DeleteEntry(CurrentEntryId);
                ResetForm();
                LastSavedText = "Entry deleted";
            }
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

        private void ExecuteSelectPrimaryMood(object? param)
        {
            if (param is string moodStr && Enum.TryParse<MoodType>(moodStr, out var mood))
            {
                PrimaryMood = mood;
            }
        }

        private void ExecuteToggleSecondaryMood(object? param)
        {
            if (param is string moodStr && Enum.TryParse<MoodType>(moodStr, out var mood))
            {
                // Cannot select primary mood as secondary
                if (mood == PrimaryMood)
                    return;

                if (SecondaryMoods.Contains(mood))
                {
                    // Remove if already selected
                    SecondaryMoods.Remove(mood);
                }
                else if (SecondaryMoods.Count < 2)
                {
                    // Add if under limit
                    SecondaryMoods.Add(mood);
                }
                // If already at 2 secondary moods, do nothing (validation)
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
