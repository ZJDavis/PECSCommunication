using PecsCommunicationApp.Models;
using PecsCommunicationApp.Services;
using Plugin.Maui.Audio;

namespace PecsCommunicationApp;

public sealed class CardEditorPage : ContentPage
{
    private readonly LocalizationService _text;
    private readonly CardStore _store;
    private readonly CommunicationCard _card;
    private readonly bool _isNew;
    private readonly Entry _english = new() { FontSize = 18 };
    private readonly Entry _russian = new() { FontSize = 18 };
    private readonly Entry _symbol = new() { FontSize = 24, MaxLength = 8 };
    private readonly Entry _order = new() { Keyboard = Keyboard.Numeric };
    private readonly Picker _category = new();
    private readonly Switch _enabled = new() { IsToggled = true };
    private readonly Label _imageStatus = new() { FontSize = 14, TextColor = Color.FromArgb("#4D5B70") };
    private readonly Label _englishAudioStatus = new() { FontSize = 14, TextColor = Color.FromArgb("#4D5B70") };
    private readonly Label _russianAudioStatus = new() { FontSize = 14, TextColor = Color.FromArgb("#4D5B70") };
    private IAudioRecorder? _recorder;
    private string? _activeRecordingLanguage;
    private string? _activeRecordingPath;
    private Button? _englishRecordButton;
    private Button? _russianRecordButton;

    private static readonly (string Id, string En, string Ru)[] Categories =
    {
        ("needs", "Needs", "Потребности"), ("food", "Food", "Еда"), ("drinks", "Drinks", "Напитки"), ("activities", "Activities", "Занятия")
    };

    public CardEditorPage(LocalizationService text, CardStore store, CommunicationCard source)
    {
        _text = text;
        _store = store;
        _isNew = string.IsNullOrWhiteSpace(source.LabelEn) && string.IsNullOrWhiteSpace(source.LabelRu);
        _card = new CommunicationCard
        {
            Id = _isNew ? Guid.NewGuid().ToString("N") : source.Id,
            LabelEn = source.LabelEn,
            LabelRu = source.LabelRu,
            CategoryId = source.CategoryId,
            Symbol = source.Symbol,
            ImagePath = source.ImagePath,
            AudioEnPath = source.AudioEnPath,
            AudioRuPath = source.AudioRuPath,
            IsEnabled = source.IsEnabled,
            SortOrder = source.SortOrder
        };
        BuildPage();
        Title = _isNew ? _text["addCard"] : _text["editCard"];
    }

