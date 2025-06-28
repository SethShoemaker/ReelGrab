using System.Text;

namespace ReelGrab.Bencoding;

public class StringNode : Node
{
    public byte[] Value { get; init; } = null!;

    public string ValueString { get; init; } = null!;

    public override byte[] Representation { get; init; } = null!;

    public static StringNode Parse(byte[] bytes)
    {
        if (bytes[0] < '0' || bytes[0] > '9')
        {
            throw new Exception("could not parse string node");
        }
        int colonIndex;
        for (int i = 1; true; i++)
        {
            if (bytes[i] >= '0' && bytes[i] <= '9')
            {
                continue;
            }
            if (bytes[i] == ':')
            {
                colonIndex = i;
                break;
            }
            throw new Exception("could not parse string node");
        }
        int charCount = int.Parse(bytes.AsSpan()[..colonIndex]);
        byte[] representation = bytes[0..(colonIndex + charCount + 1)];
        byte[] value = bytes[(colonIndex + 1)..(colonIndex + charCount + 1)];
        return new StringNode()
        {
            Value = value,
            ValueString = Encoding.UTF8.GetString(value),
            Representation = representation,
        };
    }
}