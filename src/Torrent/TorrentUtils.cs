using System.Security.Cryptography;
using ReelGrab.Bencoding;

namespace ReelGrab.Torrent;

public static class TorrentUtils
{
    public static string GetInfoHash(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("Top-level node is not a dictionary");
        }

        var infoElement = rootDict.KeyValuePairs
            .FirstOrDefault(kv => kv.Key.ValueString == "info");

        if (infoElement == null)
        {
            throw new Exception("No 'info' key found in the document");
        }

        byte[] infoBytes = infoElement.Value.Representation;
        return BitConverter.ToString(SHA1.HashData(infoBytes)).Replace("-", "").ToLowerInvariant();
    }
}