namespace tests.Bencoding;

using ReelGrab.Bencoding;

public class ListNodeTest
{
    [Fact]
    public void TestEmptyListByItself()
    {
        var node = ListNode.FromString("le");
        Assert.Empty(node.Elements);
        Assert.Equal(2, node.RepresentationLength);
        Assert.Equal("le", node.Representation);
    }

    [Fact]
    public void TestEmptyListProceededWithMore()
    {
        var node = ListNode.FromString("le4:spam");
        Assert.Empty(node.Elements);
        Assert.Equal(2, node.RepresentationLength);
        Assert.Equal("le", node.Representation);
    }

    [Fact]
    public void TestSingleStringValueListByItself()
    {
        var node = ListNode.FromString("l4:spame");
        Assert.Single(node.Elements);
        var stringNode = Assert.IsType<StringNode>(node.Elements[0]);
        Assert.Equal("spam", stringNode.Value);
        Assert.Equal(6, stringNode.RepresentationLength);
        Assert.Equal(8, node.RepresentationLength);
        Assert.Equal("l4:spame", node.Representation);
    }

    [Fact]
    public void TestSingleStringValueListProceededWithMore()
    {
        var node = ListNode.FromString("l4:spame5:green");
        Assert.Single(node.Elements);
        var stringNode = Assert.IsType<StringNode>(node.Elements[0]);
        Assert.Equal("spam", stringNode.Value);
        Assert.Equal(6, stringNode.RepresentationLength);
        Assert.Equal(8, node.RepresentationLength);
        Assert.Equal("l4:spame", node.Representation);
    }

    [Fact]
    public void TestSingleIntegerValueListByItself()
    {
        var node = ListNode.FromString("li2ee");
        Assert.Single(node.Elements);
        var integerNode = Assert.IsType<IntegerNode>(node.Elements[0]);
        Assert.Equal(2, integerNode.Value);
        Assert.Equal(3, integerNode.RepresentationLength);
        Assert.Equal(5, node.RepresentationLength);
        Assert.Equal("li2ee", node.Representation);
    }

    [Fact]
    public void TestSingleIntegerValueListProceededWithMore()
    {
        var node = ListNode.FromString("li2ee5:green");
        Assert.Single(node.Elements);
        var integerNode = Assert.IsType<IntegerNode>(node.Elements[0]);
        Assert.Equal(2, integerNode.Value);
        Assert.Equal(3, integerNode.RepresentationLength);
        Assert.Equal(5, node.RepresentationLength);
        Assert.Equal("li2ee", node.Representation);
    }

    [Fact]
    public void TestMultiValuedListByItself()
    {
        var node = ListNode.FromString("li2e4:spame");
        Assert.Equal(2, node.Elements.Count);
        var integerNode = Assert.IsType<IntegerNode>(node.Elements[0]);
        Assert.Equal(2, integerNode.Value);
        Assert.Equal(3, integerNode.RepresentationLength);
        var stringNode = Assert.IsType<StringNode>(node.Elements[1]);
        Assert.Equal("spam", stringNode.Value);
        Assert.Equal(6, stringNode.RepresentationLength);
        Assert.Equal(11, node.RepresentationLength);
        Assert.Equal("li2e4:spame", node.Representation);
    }

    [Fact]
    public void TestMultiValuedListProceededWithMore()
    {
        var node = ListNode.FromString("li2e4:spame5:green");
        Assert.Equal(2, node.Elements.Count);
        var integerNode = Assert.IsType<IntegerNode>(node.Elements[0]);
        Assert.Equal(2, integerNode.Value);
        Assert.Equal(3, integerNode.RepresentationLength);
        var stringNode = Assert.IsType<StringNode>(node.Elements[1]);
        Assert.Equal("spam", stringNode.Value);
        Assert.Equal(6, stringNode.RepresentationLength);
        Assert.Equal(11, node.RepresentationLength);
        Assert.Equal("li2e4:spame", node.Representation);
    }
    [Fact]
    public void TestNestedListByItself()
    {
        var outerNode = ListNode.FromString("lli2eel4:spami51eee");
        Assert.Equal(2, outerNode.Elements.Count);
        var firstInnerList = Assert.IsType<ListNode>(outerNode.Elements[0]);
        Assert.Single(firstInnerList.Elements);
        var firstInnerListIntegerNode = Assert.IsType<IntegerNode>(firstInnerList.Elements[0]);
        Assert.Equal(2, firstInnerListIntegerNode.Value);
        Assert.Equal(3, firstInnerListIntegerNode.RepresentationLength);
        Assert.Equal(5, firstInnerList.RepresentationLength);
        var secondInnerList = Assert.IsType<ListNode>(outerNode.Elements[1]);
        Assert.Equal(2, secondInnerList.Elements.Count);
        var secondInnerListStringNode = Assert.IsType<StringNode>(secondInnerList.Elements[0]);
        Assert.Equal("spam", secondInnerListStringNode.Value);
        Assert.Equal(6, secondInnerListStringNode.RepresentationLength);
        var secondInnerListIntegerNode = Assert.IsType<IntegerNode>(secondInnerList.Elements[1]);
        Assert.Equal(51, secondInnerListIntegerNode.Value);
        Assert.Equal(4, secondInnerListIntegerNode.RepresentationLength);
        Assert.Equal(12, secondInnerList.RepresentationLength);
        Assert.Equal("lli2eel4:spami51eee", outerNode.Representation);
    }
}