using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using PecsCommunicationApp.Services;

namespace PecsCommunicationApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .AddAudio()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<LocalizationService>();
        builder.Services.AddSingleton<CardStore>();
        builder.Services.AddSingleton<CardAudioService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ParentSettingsPage>();
        builder.Services.AddTransient<CardEditorPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
