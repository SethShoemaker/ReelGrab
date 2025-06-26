namespace tests.Bencoding;

using ReelGrab.Bencoding;

public class IntegerNodeTest
{
    [Fact]
    public void TestSingleDigitByItself()
    {
        var node = IntegerNode.FromString("i4e");
        Assert.Equal(4, node.Value);
        Assert.Equal(3, node.RepresentationLength);
        Assert.Equal("i4e", node.Representation);
    }

    [Fact]
    public void TestSingleDigitProceededWitMore()
    {
        var node = IntegerNode.FromString("i4e4:spam");
        Assert.Equal(4, node.Value);
        Assert.Equal(3, node.RepresentationLength);
        Assert.Equal("i4e", node.Representation);
    }

    [Fact]
    public void TestDoubleDigitByItself()
    {
        var node = IntegerNode.FromString("i69e");
        Assert.Equal(69, node.Value);
        Assert.Equal(4, node.RepresentationLength);
        Assert.Equal("i69e", node.Representation);
    }

    [Fact]
    public void TestDoubleDigitProceededWitMore()
    {
        var node = IntegerNode.FromString("i69e4:spam");
        Assert.Equal(69, node.Value);
        Assert.Equal(4, node.RepresentationLength);
        Assert.Equal("i69e", node.Representation);
    }

    [Fact]
    public void TestZeroByItselfProceededWithMore()
    {
        var node = IntegerNode.FromString("i0e4:spam");
        Assert.Equal(0, node.Value);
        Assert.Equal(3, node.RepresentationLength);
        Assert.Equal("i0e", node.Representation);
    }

    [Fact]
    public void TestSingleDigitNegativeNumberByItself()
    {
        var node = IntegerNode.FromString("i-1e");
        Assert.Equal(-1, node.Value);
        Assert.Equal(4, node.RepresentationLength);
        Assert.Equal("i-1e", node.Representation);
    }

    [Fact]
    public void TestSingleDigitNegativeNumberProceededWithMore()
    {
        var node = IntegerNode.FromString("i-1e4:spam");
        Assert.Equal(-1, node.Value);
        Assert.Equal(4, node.RepresentationLength);
        Assert.Equal("i-1e", node.Representation);
    }

    [Fact]
    public void TestDoubleDigitNegativeNumberByItself()
    {
        var node = IntegerNode.FromString("i-12e");
        Assert.Equal(-12, node.Value);
        Assert.Equal(5, node.RepresentationLength);
        Assert.Equal("i-12e", node.Representation);
    }

    [Fact]
    public void TestDoubleDigitNegativeNumberProceededWithMore()
    {
        var node = IntegerNode.FromString("i-12e4:spam");
        Assert.Equal(-12, node.Value);
        Assert.Equal(5, node.RepresentationLength);
        Assert.Equal("i-12e", node.Representation);
    }

    [Fact]
    public void TestFailsWhenDoesntBeginWithI()
    {
        var exception = Assert.Throws<Exception>(() => IntegerNode.FromString("4e"));
        Assert.Equal("4e does not start with an integer node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenGivenNegativeZero()
    {
        var exception = Assert.Throws<Exception>(() => IntegerNode.FromString("i-0e"));
        Assert.Equal("i-0e does not start with an integer node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenGivenLeadingZeroes()
    {
        var exception = Assert.Throws<Exception>(() => IntegerNode.FromString("i01e"));
        Assert.Equal("i01e does not start with an integer node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenGivenLeadingZeroesBeforeNegative()
    {
        var exception = Assert.Throws<Exception>(() => IntegerNode.FromString("i-09e"));
        Assert.Equal("i-09e does not start with an integer node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenAlphabeticalCharacterWhereNumberShouldBe()
    {
        var exception = Assert.Throws<Exception>(() => IntegerNode.FromString("i4he"));
        Assert.Equal("i4he does not start with an integer node", exception.Message);
    }

    [Fact]
    public void TestFailsWhenDoesntContainE()
    {
        var exception = Assert.Throws<Exception>(() => IntegerNode.FromString("i4"));
        Assert.Equal("i4 does not start with an integer node", exception.Message);
    }
}