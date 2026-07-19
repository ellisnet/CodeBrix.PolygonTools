using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class IntPointTests
{
    [Fact]
    public void IntPoint_constructor_stores_both_coordinates()
    {
        //Arrange
        //Act
        var point = new IntPoint(11L, 22L);

        //Assert
        point.X.Should().Be(11L);
        point.Y.Should().Be(22L);
    }

    [Fact]
    public void IntPoint_constructor_truncates_floating_point_coordinates()
    {
        //Arrange
        //Act
        var point = new IntPoint(11.9, -22.9);

        //Assert
        point.X.Should().Be(11L);
        point.Y.Should().Be(-22L);
    }

    [Fact]
    public void IntPoint_constructor_copies_another_point()
    {
        //Arrange
        var original = new IntPoint(7L, 8L);

        //Act
        var copy = new IntPoint(original);

        //Assert
        copy.Should().Be(original);
    }

    [Fact]
    public void IntPoint_holds_the_full_64_bit_coordinate_range()
    {
        //Arrange
        //Act
        var point = new IntPoint(long.MinValue, long.MaxValue);

        //Assert
        point.X.Should().Be(long.MinValue);
        point.Y.Should().Be(long.MaxValue);
    }

    [Fact]
    public void operator_equality_is_true_for_matching_coordinates()
        => (new IntPoint(3L, 4L) == new IntPoint(3L, 4L)).Should().BeTrue();

    [Fact]
    public void operator_equality_is_false_when_a_coordinate_differs()
        => (new IntPoint(3L, 4L) == new IntPoint(3L, 5L)).Should().BeFalse();

    [Fact]
    public void operator_inequality_is_true_when_a_coordinate_differs()
        => (new IntPoint(3L, 4L) != new IntPoint(4L, 4L)).Should().BeTrue();

    [Fact]
    public void operator_inequality_is_false_for_matching_coordinates()
        => (new IntPoint(3L, 4L) != new IntPoint(3L, 4L)).Should().BeFalse();

    [Fact]
    public void Equals_is_true_for_a_matching_point()
        => new IntPoint(3L, 4L).Equals(new IntPoint(3L, 4L)).Should().BeTrue();

    [Fact]
    public void Equals_is_false_for_a_different_point()
        => new IntPoint(3L, 4L).Equals(new IntPoint(9L, 4L)).Should().BeFalse();

    [Fact]
    public void Equals_is_false_for_a_null_reference()
        => new IntPoint(3L, 4L).Equals(null).Should().BeFalse();

    [Fact]
    public void Equals_is_false_for_an_unrelated_type()
        => new IntPoint(3L, 4L).Equals("not a point").Should().BeFalse();

    [Fact]
    public void GetHashCode_matches_for_equal_points()
        => new IntPoint(3L, 4L).GetHashCode().Should().Be(new IntPoint(3L, 4L).GetHashCode());

    [Fact]
    public void GetHashCode_differs_for_transposed_coordinates()
        => new IntPoint(3L, 4L).GetHashCode().Should().NotBe(new IntPoint(4L, 3L).GetHashCode());
}
