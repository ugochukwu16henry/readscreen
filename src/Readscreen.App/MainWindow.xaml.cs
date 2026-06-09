using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;
using Readscreen.Core.Services;
using Readscreen.Overlay;
using Readscreen.Perception;

namespace Readscreen.App;

public partial class MainWindow : Window
{
    private readonly IAppSettings _settings;
    private readonly IOverlayService _overlay;
    private readonly ContextOrchestrator _orchestrator;
    private readonly IDocumentStore _documents;
    private GlobalHotkeyService? _hotkeys;

    public MainWindow(
        IAppSettings settings,
        IOverlayService overlay,
        ContextOrchestrator orchestrator,
        IDocumentStore documents)
    {
        _settings = settings;
        _overlay = overlay;
        _orchestrator = orchestrator;
        _documents = documents;
        InitializeComponent();
        LoadSettings();
    }

    public void RegisterHotkeys(GlobalHotkeyService hotkeys)
    {
        _hotkeys = hotkeys;
        _hotkeys.Register(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x48, () => _overlay.Toggle());
        _hotkeys.Register(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x50, OnTogglePause);
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        TopBox.Text = s.CaptureRegion.Top.ToString();
        LeftBox.Text = s.CaptureRegion.Left.ToString();
        WidthBox.Text = s.CaptureRegion.Width.ToString();
        HeightBox.Text = s.CaptureRegion.Height.ToString();
        ModelBox.Text = s.LlmModel;
        PollBox.Text = s.PollIntervalSeconds.ToString();
        OllamaUrlBox.Text = s.OllamaBaseUrl;
        OpacitySlider.Value = s.OverlayOpacity;
        MeetingAssistBox.IsChecked = s.MeetingAssistEnabled;
        ClickThroughBox.IsChecked = s.ClickThrough;
        AudioEnabledBox.IsChecked = s.AudioEnabled;

        AnswerModeBox.ItemsSource = Enum.GetValues<AnswerMode>();
        AnswerModeBox.SelectedItem = s.AnswerMode;

        AudioInputModeBox.ItemsSource = Enum.GetValues<AudioInputMode>();
        AudioInputModeBox.SelectedItem = s.AudioInputMode;

        UpdateSessionLabel();
        _ = RefreshDocumentListAsync();
    }

    private async void OnPickRegion(object sender, RoutedEventArgs e)
    {
        var picker = new RegionPickerWindow();
        if (picker.ShowDialog() == true && picker.SelectedRegion != null)
        {
            var r = picker.SelectedRegion;
            TopBox.Text = r.Top.ToString();
            LeftBox.Text = r.Left.ToString();
            WidthBox.Text = r.Width.ToString();
            HeightBox.Text = r.Height.ToString();
            RegionStatus.Text = $"Region: {r.Width}x{r.Height} at ({r.Left},{r.Top})";
        }
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _overlay.SetOpacity(e.NewValue);
        _settings.Current.OverlayOpacity = e.NewValue;
    }

    private void OnClickThroughChanged(object sender, RoutedEventArgs e)
    {
        var enabled = ClickThroughBox.IsChecked == true;
        _overlay.SetClickThrough(enabled);
        _settings.Current.ClickThrough = enabled;
    }

    private void OnMeetingAssistChanged(object sender, RoutedEventArgs e)
    {
        var enabled = MeetingAssistBox.IsChecked == true;
        _settings.Current.MeetingAssistEnabled = enabled;

        if (enabled && AudioEnabledBox.IsChecked != true)
            AudioEnabledBox.IsChecked = true;

        StatusBar.Text = enabled
            ? "Meeting assist enabled: live audio questions will be answered privately on your overlay."
            : "Meeting assist disabled.";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var s = _settings.Current;
        s.CaptureRegion.Top = int.Parse(TopBox.Text);
        s.CaptureRegion.Left = int.Parse(LeftBox.Text);
        s.CaptureRegion.Width = int.Parse(WidthBox.Text);
        s.CaptureRegion.Height = int.Parse(HeightBox.Text);
        s.LlmModel = ModelBox.Text.Trim();
        s.PollIntervalSeconds = double.Parse(PollBox.Text);
        s.OllamaBaseUrl = OllamaUrlBox.Text.Trim();
        s.AnswerMode = (AnswerMode)(AnswerModeBox.SelectedItem ?? AnswerMode.Hybrid);
        s.AudioInputMode = (AudioInputMode)(AudioInputModeBox.SelectedItem ?? AudioInputMode.SystemAudio);
        s.AudioEnabled = AudioEnabledBox.IsChecked == true;
        s.MeetingAssistEnabled = MeetingAssistBox.IsChecked == true;
        s.OverlayOpacity = OpacitySlider.Value;
        s.ClickThrough = ClickThroughBox.IsChecked == true;

        if (s.MeetingAssistEnabled)
            s.AudioEnabled = true;

        _settings.Save();
        StatusBar.Text = "Settings saved.";
    }

    private void OnToggleOverlay(object sender, RoutedEventArgs e) => _overlay.Toggle();

    private void OnTogglePause(object sender, RoutedEventArgs e) => OnTogglePause();

    private void OnTogglePause()
    {
        _orchestrator.IsPaused = !_orchestrator.IsPaused;
        StatusBar.Text = _orchestrator.IsPaused ? "Assistant paused." : "Assistant resumed.";
        _overlay.SetStatus(_orchestrator.IsPaused ? AssistantStatus.Paused : AssistantStatus.Idle);
    }

    private async void OnNewSession(object sender, RoutedEventArgs e)
    {
        await CreateNewSessionAsync();
    }

    private async Task CreateNewSessionAsync()
    {
        var sessionId = await _documents.CreateSessionAsync($"Session {DateTime.Now:g}");
        _settings.Current.ActiveDocumentSessionId = sessionId;
        _settings.Save();
        UpdateSessionLabel();
        await RefreshDocumentListAsync();
        StatusBar.Text = "New document session created.";
    }

    private async void OnUploadDocument(object sender, RoutedEventArgs e)
    {
        if (!_settings.Current.ActiveDocumentSessionId.HasValue)
            await CreateNewSessionAsync();

        if (!_settings.Current.ActiveDocumentSessionId.HasValue)
            return;

        var dialog = new OpenFileDialog
        {
            Filter = "Documents|*.pdf;*.docx;*.pptx;*.txt;*.md|All files|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        StatusBar.Text = "Ingesting document...";
        try
        {
            await _documents.IngestAsync(_settings.Current.ActiveDocumentSessionId!.Value, dialog.FileName);
            await RefreshDocumentListAsync();
            StatusBar.Text = $"Ingested: {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Upload failed: {ex.Message}";
        }
    }

    private async Task RefreshDocumentListAsync()
    {
        DocumentList.Items.Clear();
        if (!_settings.Current.ActiveDocumentSessionId.HasValue)
            return;

        var files = await _documents.GetSessionFilesAsync(_settings.Current.ActiveDocumentSessionId.Value);
        foreach (var f in files)
            DocumentList.Items.Add(f);
    }

    private void UpdateSessionLabel()
    {
        SessionLabel.Text = _settings.Current.ActiveDocumentSessionId.HasValue
            ? $"Active session: {_settings.Current.ActiveDocumentSessionId}"
            : "No active document session";
    }

    private void OnManageMemory(object sender, RoutedEventArgs e)
    {
        var editor = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<MemoryEditorWindow>(App.Services);
        editor.Show();
    }
}
