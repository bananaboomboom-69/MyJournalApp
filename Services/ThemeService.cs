using System;
using System.Windows;
using System.Windows.Media;

namespace MyJournalApp.Services
{
    /// <summary>
    /// Service for managing application theme (Dark/Light mode).
    /// </summary>
    public class ThemeService
    {
        private static ThemeService? _instance;
        private bool _isDarkMode = true;

        /// <summary>
        /// Singleton instance of the theme service.
        /// </summary>
        public static ThemeService Instance => _instance ??= new ThemeService();

        /// <summary>
        /// Event fired when theme changes.
        /// </summary>
        public event Action<bool>? ThemeChanged;

        /// <summary>
        /// Whether dark mode is currently active.
        /// </summary>
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    ApplyTheme();
                    ThemeChanged?.Invoke(_isDarkMode);
                }
            }
        }

        private ThemeService()
        {
            // Default to dark mode
            _isDarkMode = true;
        }

        /// <summary>
        /// Applies the current theme to the application.
        /// </summary>
        public void ApplyTheme()
        {
            var resources = Application.Current.Resources;

            if (_isDarkMode)
            {
                // Dark Theme Colors
                resources["BackgroundPrimary"] = ColorFromHex("#0D1117");
                resources["BackgroundSecondary"] = ColorFromHex("#161B22");
                resources["BackgroundTertiary"] = ColorFromHex("#21262D");
                resources["BorderColor"] = ColorFromHex("#30363D");
                resources["TextPrimary"] = ColorFromHex("#C9D1D9");
                resources["TextSecondary"] = ColorFromHex("#8B949E");
                resources["TextMuted"] = ColorFromHex("#484F58");
                resources["TextWhite"] = ColorFromHex("#FFFFFF");

                // Update brushes
                resources["BackgroundPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#0D1117"));
                resources["BackgroundSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#161B22"));
                resources["BackgroundTertiaryBrush"] = new SolidColorBrush(ColorFromHex("#21262D"));
                resources["BorderBrush"] = new SolidColorBrush(ColorFromHex("#30363D"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#C9D1D9"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#8B949E"));
            }
            else
            {
                // Light Theme Colors
                resources["BackgroundPrimary"] = ColorFromHex("#FFFFFF");
                resources["BackgroundSecondary"] = ColorFromHex("#F6F8FA");
                resources["BackgroundTertiary"] = ColorFromHex("#EAEEF2");
                resources["BorderColor"] = ColorFromHex("#D0D7DE");
                resources["TextPrimary"] = ColorFromHex("#1F2328");
                resources["TextSecondary"] = ColorFromHex("#656D76");
                resources["TextMuted"] = ColorFromHex("#8C959F");
                resources["TextWhite"] = ColorFromHex("#FFFFFF");

                // Update brushes
                resources["BackgroundPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#FFFFFF"));
                resources["BackgroundSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#F6F8FA"));
                resources["BackgroundTertiaryBrush"] = new SolidColorBrush(ColorFromHex("#EAEEF2"));
                resources["BorderBrush"] = new SolidColorBrush(ColorFromHex("#D0D7DE"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#1F2328"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#656D76"));
            }
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
