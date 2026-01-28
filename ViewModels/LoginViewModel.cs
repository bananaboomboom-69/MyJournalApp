using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for login screen
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private bool _isPinRequired;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Login";
    }

    [RelayCommand]
    public async Task CheckPinRequiredAsync()
    {
        IsPinRequired = await _authService.IsPinEnabledAsync();

        if (!IsPinRequired)
        {
            _authService.SetAuthenticated(true);
            IsAuthenticated = true;
        }
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Pin))
        {
            SetError("Please enter your PIN");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var isValid = await _authService.ValidatePinAsync(Pin);

            if (isValid)
            {
                IsAuthenticated = true;
            }
            else
            {
                SetError("Invalid PIN. Please try again.");
                Pin = string.Empty;
            }
        }
        catch (Exception ex)
        {
            SetError($"Login failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
