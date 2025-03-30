using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ReelGrab.Web.Series;

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
    public int? StartYear { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndYear { get; set; } = null;

    [Required]
    public List<AddRequestSeason> Seasons { get; set; } = null!;
}

public class AddRequestSeason
{
    [Required]
    public int Number { get; set; }

    [Required]
    public List<AddRequestEpisode> Episodes { get; set; } = null!;
}

public class AddRequestEpisode
{
    [Required]
    public int? Number { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string ImdbId { get; set; } = null!;

    [Required]
    public bool? Wanted { get; set; } = null!;
}