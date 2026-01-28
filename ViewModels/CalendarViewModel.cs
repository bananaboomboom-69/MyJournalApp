using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for the calendar view
/// </summary>
public partial class CalendarViewModel : BaseViewModel
{
    private readonly IJournalService _journalService;

    [ObservableProperty]
    private int _currentYear;

    [ObservableProperty]
    private int _currentMonth;

    [ObservableProperty]
    private string _monthYearDisplay = string.Empty;

    [ObservableProperty]
    private List<CalendarDay> _calendarDays = new();

    [ObservableProperty]
    private JournalEntry? _selectedEntry;

    [ObservableProperty]
    private DateTime? _selectedDate;

    public CalendarViewModel(IJournalService journalService)
    {
        _journalService = journalService;
        Title = "Calendar";

        var today = DateTime.Today;
        CurrentYear = today.Year;
        CurrentMonth = today.Month;
    }

    [RelayCommand]
    public async Task LoadCalendarAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            // Update display
            MonthYearDisplay = new DateTime(CurrentYear, CurrentMonth, 1).ToString("MMMM yyyy");

            // Get dates with entries
            var datesWithEntries = await _journalService.GetDatesWithEntriesAsync(CurrentYear, CurrentMonth);
            var entryDates = datesWithEntries.ToHashSet();

            // Build calendar grid
            var firstDayOfMonth = new DateTime(CurrentYear, CurrentMonth, 1);
            var daysInMonth = DateTime.DaysInMonth(CurrentYear, CurrentMonth);
            var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            var days = new List<CalendarDay>();

            // Add empty days for previous month
            for (var i = 0; i < startDayOfWeek; i++)
            {
                days.Add(new CalendarDay { IsCurrentMonth = false });
            }

            // Add days of current month
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(CurrentYear, CurrentMonth, day);
                days.Add(new CalendarDay
                {
                    Date = date,
                    DayNumber = day,
                    IsCurrentMonth = true,
                    IsToday = date == DateTime.Today,
                    HasEntry = entryDates.Contains(date)
                });
            }

            // Fill remaining days to complete the grid (6 rows)
            while (days.Count < 42)
            {
                days.Add(new CalendarDay { IsCurrentMonth = false });
            }

            CalendarDays = days;
        }
        catch (Exception ex)
        {
            SetError($"Failed to load calendar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task PreviousMonthAsync()
    {
        if (CurrentMonth == 1)
        {
            CurrentMonth = 12;
            CurrentYear--;
        }
        else
        {
            CurrentMonth--;
        }

        await LoadCalendarAsync();
    }

    [RelayCommand]
    public async Task NextMonthAsync()
    {
        if (CurrentMonth == 12)
        {
            CurrentMonth = 1;
            CurrentYear++;
        }
        else
        {
            CurrentMonth++;
        }

        await LoadCalendarAsync();
    }

    [RelayCommand]
    public async Task GoToTodayAsync()
    {
        var today = DateTime.Today;
        CurrentYear = today.Year;
        CurrentMonth = today.Month;
        await LoadCalendarAsync();
    }

    [RelayCommand]
    public async Task SelectDateAsync(DateTime date)
    {
        SelectedDate = date;
        SelectedEntry = await _journalService.GetEntryByDateAsync(date);
    }
}

/// <summary>
/// Represents a day in the calendar grid
/// </summary>
public class CalendarDay
{
    public DateTime? Date { get; set; }
    public int DayNumber { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public bool HasEntry { get; set; }
}
