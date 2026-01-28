using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for creating and editing journal entries
/// </summary>
public partial class EntryViewModel : BaseViewModel
{
    private readonly IJournalService _journalService;
    private readonly ITagService _tagService;

    [ObservableProperty]
    private JournalEntry _entry = new();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _entryDate = DateTime.Today;

    [ObservableProperty]
    private MoodType _primaryMood = MoodType.Neutral;

    [ObservableProperty]
    private MoodType? _secondaryMood1;

    [ObservableProperty]
    private MoodType? _secondaryMood2;

    [ObservableProperty]
    private List<Tag> _availableTags = new();

    [ObservableProperty]
    private List<Tag> _selectedTags = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private string _markdownPreview = string.Empty;

    public EntryViewModel(IJournalService journalService, ITagService tagService)
    {
        _journalService = journalService;
        _tagService = tagService;
    }

    public async Task InitializeAsync(int? entryId = null, DateTime? date = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            // Load available tags
            AvailableTags = await _tagService.GetAllTagsAsync();

            if (entryId.HasValue && entryId.Value > 0)
            {
                // Edit existing entry
                var entry = await _journalService.GetEntryByIdAsync(entryId.Value);
                if (entry != null)
                {
                    LoadEntry(entry);
                    IsEditing = true;
                }
            }
            else if (date.HasValue)
            {
                // Check for existing entry on this date
                var existingEntry = await _journalService.GetEntryByDateAsync(date.Value);
                if (existingEntry != null)
                {
                    LoadEntry(existingEntry);
                    IsEditing = true;
                }
                else
                {
                    EntryDate = date.Value;
                    IsEditing = false;
                }
            }
            else
            {
                // Check for existing entry today
                var todayEntry = await _journalService.GetEntryByDateAsync(DateTime.Today);
                if (todayEntry != null)
                {
                    LoadEntry(todayEntry);
                    IsEditing = true;
                }
                else
                {
                    IsEditing = false;
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to initialize entry: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadEntry(JournalEntry entry)
    {
        Entry = entry;
        Title = entry.Title;
        Content = entry.Content;
        EntryDate = entry.EntryDate;
        PrimaryMood = entry.PrimaryMood;
        SecondaryMood1 = entry.SecondaryMood1;
        SecondaryMood2 = entry.SecondaryMood2;
        SelectedTags = new List<Tag>(entry.Tags);
        UpdatePreview();
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Title))
        {
            SetError("Title is required");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            Entry.Title = Title;
            Entry.Content = Content;
            Entry.EntryDate = EntryDate.Date;
            Entry.PrimaryMood = PrimaryMood;
            Entry.SecondaryMood1 = SecondaryMood1;
            Entry.SecondaryMood2 = SecondaryMood2;
            Entry.Tags = SelectedTags;

            await _journalService.SaveEntryAsync(Entry);
            IsSaved = true;
            IsEditing = true;
        }
        catch (Exception ex)
        {
            SetError($"Failed to save entry: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (IsBusy || Entry.Id == 0) return;

        try
        {
            IsBusy = true;
            ClearError();

            await _journalService.DeleteEntryAsync(Entry.Id);
        }
        catch (Exception ex)
        {
            SetError($"Failed to delete entry: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdatePreview()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            MarkdownPreview = string.Empty;
            return;
        }

        MarkdownPreview = Markdig.Markdown.ToHtml(Content);
    }

    public void ToggleTag(Tag tag)
    {
        var selected = new List<Tag>(SelectedTags);

        if (selected.Any(t => t.Id == tag.Id))
        {
            selected.RemoveAll(t => t.Id == tag.Id);
        }
        else
        {
            selected.Add(tag);
        }

        SelectedTags = selected;
    }

    public bool IsTagSelected(Tag tag)
    {
        return SelectedTags.Any(t => t.Id == tag.Id);
    }

    public void SetSecondaryMood(MoodType mood, int slot)
    {
        if (slot == 1)
        {
            SecondaryMood1 = SecondaryMood1 == mood ? null : mood;
        }
        else if (slot == 2)
        {
            SecondaryMood2 = SecondaryMood2 == mood ? null : mood;
        }
    }
}
