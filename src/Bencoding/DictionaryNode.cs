namespace ReelGrab.Bencoding;

public class DictionaryNode : Node
{
    public List<KeyValuePair> KeyValuePairs { get; init; } = null!;

    public override byte[] Representation { get; init; } = null!;

    public static DictionaryNode Parse(byte[] bytes)
    {
        if (bytes[0] != 'd')
        {
            throw new Exception("could not parse dicionary node");
        }
        List<KeyValuePair> keyValuePairs = [];
        int i = 1;
        while (true)
        {
            if (bytes[i] == 'e')
            {
                break;
            }
            StringNode key = StringNode.Parse(bytes[i..]);
            i += key.Representation.Length;
            Node value;
            if (bytes[i] >= '0' && bytes[i] <= '9')
            {
                value = StringNode.Parse(bytes[i..]);
                i += value.Representation.Length;
                keyValuePairs.Add(new() { Key = key, Value = value });
                continue;
            }
            if (bytes[i] == 'i')
            {
                value = IntegerNode.Parse(bytes[i..]);
                i += value.Representation.Length;
                keyValuePairs.Add(new() { Key = key, Value = value });
                continue;
            }
            if (bytes[i] == 'l')
            {
                value = ListNode.Parse(bytes[i..]);
                i += value.Representation.Length;
                keyValuePairs.Add(new() { Key = key, Value = value });
                continue;
            }
            if (bytes[i] == 'd')
            {
                value = DictionaryNode.Parse(bytes[i..]);
                i += value.Representation.Length;
                keyValuePairs.Add(new() { Key = key, Value = value });
                continue;
            }
            throw new Exception("could not parse dictionary node");
        }
        return new DictionaryNode()
        {
            KeyValuePairs = keyValuePairs,
            Representation = bytes[0..(i + 1)]
        };
    }
}

public class KeyValuePair
{
    public StringNode Key { get; init; } = null!;

    public Node Value { get; init; } = null!;
}