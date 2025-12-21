using System;

namespace MyJournalApp.Services
{
    /// <summary>
    /// Service for managing navigation between views.
    /// </summary>
    public class NavigationService
    {
        private static NavigationService? _instance;

        /// <summary>
        /// Singleton instance of the navigation service.
        /// </summary>
        public static NavigationService Instance => _instance ??= new NavigationService();

        /// <summary>
        /// Event fired when navigation is requested.
        /// </summary>
        public event Action<string>? NavigationRequested;

        private NavigationService() { }

        /// <summary>
        /// Navigate to the specified view.
        /// </summary>
        /// <param name="viewName">Name of the view to navigate to.</param>
        public void NavigateTo(string viewName)
        {
            NavigationRequested?.Invoke(viewName);
        }
    }
}
