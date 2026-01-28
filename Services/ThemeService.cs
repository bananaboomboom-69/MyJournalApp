using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Interface for theme operations
/// </summary>
public interface IThemeService
{
    Task<string> GetCurrentThemeAsync();
    Task SetThemeAsync(string theme);
    event EventHandler<string>? ThemeChanged;
}

/// <summary>
/// Service for theme management
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IDatabaseService _databaseService;
    private string _currentTheme = "dark";

    public event EventHandler<string>? ThemeChanged;

    public ThemeService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<string> GetCurrentThemeAsync()
    {
        var db = _databaseService.GetConnection();
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();
        _currentTheme = settings?.Theme ?? "dark";
        return _currentTheme;
    }

    public async Task SetThemeAsync(string theme)
    {
        var db = _databaseService.GetConnection();
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new UserSettings { Theme = theme };
            await db.InsertAsync(settings);
        }
        else
        {
            settings.Theme = theme;
            await db.UpdateAsync(settings);
        }

        _currentTheme = theme;
        ThemeChanged?.Invoke(this, theme);
    }
}
