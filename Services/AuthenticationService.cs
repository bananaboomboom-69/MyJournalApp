using BCrypt.Net;
using MyJournalApp.Models;

namespace MyJournalApp.Services
{
    /// <summary>
    /// Service for handling user authentication with secure password hashing.
    /// </summary>
    public class AuthenticationService
    {
        private static AuthenticationService? _instance;
        private readonly DatabaseService _database;

        /// <summary>
        /// Singleton instance of the authentication service.
        /// </summary>
        public static AuthenticationService Instance => _instance ??= new AuthenticationService();

        /// <summary>
        /// The currently logged-in user.
        /// </summary>
        public User? CurrentUser { get; private set; }

        private AuthenticationService()
        {
            _database = DatabaseService.Instance;
        }

        /// <summary>
        /// Checks if a user account has been set up.
        /// </summary>
        public bool IsAccountSetUp()
        {
            return _database.UserExists();
        }

        /// <summary>
        /// Creates a new user account with a hashed password.
        /// </summary>
        /// <param name="username">Display name for the user.</param>
        /// <param name="password">Plain text password or PIN.</param>
        /// <param name="usePin">True if using PIN-based authentication.</param>
        /// <returns>True if account creation was successful.</returns>
        public bool CreateAccount(string username, string password, bool usePin)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Hash the password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

            var user = new User
            {
                Username = string.IsNullOrWhiteSpace(username) ? "User" : username,
                PasswordHash = passwordHash,
                UsePin = usePin
            };

            _database.CreateUser(user);
            CurrentUser = user;
            return true;
        }

        /// <summary>
        /// Validates user credentials and logs in.
        /// </summary>
        /// <param name="password">Plain text password or PIN to verify.</param>
        /// <returns>True if authentication was successful.</returns>
        public bool Login(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            var user = _database.GetUser();
            if (user == null)
                return false;

            // Verify the password using BCrypt
            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (isValid)
            {
                CurrentUser = user;
            }

            return isValid;
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Gets the user preference for PIN vs Password authentication.
        /// </summary>
        public bool GetUserPrefersPin()
        {
            var user = _database.GetUser();
            return user?.UsePin ?? false;
        }

        /// <summary>
        /// Verifies the provided password against the current user's stored hash.
        /// </summary>
        /// <param name="password">Plain text password to verify.</param>
        /// <returns>True if password is correct.</returns>
        public bool VerifyPassword(string password)
        {
            if (CurrentUser == null || string.IsNullOrWhiteSpace(password))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, CurrentUser.PasswordHash);
        }

        /// <summary>
        /// Updates the current user's password.
        /// </summary>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>True if update was successful.</returns>
        public bool UpdatePassword(string newPassword)
        {
            if (CurrentUser == null || string.IsNullOrWhiteSpace(newPassword))
                return false;

            // Hash the new password
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

            // Update in database
            CurrentUser.PasswordHash = newPasswordHash;
            return _database.UpdateUserPassword(CurrentUser.Id, newPasswordHash);
        }
    }
}
