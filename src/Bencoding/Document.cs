using System.Text;

namespace ReelGrab.Bencoding;

public class Document
{
    public Node Root { get; init; } = null!;

    public Document(Node root)
    {
        Root = root;
    }

    public static Document FromString(string str)
    {
        if (char.IsDigit(str[0]))
        {
            return new(StringNode.FromString(str));
        }
        if (str[0] == 'i')
        {
            return new(IntegerNode.FromString(str));
        }
        if (str[0] == 'l')
        {
            return new(ListNode.FromString(str));
        }
        if (str[0] == 'd')
        {
            return new(DictionaryNode.FromString(str));
        }
        throw new Exception("could not decode string");
    }

    public static async Task<Document> FromFileAsync(string filePath)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        return FromString(Encoding.ASCII.GetString(fileBytes));
    }

    public string ToDebugString()
    {
        return ToDebugString(Root, 0);
    }

    private static string ToDebugString(Node node, int indent)
    {
        var indentStr = new string(' ', indent * 2);

        return node switch
        {
            StringNode s => $"{indentStr}StringNode: \"{s.Value}\"",
            IntegerNode i => $"{indentStr}IntegerNode: {i.Value}",
            ListNode l => $"{indentStr}ListNode:\n" + string.Join("\n", l.Elements.Select(e => ToDebugString(e, indent + 1))),
            DictionaryNode d => $"{indentStr}DictionaryNode:\n" + string.Join("\n", d.Elements.Select(e =>
                $"{new string(' ', (indent + 1) * 2)}Key: \"{e.Key.Value}\"\n{ToDebugString(e.Value, indent + 2)}")),
            _ => $"{indentStr}Unknown node"
        };
    }
}