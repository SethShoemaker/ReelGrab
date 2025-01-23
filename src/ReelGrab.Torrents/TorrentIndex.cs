public partial class TorrentIndex
{
    private TorrentIndex(){}

    public static readonly TorrentIndex instance = new TorrentIndex();

    public string? ApiUrl;

    public string? ApiKey;
}