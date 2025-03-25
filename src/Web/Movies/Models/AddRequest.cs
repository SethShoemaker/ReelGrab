using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ReelGrab.Web.Movies;

public class AddRequest
{
    [Required]
    public string ImdbId { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Poster { get; set; } = null;

    [Required]
    public int? Year { get; set; } = null!;

    [Required]
    public bool? Wanted { get; set; } = null!;
}