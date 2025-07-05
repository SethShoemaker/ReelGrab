using System.Web;
using ReelGrab.Utils;

namespace ReelGrab.Torrent;

public class MagnetLink
{
    public string Uri { get; init; } = null!;

    public string InfoHash { get; init; } = null!;

    public string? DisplayName { get; init; }

    public string[] TrackerUrls { get; init; } = null!;

    public static MagnetLink FromUri(string uri)
    {
        return new MagnetLink()
        {
            Uri = uri,
            InfoHash = GetInfoHash(uri),
            DisplayName = GetDisplayNameIfPresent(uri),
            TrackerUrls = GetTrackerUrls(uri)
        };
    }

    public static string GetInfoHash(string uri)
    {
        int start = uri.IndexOfAny("?xt=urn:btih:", "&xt=urn:btih:");
        if (start == -1)
        {
            throw new Exception("could not find info hash");
        }
        start += 13;
        int end = uri.IndexOf('&', start);
        if (end == -1)
        {
            end = uri.Length;
        }
        string hash = uri[start..end];
        if (hash.Length == 0)
        {
            throw new Exception("info hash was empty");
        }
        if (hash.Length != 40)
        {
            throw new Exception($"extracted info hash has length ${hash.Length}, not 40. This is likely becuase it is base32 encoded, which this client does not currently support");
        }
        return hash;
    }

    public static string? GetDisplayNameIfPresent(string uri)
    {
        int start = uri.IndexOfAny("?dn=", "&dn=");
        if (start == -1)
        {
            return null;
        }
        start += 4;
        int end = uri.IndexOf('&', start);
        if (end == -1)
        {
            end = uri.Length;
        }
        string displayName = uri[start..end];
        if (displayName.Length == 0)
        {
            throw new Exception("display name field was included, but empty");
        }
        return displayName;
    }

    public static string[] GetTrackerUrls(string uri)
    {
        List<string> trackerUrls = [];
        for (int i = 0; i < uri.Length;)
        {
            int start = uri.IndexOfAny(i, "?tr=", "&tr=");
            if (start == -1)
            {
                break;
            }
            start += 4;
            i = uri.IndexOf('&', start);
            if (i == -1)
            {
                i = uri.Length;
            }
            trackerUrls.Add(HttpUtility.UrlDecode(uri[start..i]));
        }
        return trackerUrls.ToArray();
    }
}