    private void BuildPage()
    {
        _english.Text = _card.LabelEn;
        _english.Placeholder = _text["englishLabel"];
        _russian.Text = _card.LabelRu;
        _russian.Placeholder = _text["russianLabel"];
        _symbol.Text = _card.Symbol;
        _symbol.Placeholder = _text["symbol"];
        _order.Text = _card.SortOrder.ToString();
        foreach (var item in Categories)
            _category.Items.Add(_text.Language == "ru" ? item.Ru : item.En);
        _category.SelectedIndex = Math.Max(0, Array.FindIndex(Categories, item => item.Id == _card.CategoryId));
        _enabled.IsToggled = _card.IsEnabled;
        RefreshMediaStatus();

        var imageButton = MediaButton(_text["chooseImage"], async () => await ChooseFileAsync("image"));
        var enAudioButton = MediaButton(_text["chooseEnglishAudio"], async () => await ChooseFileAsync("audio-en"));
        var ruAudioButton = MediaButton(_text["chooseRussianAudio"], async () => await ChooseFileAsync("audio-ru"));
        _englishRecordButton = MediaButton(_text["recordEnglish"], async () => await ToggleRecordingAsync("en"));
        _russianRecordButton = MediaButton(_text["recordRussian"], async () => await ToggleRecordingAsync("ru"));
        var saveButton = new Button { Text = _text["save"], HeightRequest = 54, CornerRadius = 14, BackgroundColor = Color.FromArgb("#1976D2"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        saveButton.Clicked += async (_, _) => await SaveAsync();

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 12,
            Children =
            {
                new Label { Text = _text["englishLabel"], FontAttributes = FontAttributes.Bold }, _english,
                new Label { Text = _text["russianLabel"], FontAttributes = FontAttributes.Bold }, _russian,
                new Label { Text = _text["symbol"], FontAttributes = FontAttributes.Bold }, _symbol,
                new Label { Text = _text["categoryLabel"], FontAttributes = FontAttributes.Bold }, _category,
                new Label { Text = _text["order"], FontAttributes = FontAttributes.Bold }, _order,
                new HorizontalStackLayout { Spacing = 10, Children = { new Label { Text = _text["enabled"], VerticalOptions = LayoutOptions.Center }, _enabled } },
                new Label { Text = _text["placeholderHint"], FontSize = 14, TextColor = Color.FromArgb("#59677C") },
                imageButton, _imageStatus,
                new Label { Text = _text["mediaHint"], FontSize = 14, TextColor = Color.FromArgb("#59677C") },
                enAudioButton, _englishAudioStatus,
                _englishRecordButton,
                ruAudioButton, _russianAudioStatus,
                _russianRecordButton,
                saveButton
            }
        };
        Content = new ScrollView { Content = stack };
        BackgroundColor = Color.FromArgb("#F4F7FC");
    }

    private Button MediaButton(string title, Func<Task> action)
    {
        var button = new Button { Text = title, CornerRadius = 12, BackgroundColor = Color.FromArgb("#E6F0FD"), TextColor = Color.FromArgb("#174A85"), HorizontalOptions = LayoutOptions.Fill };
        button.Clicked += async (_, _) => await action();
        return button;
    }

    private async Task ChooseFileAsync(string kind)
    {
        try
        {
            var audioTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "audio/*" },
                [DevicePlatform.WinUI] = new[] { ".wav", ".mp3", ".m4a", ".aac", ".flac", ".ogg" }
            });
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = kind == "image" ? _text["chooseImage"] : _text["mediaHint"],
                FileTypes = kind == "image" ? FilePickerFileType.Images : audioTypes
            });
            if (result is null)
                return;

            var folder = Path.Combine(FileSystem.AppDataDirectory, "media");
            Directory.CreateDirectory(folder);
            var extension = Path.GetExtension(result.FileName);
            var suffix = kind switch { "image" => "image", "audio-en" => "en", _ => "ru" };
            var path = Path.Combine(folder, $"{_card.Id}_{suffix}{extension}");
            await using (var source = await result.OpenReadAsync())
            await using (var destination = File.Create(path))
                await source.CopyToAsync(destination);

            if (kind == "image")
                _card.ImagePath = path;
            else if (kind == "audio-en")
                _card.AudioEnPath = path;
            else
                _card.AudioRuPath = path;

            RefreshMediaStatus();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(Title, ex.Message, _text["cancel"]);
        }
    }

    private async Task ToggleRecordingAsync(string language)
    {
        if (_activeRecordingLanguage is not null)
        {
            if (_activeRecordingLanguage == language)
                await FinishRecordingAsync();
            return;
        }

        try
        {
            var permission = await Permissions.RequestAsync<Permissions.Microphone>();
            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(Title, _text["microphoneDenied"], _text["cancel"]);
                return;
            }

            _recorder = AudioManager.Current.CreateRecorder();
            if (!_recorder.CanRecordAudio)
            {
                await DisplayAlertAsync(Title, _text["microphoneUnavailable"], _text["cancel"]);
                _recorder = null;
                return;
            }

            var folder = Path.Combine(FileSystem.AppDataDirectory, "media");
            Directory.CreateDirectory(folder);
            _activeRecordingPath = Path.Combine(folder, $"{_card.Id}_{language}.wav");
            if (File.Exists(_activeRecordingPath))
                File.Delete(_activeRecordingPath);

            await _recorder.StartAsync(_activeRecordingPath);
            _activeRecordingLanguage = language;
            UpdateRecordingControls();
        }
        catch (Exception ex)
        {
            _recorder = null;
            _activeRecordingLanguage = null;
            await DisplayAlertAsync(Title, ex.Message, _text["cancel"]);
        }
    }

    private async Task FinishRecordingAsync()
    {
        if (_recorder is null || _activeRecordingLanguage is null || _activeRecordingPath is null)
            return;

        var language = _activeRecordingLanguage;
        await _recorder.StopAsync();
        if (language == "en")
            _card.AudioEnPath = _activeRecordingPath;
        else
            _card.AudioRuPath = _activeRecordingPath;

        _recorder = null;
        _activeRecordingLanguage = null;
        _activeRecordingPath = null;
        UpdateRecordingControls();
        RefreshMediaStatus();
    }

    private void UpdateRecordingControls()
    {
        if (_englishRecordButton is not null)
            _englishRecordButton.Text = _activeRecordingLanguage == "en" ? _text["stopRecording"] : _text["recordEnglish"];
        if (_russianRecordButton is not null)
            _russianRecordButton.Text = _activeRecordingLanguage == "ru" ? _text["stopRecording"] : _text["recordRussian"];

        if (_activeRecordingLanguage == "en")
            _englishAudioStatus.Text = _text["recording"];
        else if (_activeRecordingLanguage == "ru")
            _russianAudioStatus.Text = _text["recording"];
    }

    private void RefreshMediaStatus()
    {
        _imageStatus.Text = string.IsNullOrWhiteSpace(_card.ImagePath) ? _text["placeholderHint"] : _text["imageAdded"];
        _englishAudioStatus.Text = string.IsNullOrWhiteSpace(_card.AudioEnPath) ? _text["noRecording"] : _text["audioAdded"];
        _russianAudioStatus.Text = string.IsNullOrWhiteSpace(_card.AudioRuPath) ? _text["noRecording"] : _text["audioAdded"];
    }

    private async Task SaveAsync()
    {
        if (_recorder?.IsRecording == true)
            await FinishRecordingAsync();

        var en = _english.Text?.Trim() ?? string.Empty;
        var ru = _russian.Text?.Trim() ?? string.Empty;
        if (en.Length == 0 || ru.Length == 0)
        {
            await DisplayAlertAsync(Title, _text["emptyLabel"], _text["cancel"]);
            return;
        }

        _card.LabelEn = en;
        _card.LabelRu = ru;
        _card.Symbol = string.IsNullOrWhiteSpace(_symbol.Text) ? "🖼️" : _symbol.Text.Trim();
        _card.CategoryId = _category.SelectedIndex >= 0 ? Categories[_category.SelectedIndex].Id : "needs";
        _card.IsEnabled = _enabled.IsToggled;
        _card.SortOrder = int.TryParse(_order.Text, out var order) ? order : 0;
        await _store.SaveAsync(_card);
        await Navigation.PopAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_recorder?.IsRecording == true)
        {
            await _recorder.StopAsync();
            if (!string.IsNullOrWhiteSpace(_activeRecordingPath) && File.Exists(_activeRecordingPath))
                File.Delete(_activeRecordingPath);
        }
        _recorder = null;
        _activeRecordingLanguage = null;
        _activeRecordingPath = null;
    }
}
