# PECS Communication App

An initial offline-first .NET MAUI app for Windows 11 x64 and Android 13+.

## Open in Visual Studio

Open `src/PecsCommunicationApp/PecsCommunicationApp.csproj` in Visual Studio with the .NET MAUI workload installed. Choose a Windows x64 target or an Android emulator/device.

## Current prototype

- Ten starter cards from the design document, with emoji placeholders.
- English and Russian UI and card labels, loaded from JSON locale files.
- Local SQLite storage for card edits.
- Parent settings for adding, editing, ordering, enabling, and deleting cards.
- Import a picture and record or import separate English and Russian audio for each card. Media is stored locally on the device.
- Tapping a card plays its recording for the selected language. If no recording exists, the app indicates that it needs one.
- Works without an account or network connection.

## Build

```powershell
dotnet build src/PecsCommunicationApp/PecsCommunicationApp.csproj -f net10.0-windows10.0.22000.0 -p:Platform=x64
dotnet build src/PecsCommunicationApp/PecsCommunicationApp.csproj -f net10.0-android
```

The Android debug APK is emitted under `src/PecsCommunicationApp/bin/Debug/net10.0-android/`.

The project targets Windows and Android first. The later Apple ports are intentionally not enabled yet. Speech recordings and individual card images were not included in the supplied design files, so the starter board needs imported recordings before card taps can speak.
