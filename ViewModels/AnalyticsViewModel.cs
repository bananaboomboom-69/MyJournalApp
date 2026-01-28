using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for the analytics page
/// </summary>
public partial class AnalyticsViewModel : BaseViewModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IStreakService _streakService;

    [ObservableProperty]
    private List<MoodDistribution> _moodDistribution = new();

    [ObservableProperty]
    private List<TagUsage> _tagUsage = new();

    [ObservableProperty]
    private List<WordCountTrend> _wordCountTrend = new();

    [ObservableProperty]
    private List<MonthlyStats> _monthlyStats = new();

    [ObservableProperty]
    private StreakInfo _streakInfo = new();

    [ObservableProperty]
    private int _totalWordCount;

    [ObservableProperty]
    private double _averageWordCount;

    [ObservableProperty]
    private MoodType? _mostFrequentMood;

    public AnalyticsViewModel(IAnalyticsService analyticsService, IStreakService streakService)
    {
        _analyticsService = analyticsService;
        _streakService = streakService;
        Title = "Analytics";
    }

    [RelayCommand]
    public async Task LoadAnalyticsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            // Load all analytics data
            MoodDistribution = await _analyticsService.GetMoodDistributionAsync();
            TagUsage = await _analyticsService.GetTagUsageAsync(10);
            WordCountTrend = await _analyticsService.GetWordCountTrendAsync(30);
            MonthlyStats = await _analyticsService.GetMonthlyStatsAsync(6);
            StreakInfo = await _streakService.GetStreakInfoAsync();
            TotalWordCount = await _analyticsService.GetTotalWordCountAsync();
            AverageWordCount = await _analyticsService.GetAverageWordCountAsync();
            MostFrequentMood = await _analyticsService.GetMostFrequentMoodAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load analytics: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
