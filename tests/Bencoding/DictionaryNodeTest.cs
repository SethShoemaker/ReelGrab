namespace tests.Bencoding;

using ReelGrab.Bencoding;

public class DictionaryNodeTest
{
    [Fact]
    public void TestSimpleDictionaryByItself()
    {
        var node = DictionaryNode.Parse("d5:hello5:worlde"u8.ToArray());
        Assert.Single(node.KeyValuePairs);
        Assert.Equal("hello"u8.ToArray(), node.KeyValuePairs[0].Key.Value);
        Assert.Equal("world"u8.ToArray(), ((StringNode)node.KeyValuePairs[0].Value).Value);
        Assert.Equal("d5:hello5:worlde"u8.ToArray(), node.Representation);
    }
}