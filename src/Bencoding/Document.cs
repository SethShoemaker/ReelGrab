using System.Text;

namespace ReelGrab.Bencoding;

public class Document
{
    public Node Root { get; init; } = null!;

    public Document(Node root)
    {
        Root = root;
    }

    public static Document Parse(byte[] bytes)
    {
        if (bytes[0] >= '0' && bytes[0] <= '9')
        {
            return new(StringNode.Parse(bytes));
        }
        if (bytes[0] == 'i')
        {
            return new(IntegerNode.Parse(bytes));
        }
        if (bytes[0] == 'l')
        {
            return new(ListNode.Parse(bytes));
        }
        if (bytes[0] == 'd')
        {
            return new(DictionaryNode.Parse(bytes));
        }
        throw new Exception("could not parse list node");
    }

    public static async Task<Document> FromFileAsync(string filePath)
    {
        return Parse(await File.ReadAllBytesAsync(filePath));
    }

    public string ToDebugString()
    {
        var sb = new StringBuilder();
        AppendNodeDebugString(sb, Root, 0);
        return sb.ToString();
    }

    private static void AppendNodeDebugString(StringBuilder sb, Node node, int indent)
    {
        var indentStr = new string(' ', indent * 2);

        switch (node)
        {
            case StringNode s:
                sb.AppendLine($"{indentStr}StringNode: \"{s.ValueString}\"");
                break;

            case IntegerNode i:
                sb.AppendLine($"{indentStr}IntegerNode: {i.Value}");
                break;

            case ListNode l:
                sb.AppendLine($"{indentStr}ListNode:");
                foreach (var item in l.Elements)
                {
                    AppendNodeDebugString(sb, item, indent + 1);
                }
                break;

            case DictionaryNode d:
                sb.AppendLine($"{indentStr}DictionaryNode:");
                foreach (var pair in d.KeyValuePairs)
                {
                    sb.AppendLine($"{new string(' ', (indent + 1) * 2)}Key: \"{pair.Key.ValueString}\"");
                    AppendNodeDebugString(sb, pair.Value, indent + 2);
                }
                break;

            default:
                sb.AppendLine($"{indentStr}Unknown node");
                break;
        }
    }
}