using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevOpTyper.Models;
using DevOpTyper.Services;

namespace DevOpTyper.Panels;

/// <summary>
/// Panel that displays a filterable list of code snippets.
/// </summary>
public sealed partial class SnippetListPanel : UserControl
{
    private readonly ContentLibraryService _contentLibrary = new();
    private List<Snippet> _allSnippets = new();
    private string _selectedLanguage = "";
    private int? _selectedDifficulty = null;

    public event EventHandler<Snippet>? SnippetSelected;

    public SnippetListPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _contentLibrary.Initialize();
        PopulateLanguageFilter();
        LoadAllSnippets();
        ApplyFilters();
    }

    private void PopulateLanguageFilter()
    {
        var tracks = _contentLibrary.GetLanguageTracks();
        foreach (var track in tracks)
        {
            LanguageFilter.Items.Add(new ComboBoxItem
            {
                Content = $"{track.Icon} {track.DisplayName} ({track.SnippetCount})",
                Tag = track.Id
            });
        }
    }

    private void LoadAllSnippets()
    {
        _allSnippets.Clear();
        var tracks = _contentLibrary.GetLanguageTracks();
        foreach (var track in tracks)
        {
            var snippets = _contentLibrary.GetSnippets(track.Id);
            _allSnippets.AddRange(snippets);
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<Snippet> filtered = _allSnippets;

        // Language filter
        if (!string.IsNullOrEmpty(_selectedLanguage))
        {
            filtered = filtered.Where(s => 
                s.Language.Equals(_selectedLanguage, StringComparison.OrdinalIgnoreCase));
        }

        // Difficulty filter
        if (_selectedDifficulty.HasValue)
        {
            filtered = filtered.Where(s => s.Difficulty == _selectedDifficulty.Value);
        }

        var list = filtered.OrderBy(s => s.Language).ThenBy(s => s.Difficulty).ToList();
        SnippetList.ItemsSource = list;
        CountText.Text = $"({list.Count})";
    }

    private void LanguageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageFilter.SelectedItem is ComboBoxItem item)
        {
            _selectedLanguage = item.Tag as string ?? "";
            ApplyFilters();
        }
    }

    private void DifficultyFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DifficultyFilter.SelectedItem is ComboBoxItem item)
        {
            if (item.Tag is string tagStr && int.TryParse(tagStr, out var diff))
            {
                _selectedDifficulty = diff;
            }
            else
            {
                _selectedDifficulty = null;
            }
            ApplyFilters();
        }
    }

    private void SnippetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SnippetList.SelectedItem is Snippet snippet)
        {
            SnippetSelected?.Invoke(this, snippet);
        }
    }

    /// <summary>
    /// Refreshes the snippet list.
    /// </summary>
    public void Refresh()
    {
        LoadAllSnippets();
        ApplyFilters();
    }

    /// <summary>
    /// Sets the selected language filter.
    /// </summary>
    public void SetLanguageFilter(string language)
    {
        _selectedLanguage = language;
        
        // Find and select the matching item
        for (int i = 0; i < LanguageFilter.Items.Count; i++)
        {
            if (LanguageFilter.Items[i] is ComboBoxItem item && 
                (item.Tag as string)?.Equals(language, StringComparison.OrdinalIgnoreCase) == true)
            {
                LanguageFilter.SelectedIndex = i;
                break;
            }
        }
    }
}
