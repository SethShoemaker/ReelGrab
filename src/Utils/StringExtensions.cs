namespace ReelGrab.Utils;

public static class StringExtensions
{
    public static bool ContainsMoreThanOnce(this string str, string value)
    {
        var first = str.IndexOf(value);
        return first != -1 && first != str.LastIndexOf(value);
    }
}