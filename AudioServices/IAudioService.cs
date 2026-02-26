using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBF.DataModel;

namespace DBF.AudioServices;

public interface IAudioService
{
    void Play(string path);

    void Play(string path, int volume);

    void Pause();

    void Resume();

    void Stop();

    bool IsPlaying { get; }

    int  Volume    { get; set; }
}
