using System.Security.Cryptography;
using ReelGrab.Bencoding;

namespace ReelGrab.Torrent;

public class InfoDictionary
{
    public InfoDictionaryType Type { get; init; }

    public string InfoHash { get; init; } = null!;

    public byte[] InfoBytes { get; init; } = null!;

    public string Name { get; init; } = null!;

    public long PieceLength { get; init; }

    public byte[] Pieces { get; init; } = null!;

    public string[] PieceHashes { get; init; } = null!;

    public long TotalSize => Type switch
    {
        InfoDictionaryType.SINGLE_FILE => Length,
        InfoDictionaryType.MULTI_FILE => Files.Sum(e => e.Length),
        _ => throw new NotImplementedException()
    };

    public long Length { get; init; }

    public List<InfoDictionaryFile> Files { get; init; } = null!;

    public static InfoDictionary FromDocument(Document document)
    {
        var type = GetTypeFromDocument(document);
        return new InfoDictionary()
        {
            Type = type,
            InfoHash = GetInfoHashFromDocument(document),
            InfoBytes = GetInfoBytesFromDocument(document),
            Name = GetNameFromDocument(document),
            PieceLength = GetPieceLengthFromDocument(document),
            Pieces = GetPiecesFromDocument(document),
            PieceHashes = GetPieceHashesFromDocument(document),
            Length = type == InfoDictionaryType.SINGLE_FILE ? GetLengthFromDocument(document) : default,
            Files = type == InfoDictionaryType.MULTI_FILE ? GetFilesFromDocument(document) : default!
        };
    }

    public static InfoDictionaryType GetTypeFromDocument(Document document)
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
            return InfoDictionaryType.SINGLE_FILE;
        }
        if (!hasLengthKey && hasFilesKey)
        {
            return InfoDictionaryType.MULTI_FILE;
        }
        throw new Exception("could not determine torrent file type");
    }

    public static string GetNameFromDocument(Document document)
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

    public static long GetPieceLengthFromDocument(Document document)
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

    public static byte[] GetPiecesFromDocument(Document document)
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

    public static string[] GetPieceHashesFromDocument(Document document)
    {
        var pieces = GetPiecesFromDocument(document);
        if (pieces.Length % 20 != 0)
        {
            throw new Exception("pieces string length must be a multiple of 20");
        }
        string[] hashes = new string[pieces.Length / 20];
        for (int i = 0; i < pieces.Length; i += 20)
        {
            hashes[i / 20] = BitConverter.ToString(pieces[i..(i + 20)]).Replace("-", "").ToLowerInvariant();
        }
        return hashes;
    }

    public static string GetInfoHashFromDocument(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info") ?? throw new Exception("root dictionary does not have info key");
        byte[] infoBytes = infoElement.Value.Representation;
        return BitConverter.ToString(SHA1.HashData(infoBytes)).Replace("-", "").ToLowerInvariant();
    }

    public static byte[] GetInfoBytesFromDocument(Document document)
    {
        if (document.Root is not DictionaryNode rootDict)
        {
            throw new Exception("root node is not a dictionary");
        }
        var infoElement = rootDict.KeyValuePairs.FirstOrDefault(kv => kv.Key.ValueString == "info")?.Value ?? throw new Exception("root dictionary does not have info key");
        return infoElement.Representation;
    }

    public static long GetLengthFromDocument(Document document)
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

    public static List<InfoDictionaryFile> GetFilesFromDocument(Document document)
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
        List<InfoDictionaryFile> entries = [];
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
            entries.Add(new InfoDictionaryFile()
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

        sb.AppendLine($"InfoDictionary Debug Info:");
        sb.AppendLine($"  Type: {Type}");
        sb.AppendLine($"  Name: {Name}");
        sb.AppendLine($"  InfoHash: {InfoHash}");
        sb.AppendLine($"  PieceLength: {PieceLength}");
        sb.AppendLine($"  Total Length: {Length}");

        sb.AppendLine($"  Pieces:");
        sb.AppendLine($"    Total Pieces: {Pieces.Length / 20}");
        sb.AppendLine($"    Hashes:");
        for (int i = 0; i < PieceHashes.Length; i++)
        {
            sb.AppendLine($"      - {i}: {PieceHashes[i]}");
        }

        if (Type == InfoDictionaryType.MULTI_FILE)
        {
            sb.AppendLine($"  Files:");
            foreach (var file in Files)
            {
                sb.AppendLine($"    - Path: {string.Join("/", file.PathComponents)}");
                sb.AppendLine($"      Length: {file.Length}");
            }
        }

        return sb.ToString();
    }
}

public enum InfoDictionaryType : byte
{
    SINGLE_FILE = 0x00,
    MULTI_FILE = 0xFF
}

public class InfoDictionaryFile
{
    public List<string> PathComponents { get; init; } = null!;

    public long Length { get; init; }
}