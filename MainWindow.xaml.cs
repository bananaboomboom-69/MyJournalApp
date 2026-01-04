using System.Windows;
using MyJournalApp.Services;
using MyJournalApp.Views;

namespace MyJournalApp
{
    /// <summary>
    /// Main window that hosts the application views and handles navigation.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to navigation events
            NavigationService.Instance.NavigationRequested += OnNavigationRequested;

            // Start with Login view
            NavigateToLogin();
        }

        /// <summary>
        /// Handles navigation requests from the NavigationService.
        /// </summary>
        private void OnNavigationRequested(string viewName)
        {
            switch (viewName)
            {
                case "Login":
                    NavigateToLogin();
                    break;
                case "Dashboard":
                    NavigateToDashboard();
                    break;
                case "JournalEntry":
                    NavigateToJournalEntry();
                    break;
                case "Calendar":
                    NavigateToCalendar();
                    break;
                case "Timeline":
                    NavigateToTimeline();
                    break;
                case "Analytics":
                    NavigateToAnalytics();
                    break;
                case "Settings":
                    NavigateToSettings();
                    break;
            }
        }

        /// <summary>
        /// Navigates to the Login view.
        /// </summary>
        private void NavigateToLogin()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new LoginView());
        }

        /// <summary>
        /// Navigates to the Dashboard view.
        /// </summary>
        private void NavigateToDashboard()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new DashboardView());
        }

        /// <summary>
        /// Navigates to the Journal Entry view.
        /// </summary>
        private void NavigateToJournalEntry()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new JournalEntryView());
        }

        /// <summary>
        /// Navigates to the Calendar view.
        /// </summary>
        private void NavigateToCalendar()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new CalendarView());
        }

        /// <summary>
        /// Navigates to the Timeline view.
        /// </summary>
        private void NavigateToTimeline()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new TimelineView());
        }

        /// <summary>
        /// Navigates to the Analytics view.
        /// </summary>
        private void NavigateToAnalytics()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new AnalyticsView());
        }

        /// <summary>
        /// Navigates to the Settings view.
        /// </summary>
        private void NavigateToSettings()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new SettingsView());
        }
    }
}