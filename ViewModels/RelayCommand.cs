using System;
using System.Windows.Input;

namespace MyJournalApp.ViewModels
{
    /// <summary>
    /// A reusable command implementation for MVVM pattern.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        /// <summary>
        /// Creates a new RelayCommand.
        /// </summary>
        /// <param name="execute">Action to execute.</param>
        /// <param name="canExecute">Optional predicate to determine if command can execute.</param>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Creates a new RelayCommand with a parameterless action.
        /// </summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
        {
        }

        /// <summary>
        /// Event raised when CanExecute may have changed.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Determines if the command can execute.
        /// </summary>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Executes the command.
        /// </summary>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Raises the CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
