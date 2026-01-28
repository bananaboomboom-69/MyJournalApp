using myjournal.Models;

namespace myjournal.Services;

/// <summary>
/// Interface for tag operations
/// </summary>
public interface ITagService
{
    Task<List<Tag>> GetAllTagsAsync();
    Task<List<Tag>> GetPreBuiltTagsAsync();
    Task<List<Tag>> GetCustomTagsAsync();
    Task<Tag?> GetTagByIdAsync(int id);
    Task<Tag> CreateTagAsync(string name, string color);
    Task DeleteTagAsync(int id);
    Task<List<Tag>> GetTagsForEntryAsync(int entryId);
    Task<Tag> UpdateTagAsync(Tag tag);
}

/// <summary>
/// Service for managing tags
/// </summary>
public class TagService : ITagService
{
    private readonly IDatabaseService _databaseService;

    public TagService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<Tag>> GetAllTagsAsync()
    {
        var db = _databaseService.GetConnection();
        return await db.Table<Tag>()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetPreBuiltTagsAsync()
    {
        var db = _databaseService.GetConnection();
        return await db.Table<Tag>()
            .Where(t => t.IsPreBuilt)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetCustomTagsAsync()
    {
        var db = _databaseService.GetConnection();
        return await db.Table<Tag>()
            .Where(t => !t.IsPreBuilt)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        return await _databaseService.GetByIdAsync<Tag>(id);
    }

    public async Task<Tag> CreateTagAsync(string name, string color)
    {
        var tag = new Tag
        {
            Name = name,
            Color = color,
            IsPreBuilt = false,
            CreatedAt = DateTime.Now
        };

        await _databaseService.SaveAsync(tag);
        return tag;
    }

    public async Task DeleteTagAsync(int id)
    {
        var db = _databaseService.GetConnection();

        // Delete entry-tag associations first
        await db.ExecuteAsync("DELETE FROM EntryTag WHERE TagId = ?", id);

        // Delete the tag
        var tag = await GetTagByIdAsync(id);
        if (tag != null && !tag.IsPreBuilt)
        {
            await db.DeleteAsync(tag);
        }
    }

    public async Task<List<Tag>> GetTagsForEntryAsync(int entryId)
    {
        var db = _databaseService.GetConnection();

        var entryTags = await db.Table<EntryTag>()
            .Where(et => et.EntryId == entryId)
            .ToListAsync();

        var tags = new List<Tag>();

        foreach (var entryTag in entryTags)
        {
            var tag = await db.FindAsync<Tag>(entryTag.TagId);
            if (tag != null)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    public async Task<Tag> UpdateTagAsync(Tag tag)
    {
        var db = _databaseService.GetConnection();
        await db.UpdateAsync(tag);
        return tag;
    }
}
