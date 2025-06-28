namespace tests.Bencoding;

using ReelGrab.Bencoding;

public class IntegerNodeTest
{
    [Fact]
    public void ParsesSingleDigitIntegerByItself()
    {
        byte[] input = [(byte)'i', (byte)'1', (byte)'e'];
        var node = IntegerNode.Parse(input);
        Assert.Equal(1, node.Value);
        Assert.Equal(3, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }

    [Fact]
    public void ParsesSingleDigitIntegerProceededWithMore()
    {
        byte[] input = [(byte)'i', (byte)'1', (byte)'e', (byte)'e'];
        var node = IntegerNode.Parse(input);
        Assert.Equal(1, node.Value);
        Assert.Equal(3, node.Representation.Length);
        Assert.Equal([(byte)'i', (byte)'1', (byte)'e'], node.Representation);
    }

    [Fact]
    public void ParsesZeroByItself()
    {
        byte[] input = [(byte)'i', (byte)'0', (byte)'e'];
        var node = IntegerNode.Parse(input);
        Assert.Equal(0, node.Value);
        Assert.Equal(3, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }

    [Fact]
    public void ParsesNegativeByItself()
    {
        byte[] input = [(byte)'i', (byte)'-', (byte)'1', (byte)'2', (byte)'e'];
        var node = IntegerNode.Parse(input);
        Assert.Equal(-12, node.Value);
        Assert.Equal(5, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }

    [Fact]
    public void ParsesNegativeProceededWithMore()
    {
        byte[] input = [(byte)'i', (byte)'-', (byte)'1', (byte)'2', (byte)'e', (byte)'e'];
        var node = IntegerNode.Parse(input);
        Assert.Equal(-12, node.Value);
        Assert.Equal(5, node.Representation.Length);
        Assert.Equal([(byte)'i', (byte)'-', (byte)'1', (byte)'2', (byte)'e'], node.Representation);
    }

    [Fact]
    public void FailsWhenGivenLetter()
    {
        Assert.Throws<Exception>(() => IntegerNode.Parse([(byte)'i', (byte)'a', (byte)'1', (byte)'2', (byte)'e', (byte)'e']));
    }
}