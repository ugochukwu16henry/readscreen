using System.Windows;
using System.Windows.Controls;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.App;

public partial class MemoryEditorWindow : Window
{
    private readonly IMemoryStore _memory;
    private Guid? _selectedId;
    private static readonly string[] Categories =
    [
        "Education", "WorkHistory", "Skills", "Certifications",
        "Projects", "Achievements", "CareerGoals"
    ];

    public MemoryEditorWindow(IMemoryStore memory)
    {
        _memory = memory;
        InitializeComponent();
        CategoryBox.ItemsSource = Categories;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var items = await _memory.GetAllAsync();
        MemoryList.ItemsSource = items;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MemoryList.SelectedItem is not MemoryEntry entry)
            return;

        _selectedId = entry.Id;
        CategoryBox.SelectedItem = entry.Category;
        TitleBox.Text = entry.Title;
        ContentBox.Text = entry.Content;
    }

    private void OnNew(object sender, RoutedEventArgs e)
    {
        _selectedId = null;
        CategoryBox.SelectedIndex = 0;
        TitleBox.Text = string.Empty;
        ContentBox.Text = string.Empty;
        MemoryList.SelectedItem = null;
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        var entry = new MemoryEntry(
            _selectedId ?? Guid.NewGuid(),
            CategoryBox.SelectedItem?.ToString() ?? "Projects",
            TitleBox.Text.Trim(),
            ContentBox.Text.Trim(),
            DateTime.UtcNow);

        await _memory.UpsertAsync(entry);
        _selectedId = entry.Id;
        await RefreshAsync();
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue)
            return;

        await _memory.DeleteAsync(_selectedId.Value);
        OnNew(sender, e);
        await RefreshAsync();
    }
}
