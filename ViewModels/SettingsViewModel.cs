using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Services;

namespace myjournal.ViewModels;

/// <summary>
/// ViewModel for settings page
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IThemeService _themeService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _currentTheme = "dark";

    [ObservableProperty]
    private bool _isPinEnabled;

    [ObservableProperty]
    private string _newPin = string.Empty;

    [ObservableProperty]
    private string _confirmPin = string.Empty;

    [ObservableProperty]
    private bool _isChangingPin;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public SettingsViewModel(IThemeService themeService, IAuthService authService)
    {
        _themeService = themeService;
        _authService = authService;
        Title = "Settings";
    }

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            CurrentTheme = await _themeService.GetCurrentThemeAsync();
            IsPinEnabled = await _authService.IsPinEnabledAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleThemeAsync()
    {
        try
        {
            ClearError();
            var newTheme = CurrentTheme == "dark" ? "light" : "dark";
            await _themeService.SetThemeAsync(newTheme);
            CurrentTheme = newTheme;
        }
        catch (Exception ex)
        {
            SetError($"Failed to change theme: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SetThemeAsync(string theme)
    {
        try
        {
            ClearError();
            await _themeService.SetThemeAsync(theme);
            CurrentTheme = theme;
        }
        catch (Exception ex)
        {
            SetError($"Failed to set theme: {ex.Message}");
        }
    }

    [RelayCommand]
    public void StartChangingPin()
    {
        IsChangingPin = true;
        NewPin = string.Empty;
        ConfirmPin = string.Empty;
        ClearError();
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    public void CancelChangingPin()
    {
        IsChangingPin = false;
        NewPin = string.Empty;
        ConfirmPin = string.Empty;
        ClearError();
    }

    [RelayCommand]
    public async Task SavePinAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPin) || NewPin.Length < 4)
        {
            SetError("PIN must be at least 4 characters");
            return;
        }

        if (NewPin != ConfirmPin)
        {
            SetError("PINs do not match");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            await _authService.SetPinAsync(NewPin);
            IsPinEnabled = true;
            IsChangingPin = false;
            NewPin = string.Empty;
            ConfirmPin = string.Empty;
            SuccessMessage = "PIN set successfully!";
        }
        catch (Exception ex)
        {
            SetError($"Failed to set PIN: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RemovePinAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            await _authService.RemovePinAsync();
            IsPinEnabled = false;
            SuccessMessage = "PIN removed successfully!";
        }
        catch (Exception ex)
        {
            SetError($"Failed to remove PIN: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
