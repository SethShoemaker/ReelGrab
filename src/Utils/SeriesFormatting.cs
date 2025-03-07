namespace ReelGrab.Utils;

public static class SeriesFormatting
{
    public static string FormatSeason(int season)
    {
        return season < 10 ? $"0{season}" : $"{season}";
    }

    public static string FormatEpisode(int season)
    {
        return season < 10 ? $"0{season}" : $"{season}";
    }
}