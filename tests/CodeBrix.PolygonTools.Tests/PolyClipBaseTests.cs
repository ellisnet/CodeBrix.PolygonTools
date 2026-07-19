using System;
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class PolyClipBaseTests
{
    [Fact]
    public void AddPath_returns_true_for_a_valid_closed_polygon()
        => new PolyClip().AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true)
            .Should().BeTrue();

    [Fact]
    public void AddPath_returns_false_for_a_closed_path_with_too_few_vertices()
        => new PolyClip().AddPath([new IntPoint(0, 0), new IntPoint(10, 10)], PolyType.ptSubject, true)
            .Should().BeFalse();

    [Fact]
    public void AddPath_returns_false_for_an_empty_path()
        => new PolyClip().AddPath([], PolyType.ptSubject, true).Should().BeFalse();

    [Fact]
    public void AddPath_returns_false_for_a_path_of_coincident_vertices()
        => new PolyClip().AddPath(
                [new IntPoint(5, 5), new IntPoint(5, 5), new IntPoint(5, 5)],
                PolyType.ptSubject,
                true)
            .Should().BeFalse();

    [Fact]
    public void AddPath_accepts_an_open_subject_path_of_two_vertices()
        => new PolyClip().AddPath([new IntPoint(0, 0), new IntPoint(10, 10)], PolyType.ptSubject, false)
            .Should().BeTrue();

    [Fact]
    public void AddPath_throws_for_an_open_clip_path()
    {
        //Arrange
        var polyClip = new PolyClip();

        //Act
        Action act = () => polyClip.AddPath(
            [new IntPoint(0, 0), new IntPoint(10, 10)], PolyType.ptClip, false);

        //Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddPath_throws_when_a_coordinate_exceeds_the_allowed_range()
    {
        //Arrange
        var polyClip = new PolyClip();
        var outOfRange = new List<IntPoint>
        {
            new IntPoint(long.MaxValue, 0), new IntPoint(10, 10), new IntPoint(5, 20)
        };

        //Act
        Action act = () => polyClip.AddPath(outOfRange, PolyType.ptSubject, true);

        //Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddPath_accepts_a_coordinate_at_the_high_range_limit()
        => new PolyClip().AddPath(
                [
                    new IntPoint(PolyClipBase.hiRange, 0),
                    new IntPoint(0, PolyClipBase.hiRange),
                    new IntPoint(0, 0)
                ],
                PolyType.ptSubject,
                true)
            .Should().BeTrue();

    [Fact]
    public void AddPaths_returns_true_when_at_least_one_path_is_added()
    {
        //Arrange
        var paths = new List<List<IntPoint>>
        {
            PolygonFactory.Square(0, 0, 100),
            new List<IntPoint> { new IntPoint(0, 0), new IntPoint(1, 1) }
        };

        //Act
        var result = new PolyClip().AddPaths(paths, PolyType.ptSubject, true);

        //Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AddPaths_returns_false_for_an_empty_collection()
        => new PolyClip().AddPaths([], PolyType.ptSubject, true).Should().BeFalse();

    [Fact]
    public void AddPaths_returns_false_when_every_path_is_degenerate()
    {
        //Arrange
        var paths = new List<List<IntPoint>>
        {
            new List<IntPoint> { new IntPoint(0, 0), new IntPoint(1, 1) },
            new List<IntPoint> { new IntPoint(5, 5) }
        };

        //Act
        var result = new PolyClip().AddPaths(paths, PolyType.ptSubject, true);

        //Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Clear_discards_the_paths_that_were_added()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true);

        //Act
        polyClip.Clear();
        var solution = new List<List<IntPoint>>();
        polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        solution.Should().BeEmpty();
    }

    [Fact]
    public void GetBounds_encloses_every_supplied_path()
    {
        //Arrange
        var paths = new List<List<IntPoint>>
        {
            PolygonFactory.Square(0, 0, 100),
            PolygonFactory.Square(50, 50, 100)
        };

        //Act
        var bounds = PolyClipBase.GetBounds(paths);

        //Assert
        bounds.left.Should().Be(0L);
        bounds.top.Should().Be(0L);
        bounds.right.Should().Be(150L);
        bounds.bottom.Should().Be(150L);
    }

    [Fact]
    public void GetBounds_handles_negative_coordinates()
    {
        //Arrange
        var paths = new List<List<IntPoint>> { PolygonFactory.Square(-200, -100, 50) };

        //Act
        var bounds = PolyClipBase.GetBounds(paths);

        //Assert
        bounds.left.Should().Be(-200L);
        bounds.top.Should().Be(-100L);
        bounds.right.Should().Be(-150L);
        bounds.bottom.Should().Be(-50L);
    }

    [Fact]
    public void GetBounds_of_an_empty_collection_is_an_empty_rectangle()
    {
        //Arrange
        //Act
        var bounds = PolyClipBase.GetBounds([]);

        //Assert
        bounds.left.Should().Be(0L);
        bounds.right.Should().Be(0L);
    }

    [Fact]
    public void PreserveCollinear_defaults_to_false()
        => new PolyClip().PreserveCollinear.Should().BeFalse();

    [Fact]
    public void PreserveCollinear_round_trips()
    {
        //Arrange
        var polyClip = new PolyClip();

        //Act
        polyClip.PreserveCollinear = true;

        //Assert
        polyClip.PreserveCollinear.Should().BeTrue();
    }

    [Fact]
    public void Swap_exchanges_the_two_values()
    {
        //Arrange
        long first = 11;
        long second = 22;

        //Act
        new PolyClip().Swap(ref first, ref second);

        //Assert
        first.Should().Be(22L);
        second.Should().Be(11L);
    }

    [Fact]
    public void loRange_is_the_documented_constant()
        => PolyClipBase.loRange.Should().Be(0x3FFFFFFFL);

    [Fact]
    public void hiRange_is_the_documented_constant()
        => PolyClipBase.hiRange.Should().Be(0x3FFFFFFFFFFFFFFFL);
}
