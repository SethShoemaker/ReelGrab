namespace ReelGrab.Bencoding;

public class ListNode : Node
{
    public List<Node> Elements { get; init; } = null!;

    public override int RepresentationLength { get; init; }

    public static ListNode FromString(string str)
    {
        if (!str.StartsWith('l'))
        {
            throw new Exception($"{str} does not start with a list node");
        }
        List<Node> elements = [];
        for (int i = 1; true;)
        {
            if (str[i] == 'e')
            {
                break;
            }
            if (char.IsDigit(str[i]))
            {
                var node = StringNode.FromString(str[i..]);
                i += node.RepresentationLength;
                elements.Add(node);
                continue;
            }
            if (str[i] == 'i')
            {
                var node = IntegerNode.FromString(str[i..]);
                i += node.RepresentationLength;
                elements.Add(node);
                continue;
            }
            if (str[i] == 'l')
            {
                var node = ListNode.FromString(str[i..]);
                i += node.RepresentationLength;
                elements.Add(node);
                continue;
            }
            if (str[i] == 'd')
            {
                var node = DictionaryNode.FromString(str[i..]);
                i += node.RepresentationLength;
                elements.Add(node);
                continue;
            }
            throw new Exception("error while parsing list");
        }
        return new ListNode()
        {
            Elements = elements,
            RepresentationLength = elements.Sum(n => n.RepresentationLength) + 2
        };
    }
}