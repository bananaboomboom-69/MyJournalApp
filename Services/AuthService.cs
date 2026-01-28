using System.Security.Cryptography;
using System.Text;
using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthService
{
    Task<bool> IsPinEnabledAsync();
    Task<bool> ValidatePinAsync(string pin);
    Task SetPinAsync(string pin);
    Task RemovePinAsync();
    Task<bool> IsAuthenticatedAsync();
    void SetAuthenticated(bool value);
}

/// <summary>
/// Service for PIN/password authentication
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDatabaseService _databaseService;
    private bool _isAuthenticated = false;

    public AuthService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<bool> IsPinEnabledAsync()
    {
        var db = _databaseService.GetConnection();
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();
        return settings?.IsPinEnabled ?? false;
    }

    public async Task<bool> ValidatePinAsync(string pin)
    {
        var db = _databaseService.GetConnection();
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();

        if (settings == null || !settings.IsPinEnabled || string.IsNullOrEmpty(settings.PinHash) || string.IsNullOrEmpty(settings.PinSalt))
        {
            return true; // No PIN set
        }

        var hash = HashPin(pin, settings.PinSalt);
        var isValid = hash == settings.PinHash;

        if (isValid)
        {
            _isAuthenticated = true;
        }

        return isValid;
    }

    public async Task SetPinAsync(string pin)
    {
        var db = _databaseService.GetConnection();
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new UserSettings();
            await db.InsertAsync(settings);
        }

        var salt = GenerateSalt();
        var hash = HashPin(pin, salt);

        settings.IsPinEnabled = true;
        settings.PinHash = hash;
        settings.PinSalt = salt;

        await db.UpdateAsync(settings);
        _isAuthenticated = true;
    }

    public async Task RemovePinAsync()
    {
        var db = _databaseService.GetConnection();
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();

        if (settings != null)
        {
            settings.IsPinEnabled = false;
            settings.PinHash = null;
            settings.PinSalt = null;
            await db.UpdateAsync(settings);
        }

        _isAuthenticated = true;
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }

    public void SetAuthenticated(bool value)
    {
        _isAuthenticated = value;
    }

    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPin(string pin, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var pinBytes = Encoding.UTF8.GetBytes(pin);
        var combined = new byte[saltBytes.Length + pinBytes.Length];

        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(pinBytes, 0, combined, saltBytes.Length, pinBytes.Length);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hashBytes);
    }
}
