namespace ReelGrab.Bencoding;

public class Decoder
{
    public static Document Decode(string bencoded)
    {
        if (char.IsDigit(bencoded[0]))
        {
            return new(StringNode.FromString(bencoded));
        }
        if (bencoded[0] == 'i')
        {
            return new(IntegerNode.FromString(bencoded));
        }
        if (bencoded[0] == 'l')
        {
            return new(ListNode.FromString(bencoded));
        }
        if (bencoded[0] == 'd')
        {
            return new(DictionaryNode.FromString(bencoded));
        }
        throw new Exception("could not decode string");
    }
}