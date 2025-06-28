namespace tests.Bencoding;

using ReelGrab.Bencoding;
using System.Text;

public class StringNodeTest
{
    [Fact]
    public void ProcessesSingleByteCharacterTextByItself()
    {
        byte[] input = [(byte)'4', (byte)':', (byte)'s', (byte)'p', (byte)'a', (byte)'m'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [(byte)'s', (byte)'p', (byte)'a', (byte)'m'];
        byte[] expectedRepresentation = input;

        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(6, node.Representation.Length);
        Assert.Equal(expectedRepresentation, node.Representation);
    }

    [Fact]
    public void ProcessesMultiByteCharacterTextByItself()
    {
        byte[] input = [(byte)'8', (byte)':', 0xF0, 0x9F, 0x98, 0x80, 0xC2, 0xAE, 0xCE, 0xA0];
        var node = StringNode.Parse(input);

        byte[] expectedValue = Encoding.UTF8.GetBytes("😀®Π");

        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(10, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }

    [Fact]
    public void ProcessesLongTextByItself()
    {
        byte[] input = [(byte)'1', (byte)'0', (byte)':', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [(byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j'];
        byte[] expectedRepresentation = input;

        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(13, node.Representation.Length);
        Assert.Equal(expectedRepresentation, node.Representation);
    }

    [Fact]
    public void ProcessesBinaryDataByItself()
    {
        byte[] input = [(byte)'4', (byte)':', 0xFF, 0xFE, 0xFD, 0xFC];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [0xFF, 0xFE, 0xFD, 0xFC];
        byte[] expectedRepresentation = input;

        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(6, node.Representation.Length);
        Assert.Equal(expectedRepresentation, node.Representation);
    }

    [Fact]
    public void ProcessesTextProceededByMore()
    {
        byte[] input = [(byte)'4', (byte)':', (byte)'s', (byte)'p', (byte)'a', (byte)'m', (byte)'i', (byte)'2', (byte)'e'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [(byte)'s', (byte)'p', (byte)'a', (byte)'m'];
        byte[] expectedRepresentation = [(byte)'4', (byte)':', (byte)'s', (byte)'p', (byte)'a', (byte)'m'];

        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(6, node.Representation.Length);
        Assert.Equal(expectedRepresentation, node.Representation);
    }

    [Fact]
    public void ProcessesBinaryDataProceededByMore()
    {
        byte[] input = [(byte)'4', (byte)':', 0xFF, 0xFE, 0xFD, 0xFC, (byte)'i', (byte)'2', (byte)'e'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [0xFF, 0xFE, 0xFD, 0xFC];
        byte[] expectedRepresentation = [(byte)'4', (byte)':', 0xFF, 0xFE, 0xFD, 0xFC];

        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(6, node.Representation.Length);
        Assert.Equal(expectedRepresentation, node.Representation);
    }

    [Fact]
    public void ProcessesEmptyString()
    {
        byte[] input = [(byte)'0', (byte)':'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [];
        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(input.Length, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }

    [Fact]
    public void ProcessesStringWithSymbols()
    {
        byte[] input = [(byte)'4', (byte)':', (byte)'$', (byte)'@', (byte)'#', (byte)'%'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [(byte)'$', (byte)'@', (byte)'#', (byte)'%'];
        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(input.Length, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }

    [Fact]
    public void ThrowsOnMissingColon()
    {
        byte[] input = [(byte)'3', (byte)'f', (byte)'o', (byte)'o'];

        Assert.Throws<Exception>(() => StringNode.Parse(input));
    }

    [Fact]
    public void ThrowsOnInvalidLengthPrefix()
    {
        byte[] input = [(byte)'a', (byte)'3', (byte)':', (byte)'x', (byte)'y', (byte)'z'];

        Assert.Throws<Exception>(() => StringNode.Parse(input));
    }

    [Fact]
    public void ThrowsOnInsufficientData()
    {
        byte[] input = [(byte)'5', (byte)':', (byte)'a', (byte)'b'];

        Assert.Throws<ArgumentOutOfRangeException>(() => StringNode.Parse(input));
    }

    [Fact]
    public void ProcessesDoubleDigitLength()
    {
        byte[] input = [(byte)'1', (byte)'1', (byte)':', (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o',
                    (byte)' ', (byte)'w', (byte)'o', (byte)'r', (byte)'l', (byte)'d'];
        var node = StringNode.Parse(input);

        byte[] expectedValue = [(byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o',
                            (byte)' ', (byte)'w', (byte)'o', (byte)'r', (byte)'l', (byte)'d'];
        Assert.Equal(expectedValue, node.Value);
        Assert.Equal(14, node.Representation.Length);
        Assert.Equal(input, node.Representation);
    }
}