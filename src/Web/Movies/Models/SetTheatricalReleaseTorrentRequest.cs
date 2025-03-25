using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Movies;

public class SetTheatricalReleaseTorrentRequest
{

    [Required]
    public string TorrentUrl { get; set; } = null!;

    [Required]
    public string TorrentSource { get; set; } = null!;

    [Required]
    public string TorrentFilePath { get; set; } = null!;
}