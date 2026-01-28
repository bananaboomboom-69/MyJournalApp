using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for the paginated journal list
/// </summary>
public partial class JournalListViewModel : BaseViewModel
{
    private readonly IJournalService _journalService;
    private readonly ITagService _tagService;

    [ObservableProperty]
    private List<JournalEntry> _entries = new();

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalEntries;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private MoodType? _filterMood;

    [ObservableProperty]
    private int? _filterTagId;

    [ObservableProperty]
    private DateTime? _filterStartDate;

    [ObservableProperty]
    private DateTime? _filterEndDate;

    [ObservableProperty]
    private List<Tag> _availableTags = new();

    [ObservableProperty]
    private bool _hasFilters;

    public JournalListViewModel(IJournalService journalService, ITagService tagService)
    {
        _journalService = journalService;
        _tagService = tagService;
        Title = "Journal";
    }

    [RelayCommand]
    public async Task LoadEntriesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            // Load tags for filtering
            AvailableTags = await _tagService.GetAllTagsAsync();

            // Check if any filters are active
            HasFilters = !string.IsNullOrWhiteSpace(SearchTerm) ||
                         FilterMood.HasValue ||
                         FilterTagId.HasValue ||
                         FilterStartDate.HasValue ||
                         FilterEndDate.HasValue;

            if (HasFilters && (FilterMood.HasValue || FilterTagId.HasValue || FilterStartDate.HasValue || FilterEndDate.HasValue))
            {
                // Use filter method
                var filtered = await _journalService.FilterEntriesAsync(
                    FilterMood,
                    FilterTagId,
                    FilterStartDate,
                    FilterEndDate);

                // Apply search if present
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    filtered = filtered.Where(e =>
                        e.Title.ToLower().Contains(term) ||
                        e.Content.ToLower().Contains(term)).ToList();
                }

                TotalEntries = filtered.Count;
                TotalPages = (int)Math.Ceiling((double)TotalEntries / PageSize);
                CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

                Entries = filtered
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            else
            {
                // Use pagination with optional search
                var (entries, total) = await _journalService.GetPaginatedEntriesAsync(
                    CurrentPage,
                    PageSize,
                    string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm);

                Entries = entries;
                TotalEntries = total;
                TotalPages = (int)Math.Ceiling((double)total / PageSize);
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load entries: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadEntriesAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadEntriesAsync();
        }
    }

    [RelayCommand]
    public async Task GoToPageAsync(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await LoadEntriesAsync();
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadEntriesAsync();
    }

    [RelayCommand]
    public async Task ApplyFiltersAsync()
    {
        CurrentPage = 1;
        await LoadEntriesAsync();
    }

    [RelayCommand]
    public async Task ClearFiltersAsync()
    {
        SearchTerm = string.Empty;
        FilterMood = null;
        FilterTagId = null;
        FilterStartDate = null;
        FilterEndDate = null;
        CurrentPage = 1;
        await LoadEntriesAsync();
    }

    public List<int> GetPageNumbers()
    {
        var pages = new List<int>();
        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, CurrentPage + 2);

        for (var i = startPage; i <= endPage; i++)
        {
            pages.Add(i);
        }

        return pages;
    }
}
