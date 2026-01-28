using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using myjournal.Models;
using myjournal.Services;
using System.Collections.ObjectModel;

namespace myjournal.ViewModels;

public partial class TagsViewModel : BaseViewModel
{
    private readonly ITagService _tagService;

    [ObservableProperty]
    private ObservableCollection<Tag> _customTags = new();

    [ObservableProperty]
    private ObservableCollection<Tag> _preBuiltTags = new();

    [ObservableProperty]
    private string _newTagName = string.Empty;

    [ObservableProperty]
    private string _newTagColor = "#6366F1"; // Default purple-ish color

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private Tag? _editingTag;

    [ObservableProperty]
    private string _editTagName = string.Empty;

    [ObservableProperty]
    private string _editTagColor = string.Empty;

    public TagsViewModel(ITagService tagService)
    {
        _tagService = tagService;
        Title = "Manage Tags";
    }

    [RelayCommand]
    public async Task LoadTagsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var custom = await _tagService.GetCustomTagsAsync();
            var prebuilt = await _tagService.GetPreBuiltTagsAsync();

            CustomTags = new ObservableCollection<Tag>(custom);
            PreBuiltTags = new ObservableCollection<Tag>(prebuilt);
        }
        catch (Exception ex)
        {
            SetError($"Failed to load tags: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
        {
            SetError("Tag name cannot be empty");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            var newTag = await _tagService.CreateTagAsync(NewTagName, NewTagColor);
            CustomTags.Add(newTag);

            // Reset form
            NewTagName = string.Empty;
            NewTagColor = "#6366F1";
        }
        catch (Exception ex)
        {
            SetError($"Failed to create tag: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateTagAsync()
    {
        if (EditingTag == null || string.IsNullOrWhiteSpace(EditTagName))
        {
            SetError("Invalid tag data");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            EditingTag.Name = EditTagName;
            EditingTag.Color = EditTagColor;

            await _tagService.UpdateTagAsync(EditingTag);

            // Refresh list to ensure UI updates
            var index = CustomTags.IndexOf(EditingTag);
            if (index >= 0)
            {
                CustomTags[index] = EditingTag;
            }

            CancelEditing();
        }
        catch (Exception ex)
        {
            SetError($"Failed to update tag: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteTagAsync(Tag tag)
    {
        if (tag.IsPreBuilt) return;
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ClearError();

            await _tagService.DeleteTagAsync(tag.Id);
            CustomTags.Remove(tag);
        }
        catch (Exception ex)
        {
            SetError($"Failed to delete tag: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void StartEditing(Tag tag)
    {
        EditingTag = tag;
        EditTagName = tag.Name;
        EditTagColor = tag.Color;
        IsEditing = true;
    }

    [RelayCommand]
    public void CancelEditing()
    {
        EditingTag = null;
        EditTagName = string.Empty;
        EditTagColor = string.Empty;
        IsEditing = false;
    }
}
