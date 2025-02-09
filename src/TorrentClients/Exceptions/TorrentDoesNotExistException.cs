namespace ReelGrab.TorrentClients.Exceptions;

public class TorrentDoesNotExistException : TorrentException
{
    public TorrentDoesNotExistException(string which)
    {
        Which = which;
    }

    private string Which;

    public override string Message => $"could not find torrent {Which}"; 
}