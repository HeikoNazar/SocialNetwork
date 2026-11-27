using System.Text.RegularExpressions;

namespace SocialNetwork.Data.Helpers
{
    public static class HashtagParser
    {
        // '#' followed by letters, digits or underscores (any language), not glued to a preceding word
        public static readonly Regex Pattern = new(@"(?<![\p{L}\p{N}_&#])#([\p{L}\p{N}_]{1,50})", RegexOptions.Compiled);

        // Returns the distinct hashtag names found in the text, lowercase and without the '#'
        public static List<string> Extract(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            return Pattern.Matches(text)
                .Select(m => m.Groups[1].Value.ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        // Turns "#Kyiv" or " kyiv " into "kyiv"
        public static string Normalize(string? name)
        {
            return (name ?? string.Empty).Trim().TrimStart('#').ToLowerInvariant();
        }
    }
}