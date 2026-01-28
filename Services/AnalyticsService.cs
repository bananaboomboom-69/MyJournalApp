using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Analytics data transfer objects
/// </summary>
public class MoodDistribution
{
    public MoodType Mood { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TagUsage
{
    public Tag Tag { get; set; } = new();
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class WordCountTrend
{
    public DateTime Date { get; set; }
    public int WordCount { get; set; }
}

public class MonthlyStats
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int EntryCount { get; set; }
    public int TotalWords { get; set; }
    public double AverageWords { get; set; }
}

/// <summary>
/// Interface for analytics operations
/// </summary>
public interface IAnalyticsService
{
    Task<List<MoodDistribution>> GetMoodDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<TagUsage>> GetTagUsageAsync(int topN = 10);
    Task<List<WordCountTrend>> GetWordCountTrendAsync(int lastNDays = 30);
    Task<MoodType?> GetMostFrequentMoodAsync();
    Task<int> GetTotalWordCountAsync();
    Task<double> GetAverageWordCountAsync();
    Task<List<MonthlyStats>> GetMonthlyStatsAsync(int lastNMonths = 6);
}

/// <summary>
/// Service for analytics and insights
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IDatabaseService _databaseService;

    public AnalyticsService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<MoodDistribution>> GetMoodDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var db = _databaseService.GetConnection();
        var entries = await db.Table<JournalEntry>().ToListAsync();

        if (startDate.HasValue)
            entries = entries.Where(e => e.EntryDate.Date >= startDate.Value.Date).ToList();
        if (endDate.HasValue)
            entries = entries.Where(e => e.EntryDate.Date <= endDate.Value.Date).ToList();

        if (entries.Count == 0)
            return new List<MoodDistribution>();

        // Count all moods (primary and secondary)
        var moodCounts = new Dictionary<MoodType, int>();

        foreach (var entry in entries)
        {
            if (!moodCounts.ContainsKey(entry.PrimaryMood))
                moodCounts[entry.PrimaryMood] = 0;
            moodCounts[entry.PrimaryMood]++;

            if (entry.SecondaryMood1.HasValue)
            {
                if (!moodCounts.ContainsKey(entry.SecondaryMood1.Value))
                    moodCounts[entry.SecondaryMood1.Value] = 0;
                moodCounts[entry.SecondaryMood1.Value]++;
            }

            if (entry.SecondaryMood2.HasValue)
            {
                if (!moodCounts.ContainsKey(entry.SecondaryMood2.Value))
                    moodCounts[entry.SecondaryMood2.Value] = 0;
                moodCounts[entry.SecondaryMood2.Value]++;
            }
        }

        var total = moodCounts.Values.Sum();

        return moodCounts
            .Select(mc => new MoodDistribution
            {
                Mood = mc.Key,
                Count = mc.Value,
                Percentage = Math.Round((double)mc.Value / total * 100, 1)
            })
            .OrderByDescending(md => md.Count)
            .ToList();
    }

    public async Task<List<TagUsage>> GetTagUsageAsync(int topN = 10)
    {
        var db = _databaseService.GetConnection();

        var entryTags = await db.Table<EntryTag>().ToListAsync();
        var tags = await db.Table<Tag>().ToListAsync();

        if (entryTags.Count == 0)
            return new List<TagUsage>();

        var tagCounts = entryTags
            .GroupBy(et => et.TagId)
            .ToDictionary(g => g.Key, g => g.Count());

        var total = tagCounts.Values.Sum();

        return tags
            .Where(t => tagCounts.ContainsKey(t.Id))
            .Select(t => new TagUsage
            {
                Tag = t,
                Count = tagCounts[t.Id],
                Percentage = Math.Round((double)tagCounts[t.Id] / total * 100, 1)
            })
            .OrderByDescending(tu => tu.Count)
            .Take(topN)
            .ToList();
    }

    public async Task<List<WordCountTrend>> GetWordCountTrendAsync(int lastNDays = 30)
    {
        var db = _databaseService.GetConnection();
        var startDate = DateTime.Today.AddDays(-lastNDays);

        var entries = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= startDate)
            .ToListAsync();

        return entries
            .GroupBy(e => e.EntryDate.Date)
            .Select(g => new WordCountTrend
            {
                Date = g.Key,
                WordCount = g.Sum(e => e.WordCount)
            })
            .OrderBy(wc => wc.Date)
            .ToList();
    }

    public async Task<MoodType?> GetMostFrequentMoodAsync()
    {
        var distribution = await GetMoodDistributionAsync();
        return distribution.FirstOrDefault()?.Mood;
    }

    public async Task<int> GetTotalWordCountAsync()
    {
        var db = _databaseService.GetConnection();
        var entries = await db.Table<JournalEntry>().ToListAsync();
        return entries.Sum(e => e.WordCount);
    }

    public async Task<double> GetAverageWordCountAsync()
    {
        var db = _databaseService.GetConnection();
        var entries = await db.Table<JournalEntry>().ToListAsync();

        if (entries.Count == 0)
            return 0;

        return Math.Round(entries.Average(e => e.WordCount), 1);
    }

    public async Task<List<MonthlyStats>> GetMonthlyStatsAsync(int lastNMonths = 6)
    {
        var db = _databaseService.GetConnection();
        var entries = await db.Table<JournalEntry>().ToListAsync();

        var startDate = DateTime.Today.AddMonths(-lastNMonths);

        return entries
            .Where(e => e.EntryDate >= startDate)
            .GroupBy(e => new { e.EntryDate.Year, e.EntryDate.Month })
            .Select(g => new MonthlyStats
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                EntryCount = g.Count(),
                TotalWords = g.Sum(e => e.WordCount),
                AverageWords = Math.Round(g.Average(e => e.WordCount), 1)
            })
            .OrderBy(ms => ms.Year)
            .ThenBy(ms => ms.Month)
            .ToList();
    }
}
