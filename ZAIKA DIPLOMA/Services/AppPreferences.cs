using System;
using System.IO;

namespace ClickMediaWorkTime.Services
{
    /// <summary>Локальные настройки приложения (без БД).</summary>
    internal static class AppPreferences
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClickMediaWorkTime");

        private static readonly string PathFile = Path.Combine(Dir, "preferences.ini");

        public static bool AutoBackupOnExit
        {
            get => ReadBool("AutoBackupOnExit", defaultValue: false);
            set => WriteBool("AutoBackupOnExit", value);
        }

        private static bool ReadBool(string key, bool defaultValue)
        {
            try
            {
                if (!File.Exists(PathFile))
                {
                    return defaultValue;
                }

                foreach (var line in File.ReadAllLines(PathFile))
                {
                    var t = line.Trim();
                    if (t.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        var v = t.Substring(key.Length + 1).Trim();
                        return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                // ignore
            }

            return defaultValue;
        }

        private static void WriteBool(string key, bool value)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var lines = File.Exists(PathFile) ? File.ReadAllLines(PathFile) : Array.Empty<string>();
                var list = new System.Collections.Generic.List<string>();
                var found = false;
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(key + "=" + (value ? "1" : "0"));
                        found = true;
                    }
                    else
                    {
                        list.Add(line);
                    }
                }

                if (!found)
                {
                    list.Add(key + "=" + (value ? "1" : "0"));
                }

                File.WriteAllLines(PathFile, list.ToArray());
            }
            catch
            {
                // ignore
            }
        }
    }
}
