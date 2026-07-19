using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class DoublePointTests
{
    [Fact]
    public void DoublePoint_constructor_defaults_both_coordinates_to_zero()
    {
        //Arrange
        //Act
        var point = new DoublePoint();

        //Assert
        point.X.Should().Be(0d);
        point.Y.Should().Be(0d);
    }

    [Fact]
    public void DoublePoint_constructor_stores_both_coordinates()
    {
        //Arrange
        //Act
        var point = new DoublePoint(1.5, -2.5);

        //Assert
        point.X.Should().Be(1.5d);
        point.Y.Should().Be(-2.5d);
    }

    [Fact]
    public void DoublePoint_constructor_copies_another_point()
    {
        //Arrange
        var original = new DoublePoint(1.5, -2.5);

        //Act
        var copy = new DoublePoint(original);

        //Assert
        copy.X.Should().Be(1.5d);
        copy.Y.Should().Be(-2.5d);
    }

    [Fact]
    public void DoublePoint_constructor_widens_an_integer_point()
    {
        //Arrange
        var integerPoint = new IntPoint(7L, 8L);

        //Act
        var point = new DoublePoint(integerPoint);

        //Assert
        point.X.Should().Be(7d);
        point.Y.Should().Be(8d);
    }
}
