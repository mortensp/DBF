using DBF.Helpers;

using NAudio.Wave;

using System;
using System.IO;

namespace DBF.AudioServices;

public sealed class NAudioPlayer : IDisposable
{
    #region Private Fields & Properties
    private          IWavePlayer             _output;
    private          WaveStream              _wavReader;
    private          BufferedWaveProvider    _mp3Buffer;
    private          IMp3FrameDecompressor   _mp3Decompressor;
    private          VolumeWaveProvider16    _volumeProvider;
    private          CancellationTokenSource _cts;
    private          Task                    _mp3DecodeTask;
    private readonly object                  _lock = new();

    private float _volume = 1.0f;
    #endregion

    #region Constructors
    public NAudioPlayer(int deviceNumber = -1)
    {
        DeviceNumber = deviceNumber;
    }
    #endregion

    #region Public Properties
    public int DeviceNumber { get; }

    public bool IsPlaying { get; private set; }

    public bool IsPaused { get; private set; }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);

            if (_volumeProvider != null)
                _volumeProvider.Volume = _volume;
        }
    }
    #endregion

    #region Public Methods
    // -----------------------------
    // Public API
    // -----------------------------
    public void Play(Stream stream)
    {
        if (IsWav(stream))
            PlayWav(stream);
        else
            PlayMp3(stream);
    }

    public void Play(string path)
    {
        var stream = File.OpenRead(path);
        Play(stream);
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (!IsPlaying || IsPaused)
                return;

            _output?.Pause();
            IsPaused = true;
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (!IsPlaying || !IsPaused)
                return;

            _output?.Play();
            IsPaused = false;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            StopInternal();
        }
    }

    public void Dispose()
    {
        Stop();
    }
    #endregion

    #region Private Methods
    // -----------------------------
    // WAV playback
    // -----------------------------
    private void PlayWav(Stream stream)
    {
        lock (_lock)
        {
            StopInternal();

            stream.Position = 0;
            _wavReader = new WaveFileReader(stream);
            _volumeProvider = new VolumeWaveProvider16(_wavReader)
            {
                Volume = _volume
            };

            _output = new WaveOutEvent { DeviceNumber = DeviceNumber };

            try
            {
                Logger.Debug("PlayWav");
                _output.Init(_volumeProvider);
                IsPlaying = true;
                _output.Play();
            }
            catch (Exception)
            {
                _output?.Dispose();
                _output = null;
                throw;
            }
        }
    }

    // -----------------------------
    // MP3 playback (streaming)
    // -----------------------------
    private void PlayMp3(Stream stream)
    {
        lock (_lock)
        {
            StopInternal();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _mp3DecodeTask = Task.Run(() => Mp3DecodeLoopAsync(stream, token), token);  // ← INITIALISER HER
            IsPlaying = true;
        }
    }

    private async Task Mp3DecodeLoopAsync(Stream stream, CancellationToken token)
    {
        Mp3Frame frame;
        int      maxPcmBytes = 0;

        while (!token.IsCancellationRequested)
        {
            if (IsPaused)
            {
                await Task.Delay(10, token);
                continue;
            }

            Logger.Debug("PlayMp3");

            frame = Mp3Frame.LoadFromStream(stream);

            if (frame == null)
                break;

            if (_mp3Decompressor == null)
                lock (_lock)
                {
                    if (_mp3Decompressor == null)
                    {
                        _mp3Decompressor = CreateDecompressor(frame);
                        _mp3Buffer = new BufferedWaveProvider(_mp3Decompressor.OutputFormat)
                        {
                            BufferDuration = TimeSpan.FromSeconds(5)
                        };

                        _volumeProvider = new VolumeWaveProvider16(_mp3Buffer)
                        {
                            Volume = _volume
                        };

                        _output = new WaveOutEvent
                        {
                            DeviceNumber = DeviceNumber
                        };

                        _output.Init(_volumeProvider);
                        _output.Play();
                        IsPlaying = true;
                        maxPcmBytes = _mp3Decompressor.OutputFormat.AverageBytesPerSecond / 10;
                    }
                }

            var pcmBuffer    = new byte[maxPcmBytes];
            int bytesDecoded = _mp3Decompressor.DecompressFrame(frame, pcmBuffer, 0);

            if (bytesDecoded > 0
            && _mp3Buffer != null)
                _mp3Buffer.AddSamples(pcmBuffer, 0, bytesDecoded);
        }
    }

    // ****-----------------------------****
    // Format detection
    // -----------------------------
    private static bool IsWav(Stream stream)
    {
        try
        {
            long       pos    = stream.Position;
            Span<byte> header = stackalloc byte[12];

            if (stream.Read(header) != 12)
            {
                stream.Position = pos;
                return false;
            }

            stream.Position = pos;

            return header[0] == 'R' &&
                   header[1] == 'I' &&
                   header[2] == 'F' &&
                   header[3] == 'F' &&
                   header[8] == 'W' &&
                   header[9] == 'A' &&
                   header[10] == 'V' &&
                   header[11] == 'E';
        }
        catch
        {
            return false;
        }
    }

    // -----------------------------
    // Cleanup
    // -----------------------------
    private void StopInternal()
    {
        Logger.Debug("StopInternal");
        IsPlaying = false;
        IsPaused = false;

        _cts?.Cancel();
        Task.Delay(50).Wait();

        _cts = null;
        _mp3DecodeTask = null;

        _output?.Stop();
        _output?.Dispose();
        _output = null;

        _wavReader?.Dispose();
        _wavReader = null;

        _mp3Decompressor?.Dispose();
        _mp3Decompressor = null;

        _mp3Buffer = null;
        _volumeProvider = null;
    }

    // -----------------------------
    // MP3 decompresser
    // -----------------------------
    private static IMp3FrameDecompressor CreateDecompressor(Mp3Frame frame)
    {
        var waveFormat = new Mp3WaveFormat(
                                                frame.SampleRate
                                              , frame.ChannelMode == ChannelMode.Mono ? 1 : 2
                                              , frame.FrameLength
                                              , frame.BitRate);

        return new AcmMp3FrameDecompressor(waveFormat);
    }
    #endregion
}
