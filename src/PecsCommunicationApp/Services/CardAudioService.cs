using Plugin.Maui.Audio;

namespace PecsCommunicationApp.Services;

public sealed class CardAudioService
{
    private IAudioPlayer? _player;
    private Stream? _stream;

    public void Play(string path)
    {
        Stop();
        _stream = File.OpenRead(path);
        _player = AudioManager.Current.CreatePlayer(_stream);
        _player.PlaybackEnded += OnPlaybackEnded;
        _player.Play();
    }

    public void Stop()
    {
        if (_player is not null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }

        _stream?.Dispose();
        _stream = null;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e) => Stop();
}
