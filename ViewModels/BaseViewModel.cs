using CommunityToolkit.Mvvm.ComponentModel;

namespace myjournal.ViewModels;

/// <summary>
/// Base ViewModel with INotifyPropertyChanged support via CommunityToolkit.Mvvm
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    public void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }
}
