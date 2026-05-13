using System;
using System.Runtime.InteropServices;

namespace Blitztext.Core;

public static class RuntimeEnvironment
{
    public static bool HasPackageIdentity => GetCurrentPackageFullNameLength() != 0;

    private static int GetCurrentPackageFullNameLength()
    {
        var length = 0;
        var result = GetCurrentPackageFullName(ref length, null);
        return result == AppModelErrorNoPackage ? 0 : length;
    }

    private const int AppModelErrorNoPackage = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, string? packageFullName);
}