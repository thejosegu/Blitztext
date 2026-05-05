using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Blitztext.Core;

/// <summary>
/// Thread-safe in-memory ring buffer log (max 100 entries).
/// Also auto-writes to %APPDATA%\Blitztext\blitztext.log for crash analysis.
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

    public static string LogFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Blitztext", "blitztext.log");

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
            var dir = Path.GetDirectoryName(LogFilePath)!;
            Directory.CreateDirectory(dir);
            File.AppendAllText(LogFilePath, entry + Environment.NewLine, System.Text.Encoding.UTF8);
        }
        catch { /* never crash on logging */ }
    }
}
