using ReelGrab.Bencoding;

namespace ReelGrab.Torrent;

public class TorrentFile
{
    public Document Document { get; init; } = null!;

    public List<string> TrackerUrls { get; init; } = null!;

    public InfoDictionary InfoDictionary { get; init; } = null!;

    public static TorrentFile FromDocument(Document document)
    {
        return new TorrentFile()
        {
            Document = document,
            TrackerUrls = GetTrackerUrls(document),
            InfoDictionary = InfoDictionary.FromDocument(document)
        };
    }

    public static List<string> GetTrackerUrls(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        List<string> trackerUrls = [];
        if (rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "announce")?.Value is StringNode announceString)
        {
            trackerUrls.Add(announceString.ValueString);
        }
        if (rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "announce-list")?.Value is ListNode announceList)
        {
            List<Node> toSearch = [announceList];
            while (toSearch.Count != 0)
            {
                if (toSearch.First() is ListNode listNode)
                {
                    toSearch.AddRange(listNode.Elements);
                }
                else if (toSearch.First() is StringNode stringNode)
                {
                    trackerUrls.Add(stringNode.ValueString);
                }
                toSearch.RemoveAt(0);
            }
        }
        return trackerUrls;
    }

    public string ToDebugString()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"TorrentFile Debug Info:");

        sb.AppendLine($"  Tracker URLs:");
        foreach (var url in TrackerUrls)
        {
            sb.AppendLine($"    - {url}");
        }

        sb.AppendLine(InfoDictionary.ToDebugString());

        return sb.ToString();
    }
}