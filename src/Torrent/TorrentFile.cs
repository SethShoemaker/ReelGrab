using System.Security.Cryptography;
using ReelGrab.Bencoding;

namespace ReelGrab.Torrent;

public class TorrentFile
{
    public Document Document { get; init; } = null!;

    public TorrentFileType Type { get; init; }

    public string InfoHash { get; init; } = null!;

    public byte[] InfoBytes { get; init; } = null!;

    public List<string> TrackerUrls { get; init; } = null!;

    public string Name { get; init; } = null!;

    public long PieceLength { get; init; }

    public byte[] Pieces { get; init; } = null!;

    public string[] PieceHashes { get; init; } = null!;

    public long TotalSize => Type switch
    {
        TorrentFileType.SINGLE_FILE => Length,
        TorrentFileType.MULTI_FILE => Entries.Sum(e => e.Length),
        _ => throw new NotImplementedException()
    };

    public long Length { get; init; }

    public List<TorrentFileEntry> Entries { get; init; } = null!;

    public async static Task<TorrentFile> FromFileAsync(string filePath)
    {
        var document = await Document.FromFileAsync(filePath);
        var type = GetType(document);
        return new TorrentFile()
        {
            Document = document,
            Type = type,
            InfoHash = GetInfoHash(document),
            InfoBytes = GetInfoBytes(document),
            TrackerUrls = GetTrackerUrls(document),
            Name = GetName(document),
            PieceLength = GetPieceLength(document),
            Pieces = GetPieces(document),
            PieceHashes = GetPieceHashes(document),
            Length = type == TorrentFileType.SINGLE_FILE ? GetLength(document) : default,
            Entries = type == TorrentFileType.MULTI_FILE ? GetEntries(document) : default!
        };
    }

    public static TorrentFileType GetType(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        bool hasLengthKey = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "length") != null;
        bool hasFilesKey = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "files") != null;
        if (hasLengthKey && !hasFilesKey)
        {
            return TorrentFileType.SINGLE_FILE;
        }
        if (!hasLengthKey && hasFilesKey)
        {
            return TorrentFileType.MULTI_FILE;
        }
        throw new Exception("could not determine torrent file type");
    }

    public static string GetName(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        var nameElement = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "name")?.Value ?? throw new Exception("info dictionary does not have name key");
        if (nameElement is not StringNode nameString)
        {
            throw new Exception("name value is not a string node");
        }
        return nameString.ValueString;
    }

    public static long GetPieceLength(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        var pieceLengthElement = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "piece length")?.Value ?? throw new Exception("info dictionary does not have piece length key");
        if (pieceLengthElement is not IntegerNode pieceLengthInt)
        {
            throw new Exception("piece length value is not a string node");
        }
        return pieceLengthInt.Value;
    }

    public static byte[] GetPieces(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        var piecesElement = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "pieces")?.Value ?? throw new Exception("info dictionary does not have pieces key");
        if (piecesElement is not StringNode piecesString)
        {
            throw new Exception("pieces value is not a string node");
        }
        return piecesString.Value;
    }

    public static string[] GetPieceHashes(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        var piecesElement = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "pieces")?.Value ?? throw new Exception("info dictionary does not have pieces key");
        if (piecesElement is not StringNode piecesString)
        {
            throw new Exception("pieces value is not a string node");
        }
        if (piecesString.Value.Length % 20 != 0)
        {
            throw new Exception("pieces string length must be a multiple of 20");
        }
        string[] hashes = new string[piecesString.Value.Length / 20];
        for (int i = 0; i < piecesString.Value.Length; i += 20)
        {
            hashes[i / 20] = BitConverter.ToString(piecesString.Value[i..(i + 20)]).Replace("-", "").ToLowerInvariant();
        }
        return hashes;
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
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info") ?? throw new Exception("root dictionary does not have info key");
        byte[] infoBytes = infoElement.Value.Representation;
        return BitConverter.ToString(SHA1.HashData(infoBytes)).Replace("-", "").ToLowerInvariant();
    }

    public static byte[] GetInfoBytes(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        return infoElement.Representation;
    }

    public static long GetLength(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        var lengthElement = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "length")?.Value ?? throw new Exception("info dict does not have length key");
        if (lengthElement is not IntegerNode lengthInteger)
        {
            throw new Exception("length value is not an integer node");
        }
        return lengthInteger.Value;
    }

    public static List<TorrentFileEntry> GetEntries(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        if (infoElement is not DictionaryNode infoDict)
        {
            throw new Exception("info value is not a dictionary node");
        }
        var filesElement = infoDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "files")?.Value ?? throw new Exception("info dictionary does not have files key");
        if (filesElement is not ListNode filesList)
        {
            throw new Exception("files value is not a list node");
        }
        List<TorrentFileEntry> entries = [];
        foreach (var fileElement in filesList.Elements)
        {
            if (fileElement is not DictionaryNode fileDict)
            {
                throw new Exception("all elements inside the files list must be dictionary nodes");
            }
            var lengthElement = fileDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "length")?.Value ?? throw new Exception("all dictionaries inside the files list must have a length key");
            if (lengthElement is not IntegerNode lengthInteger)
            {
                throw new Exception("all length values inside the file dictionaries must be integer nodes");
            }
            long length = lengthInteger.Value;
            var pathElement = fileDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "path")?.Value ?? throw new Exception("all dictionaries inside the files list must have a path key");
            if (pathElement is not ListNode pathList)
            {
                throw new Exception("all path values inside the file dictionaries must be list nodes");
            }
            List<string> pathComponents = [];
            foreach (var pathComponentElement in pathList.Elements)
            {
                if (pathComponentElement is not StringNode pathComponentString)
                {
                    throw new Exception("all path elements must be string nodes");
                }
                pathComponents.Add(pathComponentString.ValueString);
            }
            entries.Add(new TorrentFileEntry()
            {
                PathComponents = pathComponents,
                Length = length
            });
        }
        return entries;
    }

    public string ToDebugString()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"TorrentFile Debug Info:");
        sb.AppendLine($"  Type: {Type}");
        sb.AppendLine($"  Name: {Name}");
        sb.AppendLine($"  InfoHash: {InfoHash}");
        sb.AppendLine($"  PieceLength: {PieceLength}");
        sb.AppendLine($"  Total Length: {Length}");

        sb.AppendLine($"  Tracker URLs:");
        foreach (var url in TrackerUrls)
        {
            sb.AppendLine($"    - {url}");
        }

        sb.AppendLine($"  Pieces:");
        sb.AppendLine($"    Total Pieces: {Pieces.Length / 20}");
        sb.AppendLine($"    Hashes:");
        for (int i = 0; i < PieceHashes.Length; i++)
        {
            sb.AppendLine($"      - {i}: {PieceHashes[i]}");
        }

        if (Type == TorrentFileType.MULTI_FILE)
        {
            sb.AppendLine($"  Entries:");
            foreach (var entry in Entries)
            {
                sb.AppendLine($"    - Path: {string.Join("/", entry.PathComponents)}");
                sb.AppendLine($"      Length: {entry.Length}");
            }
        }

        return sb.ToString();
    }
}

public enum TorrentFileType : short
{
    SINGLE_FILE = 0,
    MULTI_FILE = 1
}

public class TorrentFileEntry
{
    public List<string> PathComponents { get; init; } = null!;

    public long Length { get; init; }
}