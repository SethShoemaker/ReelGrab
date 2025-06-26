namespace tests.Bencoding;

using ReelGrab.Bencoding;
public class StringNodeTest
{
    [Fact]
    public void TestSingleDigitLengthByItself()
    {
        var node = StringNode.FromString("4:spam");
        Assert.Equal("spam", node.Value);
        Assert.Equal(6, node.RepresentationLength);
    }

    [Fact]
    public void TestSingleDigitLengthProceededWithMore()
    {
        var node = StringNode.FromString("5:hello4:hello");
        Assert.Equal("hello", node.Value);
        Assert.Equal(7, node.RepresentationLength);
    }

    [Fact]
    public void TestDoubleDigitLengthByItself()
    {
        var node = StringNode.FromString("13:greetingworld4:spam");
        Assert.Equal("greetingworld", node.Value);
        Assert.Equal(16, node.RepresentationLength);
    }

    [Fact]
    public void TestDoubleDigitLengthProceededWithMore()
    {
        var node = StringNode.FromString("13:greetingworld4:spam");
        Assert.Equal("greetingworld", node.Value);
        Assert.Equal(16, node.RepresentationLength);
    }

    [Fact]
    public void TestFailsWhenFirstCharacterNotInteger()
    {
        var exception = Assert.Throws<Exception>(() => StringNode.FromString("h12:helloworld"));
        Assert.Equal("h12:helloworld does not start with a string node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenAlphabeticalCharacterBetweenIntegerAndColon()
    {
        var exception = Assert.Throws<Exception>(() => StringNode.FromString("12h:helloworld"));
        Assert.Equal("12h:helloworld does not start with a string node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenNoColonFound()
    {
        var exception = Assert.Throws<Exception>(() => StringNode.FromString("10helloworld"));
        Assert.Equal("10helloworld does not start with a string node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenMissingCharacters()
    {
        var exception = Assert.Throws<Exception>(() => StringNode.FromString("12:helloworld"));
        Assert.Equal("12:helloworld is missing some characters", exception.Message);
    }
}