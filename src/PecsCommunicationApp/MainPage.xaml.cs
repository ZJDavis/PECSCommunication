using PecsCommunicationApp.Models;
using PecsCommunicationApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace PecsCommunicationApp;

public partial class MainPage : ContentPage
{
    private readonly LocalizationService _text;
    private readonly CardStore _store;
    private readonly CardAudioService _audio;
    private readonly Grid _cardGrid = new() { ColumnSpacing = 12, RowSpacing = 12 };
    private readonly HorizontalStackLayout _categories = new() { Spacing = 8 };
    private readonly Label _title = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
    private readonly Label _feedback = new() { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#253451"), VerticalOptions = LayoutOptions.Center };
    private readonly Button _languageButton = new() { CornerRadius = 18, Padding = new Thickness(16, 8), BackgroundColor = Colors.White, TextColor = Color.FromArgb("#174A85") };
    private readonly Button _settingsButton = new() { Text = "⚙", FontSize = 24, CornerRadius = 18, WidthRequest = 52, HeightRequest = 48, BackgroundColor = Colors.White, TextColor = Color.FromArgb("#174A85") };
    private List<CommunicationCard> _cards = new();
    private string _selectedCategory = "all";
    private int _columns;

    public MainPage(LocalizationService text, CardStore store, CardAudioService audio)
    {
        _text = text;
        _store = store;
        _audio = audio;
        Title = "PECS Communication";
        Background = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromArgb("#EAF3FF"), 0.0f),
            new(Color.FromArgb("#F7F9FF"), 1.0f)
        });
        BuildPage();
        _text.LanguageChanged += (_, _) =>
        {
            ApplyLanguage();
            RenderBoard();
        };
        _languageButton.Clicked += async (_, _) => await ToggleLanguageAsync();
        _settingsButton.Clicked += async (_, _) => await Navigation.PushAsync(new ParentSettingsPage(_text, _store));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _text.InitializeAsync();
        await _store.InitializeAsync();
        _cards = await _store.GetAllAsync();
        ApplyLanguage();
        RenderBoard();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var nextColumns = width < 540 ? 2 : width < 850 ? 3 : 4;
        if (nextColumns != _columns)
        {
            _columns = nextColumns;
            RenderBoard();
        }
    }

    private void BuildPage()
    {
        var header = new Grid
        {
            Padding = new Thickness(18, 12),
            BackgroundColor = Color.FromArgb("#1762AD"),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12
        };
        _title.Text = "My Communication Board";
        header.Add(_title, 0, 0);
        header.Add(_languageButton, 1, 0);
        header.Add(_settingsButton, 2, 0);

        var categoryScroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = _categories, Margin = new Thickness(12, 10, 12, 4), HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
        var boardScroll = new ScrollView { Content = _cardGrid, Padding = new Thickness(14, 10, 14, 14) };
        var feedbackBar = new Border
        {
            Stroke = Color.FromArgb("#D0DBF6"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#F1F2FF"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = new Thickness(18, 12),
            Margin = new Thickness(14, 2, 14, 14),
            Content = _feedback
        };
        var root = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };
        root.Add(header, 0, 0);
        root.Add(categoryScroll, 0, 1);
        root.Add(boardScroll, 0, 2);
        root.Add(feedbackBar, 0, 3);
        Content = root;
    }

    private void ApplyLanguage()
    {
        _title.Text = _text["appTitle"];
        _languageButton.Text = $"🌐  {(_text.Language == "en" ? "English" : "Русский")}  ▾";
        SemanticProperties.SetDescription(_settingsButton, _text["parentMode"]);
        _feedback.Text = _text["tapPrompt"];
        BuildCategories();
    }

    private void BuildCategories()
    {
        _categories.Children.Clear();
        AddCategoryChip("all", _text["allCards"]);
        AddCategoryChip("food", _text["food"]);
        AddCategoryChip("drinks", _text["drinks"]);
        AddCategoryChip("needs", _text["needs"]);
        AddCategoryChip("activities", _text["activities"]);
    }

    private void AddCategoryChip(string id, string label)
    {
        var selected = _selectedCategory == id;
        var button = new Button
        {
            Text = label,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16,
            Padding = new Thickness(16, 10),
            BackgroundColor = selected ? Color.FromArgb("#238CE2") : Colors.White,
            TextColor = selected ? Colors.White : Color.FromArgb("#243653"),
            BorderColor = Color.FromArgb("#B4C8EA"),
            BorderWidth = 1
        };
        button.Clicked += (_, _) =>
        {
            _selectedCategory = id;
            BuildCategories();
            RenderBoard();
        };
        _categories.Children.Add(button);
    }

    private void RenderBoard()
    {
        if (_columns == 0)
            return;

        _cardGrid.Children.Clear();
        _cardGrid.ColumnDefinitions.Clear();
        _cardGrid.RowDefinitions.Clear();
        for (var i = 0; i < _columns; i++)
            _cardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var visibleCards = _cards.Where(card => card.IsEnabled && (_selectedCategory == "all" || card.CategoryId == _selectedCategory)).ToList();
        var rowCount = (int)Math.Ceiling(visibleCards.Count / (double)_columns);
        for (var row = 0; row < rowCount; row++)
            _cardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (var i = 0; i < visibleCards.Count; i++)
            _cardGrid.Add(CreateTile(visibleCards[i]), i % _columns, i / _columns);
    }

    private View CreateTile(CommunicationCard card)
    {
        View visual = !string.IsNullOrWhiteSpace(card.ImagePath) && File.Exists(card.ImagePath)
            ? new Image { Source = ImageSource.FromFile(card.ImagePath), Aspect = Aspect.AspectFit, HeightRequest = 112, Margin = new Thickness(4) }
            : new Label { Text = card.Symbol, FontSize = 56, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, HeightRequest = 112 };

        var label = new Label
        {
            Text = card.LabelFor(_text.Language),
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17263F"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        var stack = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(10, 12), Children = { visual, label } };
        var border = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#C7D8F5"),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = stack,
            MinimumHeightRequest = 170
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OnCardTappedAsync(card, border);
        border.GestureRecognizers.Add(tap);
        SemanticProperties.SetDescription(border, card.LabelFor(_text.Language));
        SemanticProperties.SetHint(border, _text["tapPrompt"]);
        return border;
    }

    private async Task OnCardTappedAsync(CommunicationCard card, Border tile)
    {
        tile.BackgroundColor = Color.FromArgb("#BFE1FF");
        await tile.ScaleToAsync(0.98, 70);
        await tile.ScaleToAsync(1, 100);
        tile.BackgroundColor = Colors.White;
        var path = card.AudioFor(_text.Language);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _feedback.Text = _text["noRecording"];
            return;
        }

        try
        {
            _audio.Play(path);
            _feedback.Text = string.Format(_text["played"], card.LabelFor(_text.Language));
        }
        catch
        {
            _feedback.Text = _text["noRecording"];
        }
    }

    private async Task ToggleLanguageAsync() =>
        await _text.SetLanguageAsync(_text.Language == "en" ? "ru" : "en");
}
