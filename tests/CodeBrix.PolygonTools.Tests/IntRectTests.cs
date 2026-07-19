using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class IntRectTests
{
    [Fact]
    public void IntRect_constructor_stores_every_edge()
    {
        //Arrange
        //Act
        var rect = new IntRect(1L, 2L, 3L, 4L);

        //Assert
        rect.left.Should().Be(1L);
        rect.top.Should().Be(2L);
        rect.right.Should().Be(3L);
        rect.bottom.Should().Be(4L);
    }

    [Fact]
    public void IntRect_constructor_copies_another_rectangle()
    {
        //Arrange
        var original = new IntRect(1L, 2L, 3L, 4L);

        //Act
        var copy = new IntRect(original);

        //Assert
        copy.left.Should().Be(original.left);
        copy.top.Should().Be(original.top);
        copy.right.Should().Be(original.right);
        copy.bottom.Should().Be(original.bottom);
    }

    [Fact]
    public void IntRect_copy_is_independent_of_the_original()
    {
        //Arrange
        var original = new IntRect(1L, 2L, 3L, 4L);
        var copy = new IntRect(original);

        //Act
        original.left = 99L;

        //Assert
        copy.left.Should().Be(1L);
    }

    [Fact]
    public void IntRect_accepts_negative_edges()
        => new IntRect(-40L, -30L, -20L, -10L).left.Should().Be(-40L);
}
