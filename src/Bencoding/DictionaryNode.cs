namespace ReelGrab.Bencoding;

public class DictionaryNode : Node
{
    public List<DictionaryNodeElement> Elements { get; init; } = null!;

    public override int RepresentationLength { get; init; }

    public override string Representation { get; init; } = null!;

    public static DictionaryNode FromString(string str)
    {
        if (!str.StartsWith('d'))
        {
            throw new Exception($"{str} does not start with a dictionary node");
        }
        List<DictionaryNodeElement> elements = [];
        for (int i = 1; true;)
        {
            if (str[i] == 'e')
            {
                break;
            }
            StringNode key = StringNode.FromString(str[i..]);
            i += key.RepresentationLength;
            Node value;
            if (char.IsDigit(str[i]))
            {
                value = StringNode.FromString(str[i..]);
                i += value.RepresentationLength;
                elements.Add(new() { Key = key, Value = value });
                continue;
            }
            if (str[i] == 'i')
            {
                value = IntegerNode.FromString(str[i..]);
                i += value.RepresentationLength;
                elements.Add(new() { Key = key, Value = value });
                continue;
            }
            if (str[i] == 'l')
            {
                value = ListNode.FromString(str[i..]);
                i += value.RepresentationLength;
                elements.Add(new() { Key = key, Value = value });
                continue;
            }
            if (str[i] == 'd')
            {
                value = DictionaryNode.FromString(str[i..]);
                i += value.RepresentationLength;
                elements.Add(new() { Key = key, Value = value });
                continue;
            }
            throw new Exception($"{str} does not start with a dictionary node");
        }
        return new DictionaryNode()
        {
            Elements = elements,
            RepresentationLength = elements.Select(e => e.Key).Sum(e => e.RepresentationLength) + elements.Select(e => e.Value).Sum(e => e.RepresentationLength) + 2,
            Representation = 'd' + string.Concat(elements.Select(e => e.Key.Representation + e.Value.Representation)) + 'e'
        };
    }
}