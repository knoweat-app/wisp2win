using System;
using System.IO;
using NAudio.Wave;

namespace Wisp2Win.Services;

public sealed class AudioRecorder : IDisposable
{
    private readonly object _syncRoot = new();
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _currentPath;

    public bool IsRecording => _waveIn is not null;

    public string Start()
    {
        if (_waveIn is not null)
        {
            throw new InvalidOperationException("Recording is already active.");
        }

        AppPaths.Ensure();
        _currentPath = Path.Combine(AppPaths.TempDirectory, $"dictation-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.wav");

        _waveIn = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 50
        };

        _writer = new WaveFileWriter(_currentPath, _waveIn.WaveFormat);
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += (_, _) => Cleanup();
        _waveIn.StartRecording();
        AppLog.Info("recording", $"Started: {_currentPath}");
        return _currentPath;
    }

    public string Stop()
    {
        if (_waveIn is null || _currentPath is null)
        {
            throw new InvalidOperationException("Recording is not active.");
        }

        var path = _currentPath;
        _currentPath = null;
        _waveIn.StopRecording();
        Cleanup();
        AppLog.Info("recording", $"Stopped: {path}");
        return path;
    }

    public void Dispose()
    {
        _waveIn?.StopRecording();
        Cleanup();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs args)
    {
        lock (_syncRoot)
        {
            _writer?.Write(args.Buffer, 0, args.BytesRecorded);
        }
    }

    private void Cleanup()
    {
        lock (_syncRoot)
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
        }
    }
}
