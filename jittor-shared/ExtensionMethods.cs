using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MacroEconomics.Shared
{
    public static class ExtensionMethods
    {
        private static readonly string illegalCharacterReplacePattern = @"[^\w]";
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }
        public static string SanitizeString(this string str)
        {
            string sanitizedString = string.Empty;
            if (!string.IsNullOrEmpty(str))
            {
                sanitizedString = Regex.Replace(str.Trim(), illegalCharacterReplacePattern, "-");
                sanitizedString = sanitizedString.Replace("---", "-").Replace("--", "-");
                sanitizedString = sanitizedString.TrimStart('-').TrimEnd('-');
            }

            return sanitizedString;
        }
        public static byte[] Zip(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(this byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return ASCIIEncoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}
