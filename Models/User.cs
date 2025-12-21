using System;

namespace MyJournalApp.Models
{
    /// <summary>
    /// Represents a user account with authentication credentials.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name of the user.
        /// </summary>
        public string Username { get; set; } = "User";

        /// <summary>
        /// BCrypt hashed password or PIN.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the user prefers PIN-based authentication.
        /// </summary>
        public bool UsePin { get; set; } = false;

        /// <summary>
        /// Timestamp when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
