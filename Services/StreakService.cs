using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Interface for streak tracking operations
/// </summary>
public interface IStreakService
{
    Task<StreakInfo> GetStreakInfoAsync();
    Task UpdateStreakAsync();
    Task<List<DateTime>> GetMissedDaysAsync(int lastNDays = 30);
}

/// <summary>
/// Service for tracking journaling streaks
/// </summary>
public class StreakService : IStreakService
{
    private readonly IDatabaseService _databaseService;

    public StreakService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<StreakInfo> GetStreakInfoAsync()
    {
        var db = _databaseService.GetConnection();
        var streakInfo = await db.Table<StreakInfo>().FirstOrDefaultAsync();

        if (streakInfo == null)
        {
            streakInfo = new StreakInfo();
            await db.InsertAsync(streakInfo);
        }

        return streakInfo;
    }

    public async Task UpdateStreakAsync()
    {
        var db = _databaseService.GetConnection();

        // Get all entry dates
        var entries = await db.Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

        var streakInfo = await GetStreakInfoAsync();

        if (entries.Count == 0)
        {
            streakInfo.CurrentStreak = 0;
            streakInfo.TotalEntries = 0;
            streakInfo.TotalDaysWithEntries = 0;
            streakInfo.LastEntryDate = null;
            streakInfo.StreakStartDate = null;
            await db.UpdateAsync(streakInfo);
            return;
        }

        // Get unique dates with entries
        var datesWithEntries = entries
            .Select(e => e.EntryDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        streakInfo.TotalEntries = entries.Count;
        streakInfo.TotalDaysWithEntries = datesWithEntries.Count;
        streakInfo.LastEntryDate = datesWithEntries.First();

        // Calculate current streak
        var today = DateTime.Today;
        var currentStreak = 0;
        var checkDate = today;

        // If no entry today, start checking from yesterday
        if (!datesWithEntries.Contains(today))
        {
            checkDate = today.AddDays(-1);
        }

        while (datesWithEntries.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        streakInfo.CurrentStreak = currentStreak;

        // Find streak start date
        if (currentStreak > 0)
        {
            streakInfo.StreakStartDate = datesWithEntries.Contains(today)
                ? today.AddDays(-(currentStreak - 1))
                : today.AddDays(-currentStreak);
        }
        else
        {
            streakInfo.StreakStartDate = null;
        }

        // Update longest streak if current is higher
        if (streakInfo.CurrentStreak > streakInfo.LongestStreak)
        {
            streakInfo.LongestStreak = streakInfo.CurrentStreak;
        }

        // Calculate missed days (in last 30 days)
        var missedDays = 0;
        var firstEntryDate = datesWithEntries.Last();
        var startCheckDate = firstEntryDate > today.AddDays(-30) ? firstEntryDate : today.AddDays(-30);

        for (var date = startCheckDate; date <= today; date = date.AddDays(1))
        {
            if (!datesWithEntries.Contains(date))
            {
                missedDays++;
            }
        }
        streakInfo.MissedDays = missedDays;

        await db.UpdateAsync(streakInfo);
    }

    public async Task<List<DateTime>> GetMissedDaysAsync(int lastNDays = 30)
    {
        var db = _databaseService.GetConnection();

        var entries = await db.Table<JournalEntry>().ToListAsync();
        var datesWithEntries = entries
            .Select(e => e.EntryDate.Date)
            .Distinct()
            .ToHashSet();

        var missedDays = new List<DateTime>();
        var today = DateTime.Today;

        for (var i = 0; i < lastNDays; i++)
        {
            var date = today.AddDays(-i);
            if (!datesWithEntries.Contains(date))
            {
                missedDays.Add(date);
            }
        }

        return missedDays;
    }
}
