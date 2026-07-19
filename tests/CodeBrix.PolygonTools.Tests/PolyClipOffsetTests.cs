using System;
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class PolyClipOffsetTests
{
    private static List<IntPoint> Square => PolygonFactory.Square(0, 0, 100);

    private static List<List<IntPoint>> Offset(
        List<IntPoint> path, double delta, JoinType joinType = JoinType.jtMiter,
        EndType endType = EndType.etClosedPolygon)
    {
        var offset = new PolyClipOffset();
        offset.AddPath(path, joinType, endType);

        var solution = new List<List<IntPoint>>();
        offset.Execute(ref solution, delta);
        return solution;
    }

    [Fact]
    public void can_get_instance()
        => new PolyClipOffset().Should().NotBeNull();

    [Fact]
    public void MiterLimit_defaults_to_two()
        => new PolyClipOffset().MiterLimit.Should().Be(2.0d);

    [Fact]
    public void ArcTolerance_defaults_to_a_quarter()
        => new PolyClipOffset().ArcTolerance.Should().Be(0.25d);

    [Fact]
    public void PolyClipOffset_constructor_applies_the_supplied_limits()
    {
        //Arrange
        //Act
        var offset = new PolyClipOffset(5.0, 0.1);

        //Assert
        offset.MiterLimit.Should().Be(5.0d);
        offset.ArcTolerance.Should().Be(0.1d);
    }

    [Fact]
    public void Execute_inflates_a_square_by_the_supplied_delta()
    {
        //Arrange
        //Act
        var inflated = Offset(Square, 10.0);

        //Assert
        inflated.Should().HaveCount(1);
        //a 100x100 square grown by 10 on every side is 120x120
        Math.Abs(PolyClip.Area(inflated[0])).Should().Be(14400d);
    }

    [Fact]
    public void Execute_deflates_a_square_for_a_negative_delta()
    {
        //Arrange
        //Act
        var deflated = Offset(Square, -10.0);

        //Assert
        deflated.Should().HaveCount(1);
        //a 100x100 square shrunk by 10 on every side is 80x80
        Math.Abs(PolyClip.Area(deflated[0])).Should().Be(6400d);
    }

    [Fact]
    public void Execute_with_a_zero_delta_preserves_the_area()
        => Math.Abs(PolyClip.Area(Offset(Square, 0.0)[0])).Should().Be(10000d);

    [Fact]
    public void Execute_deflating_past_the_shape_yields_nothing()
        => Offset(Square, -100.0).Should().BeEmpty();

    [Fact]
    public void Execute_with_a_miter_join_keeps_the_corner_count()
        => Offset(Square, 10.0, JoinType.jtMiter)[0].Should().HaveCount(4);

    [Fact]
    public void Execute_with_a_round_join_adds_vertices_to_each_corner()
        => Offset(Square, 10.0, JoinType.jtRound)[0].Count.Should().BeGreaterThan(4);

    [Fact]
    public void Execute_with_a_square_join_adds_vertices_to_each_corner()
        => Offset(Square, 10.0, JoinType.jtSquare)[0].Count.Should().BeGreaterThan(4);

    [Fact]
    public void Execute_with_a_round_join_stays_within_the_mitered_area()
    {
        //Arrange
        //Act
        var mitered = Math.Abs(PolyClip.Area(Offset(Square, 10.0, JoinType.jtMiter)[0]));
        var rounded = Math.Abs(PolyClip.Area(Offset(Square, 10.0, JoinType.jtRound)[0]));

        //Assert
        //rounding the corners removes area that mitering would have kept
        rounded.Should().BeLessThan(mitered);
        rounded.Should().BeGreaterThan(10000d);
    }

    [Fact]
    public void Execute_offsets_an_open_path_on_both_sides()
    {
        //Arrange
        var line = new List<IntPoint> { new IntPoint(0, 0), new IntPoint(100, 0) };

        //Act
        var offset = Offset(line, 10.0, JoinType.jtMiter, EndType.etOpenButt);

        //Assert
        offset.Should().HaveCount(1);
        //a 100-long line offset by 10 with butt ends is a 100x20 rectangle
        Math.Abs(PolyClip.Area(offset[0])).Should().Be(2000d);
    }

    [Fact]
    public void Execute_extends_an_open_path_when_the_ends_are_squared()
    {
        //Arrange
        var line = new List<IntPoint> { new IntPoint(0, 0), new IntPoint(100, 0) };

        //Act
        var butt = Math.Abs(PolyClip.Area(Offset(line, 10.0, JoinType.jtMiter, EndType.etOpenButt)[0]));
        var squared = Math.Abs(PolyClip.Area(Offset(line, 10.0, JoinType.jtMiter, EndType.etOpenSquare)[0]));

        //Assert
        //squaring the ends extends the rectangle by the delta at each end
        squared.Should().Be(butt + 400d);
    }

    [Fact]
    public void Execute_with_a_polytree_returns_the_offset_result()
    {
        //Arrange
        var offset = new PolyClipOffset();
        offset.AddPath(Square, JoinType.jtMiter, EndType.etClosedPolygon);
        var tree = new PolyTree();

        //Act
        offset.Execute(ref tree, 10.0);

        //Assert
        tree.Total.Should().Be(1);
        Math.Abs(PolyClip.Area(tree.GetFirst().Contour)).Should().Be(14400d);
    }

    [Fact]
    public void AddPaths_offsets_every_supplied_path()
    {
        //Arrange
        var offset = new PolyClipOffset();
        offset.AddPaths(
            [PolygonFactory.Square(0, 0, 100), PolygonFactory.Square(500, 500, 100)],
            JoinType.jtMiter,
            EndType.etClosedPolygon);
        var solution = new List<List<IntPoint>>();

        //Act
        offset.Execute(ref solution, 10.0);

        //Assert
        solution.Should().HaveCount(2);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(28800d);
    }

    [Fact]
    public void Clear_discards_the_paths_that_were_added()
    {
        //Arrange
        var offset = new PolyClipOffset();
        offset.AddPath(Square, JoinType.jtMiter, EndType.etClosedPolygon);
        var solution = new List<List<IntPoint>>();

        //Act
        offset.Clear();
        offset.Execute(ref solution, 10.0);

        //Assert
        solution.Should().BeEmpty();
    }

    [Fact]
    public void Execute_replaces_the_contents_of_the_supplied_solution()
    {
        //Arrange
        var offset = new PolyClipOffset();
        offset.AddPath(Square, JoinType.jtMiter, EndType.etClosedPolygon);
        var solution = new List<List<IntPoint>> { PolygonFactory.Square(900, 900, 10) };

        //Act
        offset.Execute(ref solution, 10.0);

        //Assert
        solution.Should().HaveCount(1);
        Math.Abs(PolyClip.Area(solution[0])).Should().Be(14400d);
    }

    [Fact]
    public void AddPath_does_not_range_check_its_coordinates()
    {
        //Arrange
        //Unlike PolyClipBase.AddPath, which rejects coordinates beyond hiRange,
        //PolyClipOffset.AddPath performs no range validation of its own. This
        //asymmetry is upstream behaviour and is preserved by this port.
        var offset = new PolyClipOffset();

        //Act
        var act = () => offset.AddPath(
            [
                new IntPoint(long.MaxValue, 0), new IntPoint(10, 10), new IntPoint(5, 20)
            ],
            JoinType.jtMiter,
            EndType.etClosedPolygon);

        //Assert
        act.Should().NotThrow();
    }
}
