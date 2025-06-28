namespace ReelGrab.Bencoding;

public class ListNode : Node
{
    public List<Node> Elements { get; init; } = null!;

    public override byte[] Representation { get; init; } = null!;

    public static ListNode Parse(byte[] bytes)
    {
        if (bytes[0] != 'l')
        {
            throw new Exception("could not parse list node");
        }
        List<Node> elements = [];
        int i = 1;
        while (true)
        {
            if (bytes[i] == 'e')
            {
                break;
            }
            if (bytes[i] >= '0' && bytes[i] <= '9')
            {
                var node = StringNode.Parse(bytes[i..]);
                i += node.Representation.Length;
                elements.Add(node);
                continue;
            }
            if (bytes[i] == 'i')
            {
                var node = IntegerNode.Parse(bytes[i..]);
                i += node.Representation.Length;
                elements.Add(node);
                continue;
            }
            if (bytes[i] == 'l')
            {
                var node = ListNode.Parse(bytes[i..]);
                i += node.Representation.Length;
                elements.Add(node);
                continue;
            }
            if (bytes[i] == 'd')
            {
                var node = DictionaryNode.Parse(bytes[i..]);
                i += node.Representation.Length;
                elements.Add(node);
                continue;
            }
            throw new Exception("could not parse list node");
        }
        return new ListNode()
        {
            Elements = elements,
            Representation = bytes[0..(i + 1)]
        };
    }
}