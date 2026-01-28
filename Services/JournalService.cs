using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Interface for journal entry operations
/// </summary>
public interface IJournalService
{
    Task<List<JournalEntry>> GetAllEntriesAsync();
    Task<JournalEntry?> GetEntryByIdAsync(int id);
    Task<JournalEntry?> GetEntryByDateAsync(DateTime date);
    Task<List<JournalEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm);
    Task<List<JournalEntry>> FilterEntriesAsync(MoodType? mood = null, int? tagId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<(List<JournalEntry> entries, int totalCount)> GetPaginatedEntriesAsync(int page, int pageSize, string? searchTerm = null);
    Task<JournalEntry> SaveEntryAsync(JournalEntry entry);
    Task DeleteEntryAsync(int id);
    Task<List<DateTime>> GetDatesWithEntriesAsync(int year, int month);
    Task<int> GetTotalEntriesCountAsync();
}

/// <summary>
/// Service for managing journal entries
/// </summary>
public class JournalService : IJournalService
{
    private readonly IDatabaseService _databaseService;
    private readonly IStreakService _streakService;

    public JournalService(IDatabaseService databaseService, IStreakService streakService)
    {
        _databaseService = databaseService;
        _streakService = streakService;
    }

    public async Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        var db = _databaseService.GetConnection();
        var entries = await db.Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

        // Load tags for each entry
        foreach (var entry in entries)
        {
            await LoadTagsForEntryAsync(entry);
        }

        return entries;
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(int id)
    {
        var entry = await _databaseService.GetByIdAsync<JournalEntry>(id);
        if (entry != null)
        {
            await LoadTagsForEntryAsync(entry);
        }
        return entry;
    }

    public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
    {
        var db = _databaseService.GetConnection();
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        var entry = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= startOfDay && e.EntryDate <= endOfDay)
            .FirstOrDefaultAsync();

        if (entry != null)
        {
            await LoadTagsForEntryAsync(entry);
        }

        return entry;
    }

    public async Task<List<JournalEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var db = _databaseService.GetConnection();
        var entries = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= startDate.Date && e.EntryDate <= endDate.Date)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

        foreach (var entry in entries)
        {
            await LoadTagsForEntryAsync(entry);
        }

        return entries;
    }

    public async Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllEntriesAsync();

        var db = _databaseService.GetConnection();
        var term = searchTerm.ToLower();

        var entries = await db.Table<JournalEntry>()
            .ToListAsync();

        var filtered = entries
            .Where(e => e.Title.ToLower().Contains(term) || e.Content.ToLower().Contains(term))
            .OrderByDescending(e => e.EntryDate)
            .ToList();

        foreach (var entry in filtered)
        {
            await LoadTagsForEntryAsync(entry);
        }

        return filtered;
    }

    public async Task<List<JournalEntry>> FilterEntriesAsync(MoodType? mood = null, int? tagId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var db = _databaseService.GetConnection();
        var query = db.Table<JournalEntry>();

        var entries = await query.ToListAsync();

        if (mood.HasValue)
        {
            entries = entries.Where(e =>
                e.PrimaryMood == mood.Value ||
                e.SecondaryMood1 == mood.Value ||
                e.SecondaryMood2 == mood.Value).ToList();
        }

        if (startDate.HasValue)
        {
            entries = entries.Where(e => e.EntryDate.Date >= startDate.Value.Date).ToList();
        }

        if (endDate.HasValue)
        {
            entries = entries.Where(e => e.EntryDate.Date <= endDate.Value.Date).ToList();
        }

        if (tagId.HasValue)
        {
            var entryTags = await db.Table<EntryTag>()
                .Where(et => et.TagId == tagId.Value)
                .ToListAsync();
            var entryIds = entryTags.Select(et => et.EntryId).ToHashSet();
            entries = entries.Where(e => entryIds.Contains(e.Id)).ToList();
        }

        entries = entries.OrderByDescending(e => e.EntryDate).ToList();

        foreach (var entry in entries)
        {
            await LoadTagsForEntryAsync(entry);
        }

        return entries;
    }

    public async Task<(List<JournalEntry> entries, int totalCount)> GetPaginatedEntriesAsync(int page, int pageSize, string? searchTerm = null)
    {
        List<JournalEntry> allEntries;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            allEntries = await SearchEntriesAsync(searchTerm);
        }
        else
        {
            allEntries = await GetAllEntriesAsync();
        }

        var totalCount = allEntries.Count;
        var paginatedEntries = allEntries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (paginatedEntries, totalCount);
    }

    public async Task<JournalEntry> SaveEntryAsync(JournalEntry entry)
    {
        var db = _databaseService.GetConnection();

        // Check for existing entry on the same date
        var existingEntry = await GetEntryByDateAsync(entry.EntryDate);

        if (existingEntry != null && existingEntry.Id != entry.Id)
        {
            // Update existing entry instead of creating new one
            entry.Id = existingEntry.Id;
            entry.CreatedAt = existingEntry.CreatedAt;
        }

        entry.UpdateWordCount();
        entry.ModifiedAt = DateTime.Now;

        if (entry.Id == 0)
        {
            entry.CreatedAt = DateTime.Now;
            await db.InsertAsync(entry);
        }
        else
        {
            await db.UpdateAsync(entry);
        }

        // Handle tags
        await db.ExecuteAsync("DELETE FROM EntryTag WHERE EntryId = ?", entry.Id);

        foreach (var tag in entry.Tags)
        {
            await db.InsertAsync(new EntryTag { EntryId = entry.Id, TagId = tag.Id });
        }

        // Update streak
        await _streakService.UpdateStreakAsync();

        return entry;
    }

    public async Task DeleteEntryAsync(int id)
    {
        var db = _databaseService.GetConnection();

        // Delete entry tags first
        await db.ExecuteAsync("DELETE FROM EntryTag WHERE EntryId = ?", id);

        // Delete entry
        var entry = await GetEntryByIdAsync(id);
        if (entry != null)
        {
            await db.DeleteAsync(entry);
        }

        // Update streak
        await _streakService.UpdateStreakAsync();
    }

    public async Task<List<DateTime>> GetDatesWithEntriesAsync(int year, int month)
    {
        var db = _databaseService.GetConnection();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var entries = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate)
            .ToListAsync();

        return entries.Select(e => e.EntryDate.Date).Distinct().ToList();
    }

    public async Task<int> GetTotalEntriesCountAsync()
    {
        var db = _databaseService.GetConnection();
        return await db.Table<JournalEntry>().CountAsync();
    }

    private async Task LoadTagsForEntryAsync(JournalEntry entry)
    {
        var db = _databaseService.GetConnection();

        var entryTags = await db.Table<EntryTag>()
            .Where(et => et.EntryId == entry.Id)
            .ToListAsync();

        entry.Tags = new List<Tag>();

        foreach (var entryTag in entryTags)
        {
            var tag = await db.FindAsync<Tag>(entryTag.TagId);
            if (tag != null)
            {
                entry.Tags.Add(tag);
            }
        }
    }
}
