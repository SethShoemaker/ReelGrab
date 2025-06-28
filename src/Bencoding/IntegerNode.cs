namespace ReelGrab.Bencoding;

public class IntegerNode : Node
{
    public long Value { get; init; }

    public override byte[] Representation { get; init; } = null!;

    public static IntegerNode Parse(byte[] bytes)
    {
        if (bytes[0] != 'i')
        {
            throw new Exception("could not parse integer node");
        }
        int eIndex = bytes[1] == '-' ? 2 : 1;
        while (bytes[eIndex] != 'e')
        {
            if (bytes[eIndex] < '0' || bytes[eIndex] > '9')
            {
                throw new Exception("could not parse integer node");
            }
            eIndex++;
        }
        return new IntegerNode()
        {
            Value = long.Parse(bytes.AsSpan()[1..eIndex]),
            Representation = bytes[0..(eIndex + 1)]
        };
    }
}