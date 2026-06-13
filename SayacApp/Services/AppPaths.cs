using System;
using System.IO;

namespace SayacApp.Services;

public static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            string baseDir;
            if (OperatingSystem.IsWindows())
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            else if (OperatingSystem.IsMacOS())
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support");
            else
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var dir = Path.Combine(baseDir, "Sayac");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    /// <summary>Likely location of an old AHK INI sitting next to a copy of the script.</summary>
    public static string LegacyIniGuess =>
        Path.Combine(AppContext.BaseDirectory, "sayac_data.ini");
}
