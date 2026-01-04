using System;
using System.Windows.Input;
using MyJournalApp.Services;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings & Preferences screen.
    /// Handles data export, appearance settings, and security options.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;

        private DateTime _exportStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _exportEndDate = DateTime.Now;
        private bool _isDarkMode = true;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _hasStatusMessage;
        private string _selectedNavItem = "Settings";
        private string _userName = "Jane Doe";

        #region Properties

        /// <summary>
        /// Start date for export range.
        /// </summary>
        public DateTime ExportStartDate
        {
            get => _exportStartDate;
            set => SetProperty(ref _exportStartDate, value);
        }

        /// <summary>
        /// End date for export range.
        /// </summary>
        public DateTime ExportEndDate
        {
            get => _exportEndDate;
            set => SetProperty(ref _exportEndDate, value);
        }

        /// <summary>
        /// Formatted start date for display.
        /// </summary>
        public string ExportStartDateFormatted => ExportStartDate.ToString("MM/dd/yyyy");

        /// <summary>
        /// Formatted end date for display.
        /// </summary>
        public string ExportEndDateFormatted => ExportEndDate.ToString("MM/dd/yyyy");

        /// <summary>
        /// Whether dark mode is enabled.
        /// </summary>
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    ApplyTheme();
                }
            }
        }

        /// <summary>
        /// Current password for verification.
        /// </summary>
        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }

        /// <summary>
        /// New password to set.
        /// </summary>
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        /// <summary>
        /// Confirmation of new password.
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        /// <summary>
        /// Status message for user feedback.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                HasStatusMessage = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Whether there's a status message to display.
        /// </summary>
        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            set => SetProperty(ref _hasStatusMessage, value);
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

        #endregion

        #region Commands

        public ICommand ExportPdfCommand { get; }
        public ICommand ToggleDarkModeCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ViewProfileCommand { get; }

        #endregion

        public SettingsViewModel()
        {
            _authService = AuthenticationService.Instance;

            // Sync with current theme state
            _isDarkMode = ThemeService.Instance.IsDarkMode;

            // Set user info
            if (_authService.CurrentUser != null)
            {
                UserName = _authService.CurrentUser.Username;
            }

            // Initialize commands
            ExportPdfCommand = new RelayCommand(ExecuteExportPdf);
            ToggleDarkModeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanChangePassword);
            NavigateCommand = new RelayCommand(ExecuteNavigate);
            ViewProfileCommand = new RelayCommand(ExecuteViewProfile);
        }

        private void ExecuteExportPdf()
        {
            // Export functionality
            System.Windows.MessageBox.Show(
                $"Exporting entries from {ExportStartDateFormatted} to {ExportEndDateFormatted}.\n\nThis feature will be available in a future update.",
                "Export PDF",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   NewPassword == ConfirmPassword &&
                   NewPassword.Length >= 6;
        }

        private void ExecuteChangePassword()
        {
            // Verify current password
            if (!_authService.VerifyPassword(CurrentPassword))
            {
                StatusMessage = "Current password is incorrect.";
                return;
            }

            // Check new password requirements
            if (NewPassword != ConfirmPassword)
            {
                StatusMessage = "New passwords do not match.";
                return;
            }

            if (NewPassword.Length < 6)
            {
                StatusMessage = "New password must be at least 6 characters.";
                return;
            }

            // Update password
            if (_authService.UpdatePassword(NewPassword))
            {
                StatusMessage = "Password updated successfully!";
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            else
            {
                StatusMessage = "Failed to update password. Please try again.";
            }
        }

        private void ApplyTheme()
        {
            // Apply theme using the ThemeService
            ThemeService.Instance.IsDarkMode = IsDarkMode;

            // Refresh the current view by navigating back to Settings
            NavigationService.Instance.NavigateTo("Settings");
        }

        private void ExecuteNavigate(object? param)
        {
            if (param is string navItem)
            {
                SelectedNavItem = navItem;
                switch (navItem)
                {
                    case "Entries":
                        NavigationService.Instance.NavigateTo("Timeline");
                        break;
                    case "Analytics":
                        NavigationService.Instance.NavigateTo("Analytics");
                        break;
                    case "Calendar":
                        NavigationService.Instance.NavigateTo("Calendar");
                        break;
                    case "Dashboard":
                        NavigationService.Instance.NavigateTo("Dashboard");
                        break;
                }
            }
        }

        private void ExecuteViewProfile()
        {
            System.Windows.MessageBox.Show(
                $"Profile: {UserName}\n\nProfile management coming soon!",
                "View Profile",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
}
