using System.Text.RegularExpressions;

namespace ReelGrab.Bencoding;

public partial class StringNode : Node
{
    public string Value { get; init; } = null!;

    public override string Representation { get; init; } = null!;

    public override int RepresentationLength { get; init; }

    public static StringNode FromString(string str)
    {
        if (!EnsureBeginsWithStringNode().Match(str).Success)
        {
            throw new Exception($"{str} does not start with a string node");
        }
        int colonIndex = str.IndexOf(':');
        int charCount = int.Parse(str[..colonIndex]);
        string value = str[(colonIndex + 1)..(colonIndex + charCount + 1)];
        return new StringNode()
        {
            RepresentationLength = charCount.ToString().Length + 1 + charCount,
            Value = value,
            Representation = charCount.ToString() + ':' + value
        };
    }

    [GeneratedRegex(@"^\d+:", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex EnsureBeginsWithStringNode();
}