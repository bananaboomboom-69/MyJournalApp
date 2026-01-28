using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for PDF export functionality
/// </summary>
public partial class ExportViewModel : BaseViewModel
{
    private readonly IExportService _exportService;
    private readonly IJournalService _journalService;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private List<JournalEntry> _previewEntries = new();

    [ObservableProperty]
    private int _entryCount;

    [ObservableProperty]
    private string? _exportedFilePath;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private bool _exportSuccess;

    public ExportViewModel(IExportService exportService, IJournalService journalService)
    {
        _exportService = exportService;
        _journalService = journalService;
        Title = "Export";
    }

    [RelayCommand]
    public async Task LoadPreviewAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();
            ExportSuccess = false;

            PreviewEntries = await _journalService.GetEntriesByDateRangeAsync(StartDate, EndDate);
            EntryCount = PreviewEntries.Count;
        }
        catch (Exception ex)
        {
            SetError($"Failed to load preview: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportToPdfAsync()
    {
        if (IsExporting) return;

        if (EntryCount == 0)
        {
            SetError("No entries to export in the selected date range");
            return;
        }

        try
        {
            IsExporting = true;
            ClearError();

            var fileName = $"journal_export_{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}";
            ExportedFilePath = await _exportService.ExportToPdfAsync(PreviewEntries, fileName);
            ExportSuccess = true;
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    public async Task OpenExportedFileAsync()
    {
        if (string.IsNullOrEmpty(ExportedFilePath) || !File.Exists(ExportedFilePath))
        {
            SetError("Exported file not found");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(ExportedFilePath)
            });
        }
        catch (Exception ex)
        {
            SetError($"Failed to open file: {ex.Message}");
        }
    }

    public void SetDateRange(DateTime start, DateTime end)
    {
        StartDate = start;
        EndDate = end;
    }

    public void SetLastWeek()
    {
        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddDays(-7);
    }

    public void SetLastMonth()
    {
        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddMonths(-1);
    }

    public void SetLastYear()
    {
        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddYears(-1);
    }
}
