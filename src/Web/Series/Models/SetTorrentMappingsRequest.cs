using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Series;

public class SetTorrentMappingsRequest
{
    [Required]
    public List<SetTorrentMappingsRequestTorrent> Torrents { get; set; } = null!;
}

public class SetTorrentMappingsRequestTorrent
{
    [Required]
    public string Url { get; set; } = null!;

    public List<SetTorrentMappingsRequestMapping> Mappings { get; set; } = null!;
}

public class SetTorrentMappingsRequestMapping
{
    [Required]
    public string Path { get; set; } = null!;

    [Required]
    public string ImdbId { get; set; } = null!;
}