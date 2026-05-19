using System;
using System.Linq;

namespace DBF.AudioServices;

public interface IAudioService
{
    //void Play(string path);
    void Play(SoundDefinition sound);

    //void Play(string path, int volume);

    void Play(SoundDefinition sound, int volume);

    void Pause();

    void Resume();

    void Stop();

    bool IsPlaying { get; }

    int Volume { get; set; }
}
