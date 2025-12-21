using System.Windows.Input;
using MyJournalApp.Services;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Login screen.
    /// Handles authentication, account setup, and Password/PIN toggle.
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;
        private readonly NavigationService _navigationService;

        private string _password = string.Empty;
        private string _username = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private bool _isPasswordMode = true;
        private bool _isPinMode;
        private bool _isNewUser;
        private bool _showPassword;

        #region Properties

        /// <summary>
        /// The password or PIN entered by the user.
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    HasError = false;
                    ErrorMessage = string.Empty;
                }
            }
        }

        /// <summary>
        /// Username for new account setup.
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// Error message to display.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Whether there is an error to display.
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// Whether password authentication mode is selected.
        /// </summary>
        public bool IsPasswordMode
        {
            get => _isPasswordMode;
            set
            {
                if (SetProperty(ref _isPasswordMode, value))
                {
                    if (value) IsPinMode = false;
                }
            }
        }

        /// <summary>
        /// Whether PIN authentication mode is selected.
        /// </summary>
        public bool IsPinMode
        {
            get => _isPinMode;
            set
            {
                if (SetProperty(ref _isPinMode, value))
                {
                    if (value) IsPasswordMode = false;
                }
            }
        }

        /// <summary>
        /// Whether this is a new user (first-time setup).
        /// </summary>
        public bool IsNewUser
        {
            get => _isNewUser;
            set => SetProperty(ref _isNewUser, value);
        }

        /// <summary>
        /// Whether to show the password in plain text.
        /// </summary>
        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        /// <summary>
        /// Placeholder text based on current mode.
        /// </summary>
        public string PasswordPlaceholder => IsPinMode ? "Enter your PIN" : "Enter your secure password";

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand CreateAccountCommand { get; }
        public ICommand TogglePasswordModeCommand { get; }
        public ICommand TogglePinModeCommand { get; }
        public ICommand ToggleShowPasswordCommand { get; }

        #endregion

        public LoginViewModel()
        {
            _authService = AuthenticationService.Instance;
            _navigationService = NavigationService.Instance;

            // Check if user exists
            IsNewUser = !_authService.IsAccountSetUp();

            // Set default mode based on user preference
            if (!IsNewUser)
            {
                IsPinMode = _authService.GetUserPrefersPin();
                IsPasswordMode = !IsPinMode;
            }

            // Initialize commands
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            CreateAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
            TogglePasswordModeCommand = new RelayCommand(() => { IsPasswordMode = true; IsPinMode = false; OnPropertyChanged(nameof(PasswordPlaceholder)); });
            TogglePinModeCommand = new RelayCommand(() => { IsPinMode = true; IsPasswordMode = false; OnPropertyChanged(nameof(PasswordPlaceholder)); });
            ToggleShowPasswordCommand = new RelayCommand(() => ShowPassword = !ShowPassword);
        }

        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(Password) && !IsNewUser;
        }

        private void ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = IsPinMode ? "Please enter your PIN" : "Please enter your password";
                HasError = true;
                return;
            }

            bool success = _authService.Login(Password);

            if (success)
            {
                // Navigate to Dashboard
                _navigationService.NavigateTo("Dashboard");
            }
            else
            {
                ErrorMessage = IsPinMode ? "Incorrect PIN. Please try again." : "Incorrect password. Please try again.";
                HasError = true;
                Password = string.Empty;
            }
        }

        private bool CanExecuteCreateAccount()
        {
            return IsNewUser && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteCreateAccount()
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = IsPinMode ? "Please enter a PIN" : "Please enter a password";
                HasError = true;
                return;
            }

            // Validate PIN (should be numeric)
            if (IsPinMode && !System.Text.RegularExpressions.Regex.IsMatch(Password, @"^\d{4,8}$"))
            {
                ErrorMessage = "PIN must be 4-8 digits";
                HasError = true;
                return;
            }

            // Validate password (minimum length)
            if (IsPasswordMode && Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters";
                HasError = true;
                return;
            }

            bool success = _authService.CreateAccount(Username, Password, IsPinMode);

            if (success)
            {
                _navigationService.NavigateTo("Dashboard");
            }
            else
            {
                ErrorMessage = "Failed to create account. Please try again.";
                HasError = true;
            }
        }
    }
}
