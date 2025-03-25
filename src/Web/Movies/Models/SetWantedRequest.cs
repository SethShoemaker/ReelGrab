using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Movies;

public class SetWantedRequest
{
    [Required]
    public bool? Wanted { get; set; } = null!;
}