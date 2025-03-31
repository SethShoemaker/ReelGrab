using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Series;

public class UpdateEpisodesRequest
{
    [Required]
    public List<UpdateEpisodesRequestSeason> Seasons { get; set; } = null!;
}

public class UpdateEpisodesRequestSeason
{
    [Required]
    public int Number { get; set; }

    [Required]
    public List<UpdateEpisodesRequestEpisode> Episodes { get; set; } = null!;
}

public class UpdateEpisodesRequestEpisode
{
    [Required]
    public int Number { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string ImdbId { get; set; } = null!;
}