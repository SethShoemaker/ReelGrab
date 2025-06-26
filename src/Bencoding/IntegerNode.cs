using System.Text.RegularExpressions;

namespace ReelGrab.Bencoding;

public partial class IntegerNode : Node
{
    public long Value { get; init; }

    public override int RepresentationLength { get; init; }

    public override string Representation { get; init; } = null!;

    public static IntegerNode FromString(string str)
    {
        if (!EnsureIsValidIntegerNode().Match(str).Success)
        {
            throw new Exception($"{str} does not start with an integer node");
        }
        long value = long.Parse(str[1..str.IndexOf('e')]);
        return new IntegerNode()
        {
            Value = value,
            RepresentationLength = value.ToString().Length + 2,
            Representation = 'i' + value.ToString() + 'e'
        };
    }

    [GeneratedRegex(@"^i(0|-[1-9][0-9]*|[1-9][0-9]*)e", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex EnsureIsValidIntegerNode();
}