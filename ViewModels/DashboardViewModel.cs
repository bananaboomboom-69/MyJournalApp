using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IJournalService _journalService;
    private readonly IStreakService _streakService;
    private readonly IAnalyticsService _analyticsService;

    [ObservableProperty]
    private StreakInfo _streakInfo = new();

    [ObservableProperty]
    private List<JournalEntry> _recentEntries = new();

    [ObservableProperty]
    private JournalEntry? _todayEntry;

    [ObservableProperty]
    private List<MoodDistribution> _moodDistribution = new();

    [ObservableProperty]
    private int _totalEntries;

    [ObservableProperty]
    private int _totalWords;

    [ObservableProperty]
    private MoodType? _mostFrequentMood;

    public DashboardViewModel(
        IJournalService journalService,
        IStreakService streakService,
        IAnalyticsService analyticsService)
    {
        _journalService = journalService;
        _streakService = streakService;
        _analyticsService = analyticsService;
        Title = "Dashboard";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            // Load streak info
            StreakInfo = await _streakService.GetStreakInfoAsync();

            // Load today's entry
            TodayEntry = await _journalService.GetEntryByDateAsync(DateTime.Today);

            // Load recent entries
            var (entries, _) = await _journalService.GetPaginatedEntriesAsync(1, 5);
            RecentEntries = entries;

            // Load analytics
            MoodDistribution = await _analyticsService.GetMoodDistributionAsync();
            TotalEntries = await _journalService.GetTotalEntriesCountAsync();
            TotalWords = await _analyticsService.GetTotalWordCountAsync();
            MostFrequentMood = await _analyticsService.GetMostFrequentMoodAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
