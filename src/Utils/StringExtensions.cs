namespace ReelGrab.Utils;

public static class StringExtensions
{
    public static bool ContainsMoreThanOnce(this string str, string value)
    {
        var first = str.IndexOf(value);
        return first != -1 && first != str.LastIndexOf(value);
    }

    public static int IndexOfAny(this string str, params string[] values)
    {
        int min = -1;
        for (int i = 0; i < values.Length; i++)
        {
            int pos = str.IndexOf(values[i]);
            if (pos != -1 && (min == -1 || pos < min))
            {
                min = pos;
            }
        }
        return min;
    }

    public static int IndexOfAny(this string str, int start, params string[] values)
    {
        int min = -1;
        for (int i = 0; i < values.Length; i++)
        {
            int pos = str.IndexOf(values[i], start);
            if (pos != -1 && (min == -1 || pos < min))
            {
                min = pos;
            }
        }
        return min;
    }
}