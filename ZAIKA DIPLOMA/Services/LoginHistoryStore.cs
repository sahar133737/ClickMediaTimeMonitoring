using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClickMediaWorkTime.Services
{
    /// <summary>Последние логины для списка на форме входа.</summary>
    internal static class LoginHistoryStore
    {
        private static readonly string PathFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "clickmedia-login-history.txt");

        private const int MaxItems = 12;

        public static IReadOnlyList<string> Load()
        {
            try
            {
                if (!File.Exists(PathFile))
                {
                    return Array.Empty<string>();
                }

                return File.ReadAllLines(PathFile)
                    .Select(l => l.Trim())
                    .Where(l => l.Length >= 3 && l.Length <= 64)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxItems)
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static void Remember(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                return;
            }

            login = login.Trim();
            if (login.Length < 3)
            {
                return;
            }

            try
            {
                var list = new List<string> { login };
                foreach (var x in Load())
                {
                    if (!list.Contains(x, StringComparer.OrdinalIgnoreCase))
                    {
                        list.Add(x);
                    }
                }

                while (list.Count > MaxItems)
                {
                    list.RemoveAt(list.Count - 1);
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
