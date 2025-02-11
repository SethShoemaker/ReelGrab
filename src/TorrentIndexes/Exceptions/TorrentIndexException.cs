namespace ReelGrab.TorrentIndexes.Exceptions;

public class TorrentIndexException : Exception
{
    public TorrentIndexException() { }

    public TorrentIndexException(string? message) : base(message) { }
}