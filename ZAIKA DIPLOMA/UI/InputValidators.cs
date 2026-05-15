using System;
using System.Text.RegularExpressions;

namespace ClickMediaWorkTime.UI
{
    internal static class InputValidators
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            return EmailRegex.IsMatch(email.Trim());
        }

        public static bool IsValidPersonnelNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var t = value.Trim();
            return t.Length >= 3 && t.Length <= 40;
        }

        public static bool IsValidLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                return false;
            }

            var t = login.Trim();
            return t.Length >= 3 && t.Length <= 64 && !t.Contains(" ");
        }

        public static string NormalizePhoneDigits(string maskedText)
        {
            if (string.IsNullOrWhiteSpace(maskedText))
            {
                return null;
            }

            var digits = Regex.Replace(maskedText, @"\D", string.Empty);
            return digits.Length == 0 ? null : maskedText.Trim();
        }
    }
}
