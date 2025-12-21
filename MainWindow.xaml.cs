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
    }
}