using System;
using System.IO;

namespace Blitztext.Core;

public static class AppStorage
{
    private static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Blitztext");

    private static string ProgramDir => AppContext.BaseDirectory;

    public static bool UsesPackageCompatiblePaths => RuntimeEnvironment.HasPackageIdentity;

    public static string ConfigPath =>
        UsesPackageCompatiblePaths
            ? Path.Combine(AppDataDir, "config.json")
            : Path.Combine(ProgramDir, "config.json");

    public static string? ConfigFallbackPath =>
        UsesPackageCompatiblePaths ? null : Path.Combine(AppDataDir, "config.json");

    public static string EnvPath =>
        UsesPackageCompatiblePaths
            ? Path.Combine(AppDataDir, ".env")
            : Path.Combine(ProgramDir, ".env");

    public static string? EnvFallbackPath =>
        UsesPackageCompatiblePaths ? null : Path.Combine(AppDataDir, ".env");

    public static string LogPath =>
        UsesPackageCompatiblePaths
            ? Path.Combine(AppDataDir, "blitztext.log")
            : Path.Combine(ProgramDir, "blitztext.log");

    public static string? LogFallbackPath =>
        UsesPackageCompatiblePaths ? null : Path.Combine(AppDataDir, "blitztext.log");
}