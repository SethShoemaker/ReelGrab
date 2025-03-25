using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Torrents.Models;

public class AddRequest
{
    [Required]
    public string Url { get; set; } = null!;

    [Required]
    public string Source { get; set; } = null!;
}