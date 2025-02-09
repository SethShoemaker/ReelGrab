namespace ReelGrab.TorrentClients.Exceptions;

public class TorrentException : Exception
{
    public TorrentException() { }

    public TorrentException(string? message) : base(message) { }
}