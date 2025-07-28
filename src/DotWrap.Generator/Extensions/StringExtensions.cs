
namespace DotWrap.Generator.Extensions;

public static class StringExtensions
{
    extension(string s)
    {
        public string? AddOnIfNotNullOrEmpty(string? prefix = null, string? suffix = null)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            return prefix + s + suffix;
        }
    }
}