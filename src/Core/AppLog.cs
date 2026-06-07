using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Blitztext.Core;

/// <summary>
/// Thread-safe in-memory ring buffer log (max 100 entries).
/// Also auto-writes to blitztext.log at the active runtime storage path for crash analysis.
/// </summary>
public static class AppLog
{
    private static readonly Lock _lock = new();
    private static readonly Queue<string> _entries = new(100);
    private const int MaxEntries = 100;

    public static string LastTranscript { get; private set; } = "";
    public static string LastProcessed  { get; private set; } = "";
    public static string LastMode       { get; private set; } = "";
    public static string LastError      { get; private set; } = "";

    public static string LogFilePath => AppStorage.LogPath;

    public static void Add(string message)
    {
        var ts    = DateTime.Now.ToString("HH:mm:ss");
        var entry = $"[{ts}] {message}";
        lock (_lock)
        {
            if (_entries.Count >= MaxEntries)
                _entries.Dequeue();
            _entries.Enqueue(entry);
        }
        WriteToFileAsync(entry);
    }

    public static IReadOnlyList<string> GetAll()
    {
        lock (_lock)
            return [.. _entries];
    }

    public static void Clear()
    {
        lock (_lock)
            _entries.Clear();
    }

    public static void SetLast(string transcript, string processed, string mode)
    {
        LastTranscript = transcript;
        LastProcessed  = processed;
        LastMode       = mode;
        LastError      = "";
    }

    public static void SetError(string message)
    {
        LastError = message;
        Add($"FEHLER: {message}");
    }

    private static void WriteToFileAsync(string entry)
    {
        try
        {
            AppendWithFallback(LogFilePath, AppStorage.LogFallbackPath, entry + Environment.NewLine);
        }
        catch { /* never crash on logging */ }
    }

    private static void AppendWithFallback(string primaryPath, string? fallbackPath, string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(primaryPath)!);
            File.AppendAllText(primaryPath, content, System.Text.Encoding.UTF8);
        }
        catch
        {
            if (string.IsNullOrEmpty(fallbackPath))
                throw;

            Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath)!);
            File.AppendAllText(fallbackPath, content, System.Text.Encoding.UTF8);
        }
    }
}
