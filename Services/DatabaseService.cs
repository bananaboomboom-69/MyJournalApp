using SQLite;
using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Interface for database operations
/// </summary>
public interface IDatabaseService
{
    Task InitializeAsync();
    SQLiteAsyncConnection GetConnection();
    Task<List<T>> GetAllAsync<T>() where T : new();
    Task<T?> GetByIdAsync<T>(int id) where T : new();
    Task<int> SaveAsync<T>(T item) where T : new();
    Task<int> DeleteAsync<T>(T item) where T : new();
}

/// <summary>
/// SQLite database service for managing local data storage
/// </summary>
public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private bool _initialized = false;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "myjournal.db3");
    }

    public SQLiteAsyncConnection GetConnection()
    {
        if (_database == null)
        {
            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        }
        return _database;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var db = GetConnection();

        // Create tables
        await db.CreateTableAsync<JournalEntry>();
        await db.CreateTableAsync<Tag>();
        await db.CreateTableAsync<EntryTag>();
        await db.CreateTableAsync<UserSettings>();
        await db.CreateTableAsync<StreakInfo>();

        // Initialize default settings if not exists
        var settings = await db.Table<UserSettings>().FirstOrDefaultAsync();
        if (settings == null)
        {
            await db.InsertAsync(new UserSettings());
        }

        // Initialize streak info if not exists
        var streakInfo = await db.Table<StreakInfo>().FirstOrDefaultAsync();
        if (streakInfo == null)
        {
            await db.InsertAsync(new StreakInfo());
        }

        // Initialize pre-built tags if not exists
        var existingTags = await db.Table<Tag>().Where(t => t.IsPreBuilt).CountAsync();
        if (existingTags == 0)
        {
            var preBuiltTags = Tag.GetPreBuiltTags();
            await db.InsertAllAsync(preBuiltTags);
        }

        _initialized = true;
    }

    public async Task<List<T>> GetAllAsync<T>() where T : new()
    {
        var db = GetConnection();
        return await db.Table<T>().ToListAsync();
    }

    public async Task<T?> GetByIdAsync<T>(int id) where T : new()
    {
        var db = GetConnection();
        return await db.FindAsync<T>(id);
    }

    public async Task<int> SaveAsync<T>(T item) where T : new()
    {
        var db = GetConnection();
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var idValue = (int)(idProperty.GetValue(item) ?? 0);
            if (idValue != 0)
            {
                return await db.UpdateAsync(item);
            }
        }
        return await db.InsertAsync(item);
    }

    public async Task<int> DeleteAsync<T>(T item) where T : new()
    {
        var db = GetConnection();
        return await db.DeleteAsync(item);
    }
}
