using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace Blitztext.Core;

/// <summary>
/// Records audio from the default microphone into an in-memory WAV buffer.
/// WaveInEvent is created ONCE and reused to avoid InvalidHandle errors
/// caused by rapid create/dispose cycles.
/// </summary>
public sealed class AudioRecorder : IDisposable
{
    private const int SampleRate = 16_000;
    private const int Channels   = 1;
    private const int BitDepth   = 16;

    private static readonly WaveFormat Fmt = new(SampleRate, BitDepth, Channels);

    // Created once, reused for every recording session
    private WaveInEvent?             _waveIn;
    private MemoryStream?            _ms;
    private WaveFileWriter?          _writer;
    private readonly object          _lock = new();
    private bool                     _recording;
    // Starts "set" = device is idle. Reset when StartRecording, Set again on RecordingStopped.
    private readonly ManualResetEventSlim _deviceIdle = new(initialState: true);

    public bool IsRecording => _recording;

    private void EnsureDevice()
    {
        if (_waveIn != null) return;
        _waveIn = new WaveInEvent { WaveFormat = Fmt, BufferMilliseconds = 50 };
        _waveIn.DataAvailable    += OnData;
        _waveIn.RecordingStopped += OnStopped;
    }

    private void ResetDevice()
    {
        if (_waveIn == null) return;

        try { _waveIn.DataAvailable    -= OnData; } catch { }
        try { _waveIn.RecordingStopped -= OnStopped; } catch { }
        try { _waveIn.Dispose(); } catch { }

        _waveIn = null;
        _deviceIdle.Set();
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_recording) return;

            // Wait up to 500 ms for the device to signal idle after previous StopRecording
            if (!_deviceIdle.Wait(500))
            {
                AppLog.Add("AudioRecorder.Start: Warten auf RecordingStopped Timeout — Gerät wird neu initialisiert");
                ResetDevice();
            }

            EnsureDevice();

            _ms     = new MemoryStream();
            _writer = new WaveFileWriter(_ms, Fmt);

            // Mark device as busy — OnStopped will set it idle again
            _deviceIdle.Reset();

            _waveIn!.StartRecording();
            _recording = true;
        }
    }

    /// <summary>
    /// Stop recording and return a WAV MemoryStream, or null if audio is too short.
    /// </summary>
    public MemoryStream? Stop()
    {
        WaveFileWriter? wf;
        MemoryStream?   ms;

        lock (_lock)
        {
            if (!_recording) return null;
            _recording = false;
            wf = _writer;
            ms = _ms;
            _writer = null;
            _ms     = null;
        }

        // Signal WaveIn to stop; RecordingStopped will set _deviceIdle
        try { _waveIn?.StopRecording(); }
        catch (Exception ex) { AppLog.Add($"StopRecording Fehler: {ex.Message}"); _deviceIdle.Set(); }

        if (!_deviceIdle.Wait(750))
        {
            AppLog.Add("AudioRecorder.Stop: RecordingStopped Timeout — Gerät wird neu initialisiert");
            lock (_lock)
                ResetDevice();
        }

        // Flush and finalise WAV header
        try { wf?.Flush(); wf?.Dispose(); }
        catch (Exception ex) { AppLog.Add($"WaveFileWriter.Dispose Fehler: {ex.Message}"); }

        byte[]? bytes;
        try   { bytes = ms?.ToArray(); }
        catch { bytes = null; }
        finally { try { ms?.Dispose(); } catch { } }

        const int WavHeaderBytes = 44;
        const int MinAudioBytes  = WavHeaderBytes + SampleRate * (BitDepth / 8) * 3 / 10; // 300 ms

        if (bytes == null || bytes.Length <= WavHeaderBytes)
        {
            AppLog.Add("Aufnahme verworfen — keine Audiodaten (nur WAV-Header)");
            return null;
        }

        if (bytes.Length < MinAudioBytes)
        {
            int ms_ = (bytes.Length - WavHeaderBytes) * 1000 / (SampleRate * BitDepth / 8);
            AppLog.Add($"Aufnahme verworfen — zu kurz ({ms_} ms, Minimum 300 ms)");
            return null;
        }

        return new MemoryStream(bytes);
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        lock (_lock)
        {
            if (_recording)
                _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            AppLog.Add($"Aufnahme-Fehler: {e.Exception.Message}");
        // Unblock Start() for the next recording
        _deviceIdle.Set();
    }

    public void Dispose()
    {
        try { Stop()?.Dispose(); } catch { }
        try { _waveIn?.Dispose(); } catch { }
        _waveIn = null;
        _deviceIdle.Dispose();
    }
}
