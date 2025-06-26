namespace ReelGrab.Bencoding;

public abstract class Node
{
    public abstract int RepresentationLength { get; init; }

    public abstract string Representation { get; init; }
}