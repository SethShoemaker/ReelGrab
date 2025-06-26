using System.Text.RegularExpressions;

namespace ReelGrab.Bencoding;

public partial class IntegerNode : Node
{
    public int Value { get; init; }

    public override int RepresentationLength { get; init; }

    public static IntegerNode FromString(string str)
    {
        if (!EnsureIsValidIntegerNode().Match(str).Success)
        {
            throw new Exception($"{str} does not start with an integer node");
        }
        int value = int.Parse(str[1..str.IndexOf('e')]);
        return new IntegerNode()
        {
            Value = value,
            RepresentationLength = value.ToString().Length + 2
        };
    }

    [GeneratedRegex(@"^i(0|-[1-9][0-9]*|[1-9][0-9]*)e", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex EnsureIsValidIntegerNode();
}