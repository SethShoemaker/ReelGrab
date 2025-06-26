namespace tests.Bencoding;

using ReelGrab.Bencoding;

public class DictionaryNodeTest
{
    [Fact]
    public void TestSimpleDictionaryByItself()
    {
        var node = DictionaryNode.FromString("d5:hello5:worlde");
    }

    [Fact]
    public void ParsesEmptyDictionary()
    {
        var node = DictionaryNode.FromString("de");

        Assert.Empty(node.Elements);
        Assert.Equal(2, node.RepresentationLength);
    }

    [Fact]
    public void ParsesSingleStringPair()
    {
        var node = DictionaryNode.FromString("d3:foo3:bare");

        Assert.Single(node.Elements);

        var element = node.Elements[0];
        Assert.Equal("foo", element.Key.Value);
        Assert.IsType<StringNode>(element.Value);
        Assert.Equal("bar", ((StringNode)element.Value).Value);
        Assert.Equal(12, node.RepresentationLength);
    }

    [Fact]
    public void ParsesStringAndIntegerPair()
    {
        var node = DictionaryNode.FromString("d3:fooi42ee");

        Assert.Single(node.Elements);

        var element = node.Elements[0];
        Assert.Equal("foo", element.Key.Value);
        Assert.IsType<IntegerNode>(element.Value);
        Assert.Equal(42, ((IntegerNode)element.Value).Value);
        Assert.Equal(11, node.RepresentationLength);
    }

    [Fact]
    public void ParsesMultipleEntries()
    {
        var node = DictionaryNode.FromString("d1:a3:one1:b3:twoe");

        Assert.Equal(2, node.Elements.Count);
        Assert.Equal("a", node.Elements[0].Key.Value);
        Assert.Equal("one", ((StringNode)node.Elements[0].Value).Value);
        Assert.Equal("b", node.Elements[1].Key.Value);
        Assert.Equal("two", ((StringNode)node.Elements[1].Value).Value);
        Assert.Equal(18, node.RepresentationLength);
    }

    [Fact]
    public void ParsesListAsValue()
    {
        var node = DictionaryNode.FromString("d4:listli1ei2ei3eee");

        Assert.Single(node.Elements);

        var element = node.Elements[0];
        Assert.Equal("list", element.Key.Value);
        var listNode = Assert.IsType<ListNode>(element.Value);
        Assert.Equal(3, listNode.Elements.Count);
        Assert.All(listNode.Elements, n => Assert.IsType<IntegerNode>(n));
        Assert.Equal(19, node.RepresentationLength);
    }

    [Fact]
    public void ThrowsOnMissingStartingD()
    {
        var ex = Assert.Throws<Exception>(() => DictionaryNode.FromString("3:key3:val"));
        Assert.Equal("3:key3:val does not start with a dictionary node", ex.Message);
    }

    [Fact]
    public void ThrowsOnMissingEnd()
    {
        var ex = Assert.Throws<IndexOutOfRangeException>(() => DictionaryNode.FromString("d3:foo3:bar"));
    }

    [Fact]
    public void TestNestedDictionary()
    {
        var node = DictionaryNode.FromString("d5:hellod4:name3:bobee");

        Assert.Single(node.Elements);

        var topElement = node.Elements[0];
        Assert.Equal("hello", topElement.Key.Value);

        var nested = Assert.IsType<DictionaryNode>(topElement.Value);

        Assert.Single(nested.Elements);
        var nestedElement = nested.Elements[0];

        Assert.Equal("name", nestedElement.Key.Value);
        var nameValue = Assert.IsType<StringNode>(nestedElement.Value);
        Assert.Equal("bob", nameValue.Value);

        Assert.Equal(22, node.RepresentationLength);
    }
}