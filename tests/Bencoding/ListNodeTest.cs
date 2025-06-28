namespace tests.Bencoding;

using ReelGrab.Bencoding;

public class ListNodeTest
{
    [Fact]
    public void ParsesEmptyListByItself()
    {
        var node = ListNode.Parse([(byte)'l', (byte)'e']);
        Assert.Empty(node.Elements);
        Assert.Equal(2, node.Representation.Length);
        Assert.Equal([(byte)'l', (byte)'e'], node.Representation);
    }

    [Fact]
    public void ParsesEmptyListProceededWithMore()
    {
        var node = ListNode.Parse([(byte)'l', (byte)'e', (byte)'e']);
        Assert.Empty(node.Elements);
        Assert.Equal(2, node.Representation.Length);
        Assert.Equal([(byte)'l', (byte)'e'], node.Representation);
    }

    [Fact]
    public void TestListWithSingleInteger()
    {
        var node = ListNode.Parse([(byte)'l', (byte)'i', (byte)'4', (byte)'e', (byte)'e']);
        Assert.Single(node.Elements);
        Assert.IsType<IntegerNode>(node.Elements[0]);
        Assert.Equal(5, node.Representation.Length);
        Assert.Equal([(byte)'l', (byte)'i', (byte)'4', (byte)'e', (byte)'e'], node.Representation);
    }

    [Fact]
    public void TestListWithMultipleStrings()
    {
        var node = ListNode.Parse("l3:foo3:bare"u8.ToArray());
        Assert.Equal(2, node.Elements.Count);
        Assert.All(node.Elements, el => Assert.IsType<StringNode>(el));
        Assert.Equal("foo", ((StringNode)node.Elements[0]).ValueString);
        Assert.Equal("bar", ((StringNode)node.Elements[1]).ValueString);
    }

    [Fact]
    public void TestListWithNestedList()
    {
        var node = ListNode.Parse("ll5:helloee"u8.ToArray());
        Assert.Single(node.Elements);
        var inner = Assert.IsType<ListNode>(node.Elements[0]);
        Assert.Single(inner.Elements);
        Assert.Equal("hello", ((StringNode)inner.Elements[0]).ValueString);
    }
}