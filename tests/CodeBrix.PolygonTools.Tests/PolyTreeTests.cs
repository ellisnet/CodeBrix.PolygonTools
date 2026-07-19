using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class PolyTreeTests
{
    /// <summary>
    /// A 100x100 square with a 50x50 hole in the middle, expressed as a PolyTree.
    /// </summary>
    private static PolyTree SquareWithHole()
    {
        var polyClip = new PolyClip();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true);
        polyClip.AddPath(PolygonFactory.ReversedSquare(25, 25, 50), PolyType.ptSubject, true);

        var tree = new PolyTree();
        polyClip.Execute(ClipType.ctUnion, tree, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
        return tree;
    }

    [Fact]
    public void can_get_instance()
        => new PolyTree().Should().NotBeNull();

    [Fact]
    public void Total_of_a_new_tree_is_zero()
        => new PolyTree().Total.Should().Be(0);

    [Fact]
    public void GetFirst_of_a_new_tree_is_null()
        => new PolyTree().GetFirst().Should().BeNull();

    [Fact]
    public void Total_counts_every_node_except_the_root()
        => SquareWithHole().Total.Should().Be(2);

    [Fact]
    public void ChildCount_of_the_root_counts_only_the_outer_polygons()
        => SquareWithHole().ChildCount.Should().Be(1);

    [Fact]
    public void GetFirst_returns_the_outer_polygon()
    {
        //Arrange
        var tree = SquareWithHole();

        //Act
        var first = tree.GetFirst();

        //Assert
        first.Should().NotBeNull();
        first.IsHole.Should().BeFalse();
    }

    [Fact]
    public void IsHole_is_true_for_the_child_of_an_outer_polygon()
        => SquareWithHole().GetFirst().Childs[0].IsHole.Should().BeTrue();

    [Fact]
    public void Parent_of_a_hole_is_the_polygon_that_contains_it()
    {
        //Arrange
        var tree = SquareWithHole();
        var outer = tree.GetFirst();

        //Act
        var parent = outer.Childs[0].Parent;

        //Assert
        parent.Should().BeSameAs(outer);
    }

    [Fact]
    public void GetNext_walks_the_whole_tree()
    {
        //Arrange
        var tree = SquareWithHole();
        var node = tree.GetFirst();
        var visited = 0;

        //Act
        while (node != null)
        {
            visited++;
            node = node.GetNext();
        }

        //Assert
        visited.Should().Be(2);
    }

    [Fact]
    public void ChildCount_of_a_leaf_node_is_zero()
        => SquareWithHole().GetFirst().Childs[0].ChildCount.Should().Be(0);

    [Fact]
    public void Contour_holds_the_vertices_of_the_node()
        => SquareWithHole().GetFirst().Contour.Should().HaveCount(4);

    [Fact]
    public void IsOpen_is_false_for_a_closed_polygon()
        => SquareWithHole().GetFirst().IsOpen.Should().BeFalse();

    [Fact]
    public void IsOpen_is_true_for_a_clipped_open_path()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath([new IntPoint(-50, 50), new IntPoint(150, 50)], PolyType.ptSubject, false);
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptClip, true);
        var tree = new PolyTree();

        //Act
        polyClip.Execute(ClipType.ctIntersection, tree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        tree.GetFirst().IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Clear_empties_a_populated_tree()
    {
        //Arrange
        var tree = SquareWithHole();

        //Act
        tree.Clear();

        //Assert
        tree.Total.Should().Be(0);
        tree.ChildCount.Should().Be(0);
        tree.GetFirst().Should().BeNull();
    }
}
