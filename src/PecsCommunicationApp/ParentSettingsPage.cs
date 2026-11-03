using PecsCommunicationApp.Models;
using PecsCommunicationApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace PecsCommunicationApp;

public sealed class ParentSettingsPage : ContentPage
{
    private readonly LocalizationService _text;
    private readonly CardStore _store;
    private readonly VerticalStackLayout _cards = new() { Spacing = 10 };
    private readonly Button _addButton = new() { CornerRadius = 14, BackgroundColor = Color.FromArgb("#1976D2"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, HeightRequest = 52 };

    public ParentSettingsPage(LocalizationService text, CardStore store)
    {
        _text = text;
        _store = store;
        BackgroundColor = Color.FromArgb("#F4F7FC");
        _addButton.Clicked += async (_, _) => await OpenEditorAsync(new CommunicationCard { SortOrder = 100 });
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new Label { Text = _text["mediaHint"], FontSize = 16, TextColor = Color.FromArgb("#43516A") },
                    _addButton,
                    _cards
                }
            }
        };
        Title = _text["parentMode"];
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = _text["editCards"];
        _addButton.Text = $"＋  {_text["addCard"]}";
        await LoadCardsAsync();
    }

    private async Task LoadCardsAsync()
    {
        _cards.Children.Clear();
        foreach (var card in (await _store.GetAllAsync()).OrderBy(card => card.SortOrder))
        {
            var label = new Label
            {
                Text = $"{card.LabelEn}  ·  {card.LabelRu}",
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#17263F"),
                VerticalOptions = LayoutOptions.Center
            };
            var edit = new Button { Text = "✎", FontSize = 20, WidthRequest = 48, HeightRequest = 46, CornerRadius = 12, BackgroundColor = Color.FromArgb("#E7F0FC"), TextColor = Color.FromArgb("#174A85") };
            var delete = new Button { Text = "⌫", FontSize = 20, WidthRequest = 48, HeightRequest = 46, CornerRadius = 12, BackgroundColor = Color.FromArgb("#FFF0EF"), TextColor = Color.FromArgb("#9A322C") };
            edit.Clicked += async (_, _) => await OpenEditorAsync(card);
            delete.Clicked += async (_, _) =>
            {
                if (await DisplayAlertAsync(_text["delete"], _text["deleteConfirm"], _text["delete"], _text["cancel"]))
                {
                    await _store.DeleteAsync(card);
                    await LoadCardsAsync();
                }
            };
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 8,
                Children = { label, edit, delete }
            };
            Grid.SetColumn(edit, 1);
            Grid.SetColumn(delete, 2);
            _cards.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#D5E0F0"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(12, 6),
                Content = row
            });
        }
    }

    private async Task OpenEditorAsync(CommunicationCard card)
    {
        var editor = new CardEditorPage(_text, _store, card);
        await Navigation.PushAsync(editor);
        await LoadCardsAsync();
    }
}
