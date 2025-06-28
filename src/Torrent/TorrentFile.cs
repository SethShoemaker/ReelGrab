using System.Security.Cryptography;
using ReelGrab.Bencoding;

namespace ReelGrab.Torrent;

public class TorrentFile
{
    public Document Document { get; init; } = null!;

    public string InfoHash { get; init; } = null!;

    public List<string> TrackerUrls { get; init; } = null!;

    public async static Task<TorrentFile> FromFileAsync(string filePath)
    {
        var document = await Document.FromFileAsync(filePath);
        return new TorrentFile()
        {
            Document = document,
            InfoHash = GetInfoHash(document),
            TrackerUrls = GetTrackerUrls(document)
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

    public static string GetInfoHash(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info") ?? throw new Exception("No 'info' key found in the document");
        byte[] infoBytes = infoElement.Value.Representation;
        return BitConverter.ToString(SHA1.HashData(infoBytes)).Replace("-", "").ToLowerInvariant();
    }
}
