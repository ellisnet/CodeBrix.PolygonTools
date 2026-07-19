using System;
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

public class PolyClipTests
{
    //two 100x100 squares overlapping over a 50x50 region
    private static List<IntPoint> SubjectSquare => PolygonFactory.Square(0, 0, 100);
    private static List<IntPoint> ClipSquare => PolygonFactory.Square(50, 50, 100);

    [Fact]
    public void can_get_instance()
        => new PolyClip().Should().NotBeNull();

    [Fact]
    public void Execute_union_covers_both_squares()
    {
        //Arrange
        //Act
        var solution = PolygonFactory.Clip(ClipType.ctUnion, SubjectSquare, ClipSquare);

        //Assert
        solution.Should().HaveCount(1);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(17500d);
    }

    [Fact]
    public void Execute_intersection_covers_only_the_overlap()
    {
        //Arrange
        //Act
        var solution = PolygonFactory.Clip(ClipType.ctIntersection, SubjectSquare, ClipSquare);

        //Assert
        solution.Should().HaveCount(1);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(2500d);
    }

    [Fact]
    public void Execute_difference_removes_the_overlap_from_the_subject()
    {
        //Arrange
        //Act
        var solution = PolygonFactory.Clip(ClipType.ctDifference, SubjectSquare, ClipSquare);

        //Assert
        solution.Should().HaveCount(1);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(7500d);
    }

    [Fact]
    public void Execute_xor_keeps_both_non_overlapping_regions()
    {
        //Arrange
        //Act
        var solution = PolygonFactory.Clip(ClipType.ctXor, SubjectSquare, ClipSquare);

        //Assert
        solution.Should().HaveCount(2);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(15000d);
    }

    [Fact]
    public void Execute_returns_true_on_success()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(SubjectSquare, PolyType.ptSubject, true);
        polyClip.AddPath(ClipSquare, PolyType.ptClip, true);

        //Act
        var result = polyClip.Execute(ClipType.ctUnion, new List<List<IntPoint>>());

        //Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Execute_clears_the_supplied_solution_before_populating_it()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(SubjectSquare, PolyType.ptSubject, true);
        polyClip.AddPath(ClipSquare, PolyType.ptClip, true);
        var solution = new List<List<IntPoint>> { PolygonFactory.Square(900, 900, 10) };

        //Act
        polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        solution.Should().HaveCount(1);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(17500d);
    }

    [Fact]
    public void Execute_of_disjoint_squares_returns_both_unchanged()
    {
        //Arrange
        var far = PolygonFactory.Square(1000, 1000, 100);

        //Act
        var solution = PolygonFactory.Clip(ClipType.ctUnion, SubjectSquare, far);

        //Assert
        solution.Should().HaveCount(2);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(20000d);
    }

    [Fact]
    public void Execute_intersection_of_disjoint_squares_is_empty()
    {
        //Arrange
        var far = PolygonFactory.Square(1000, 1000, 100);

        //Act
        var solution = PolygonFactory.Clip(ClipType.ctIntersection, SubjectSquare, far);

        //Assert
        solution.Should().BeEmpty();
    }

    [Fact]
    public void Execute_of_identical_squares_yields_that_square()
    {
        //Arrange
        //Act
        var solution = PolygonFactory.Clip(ClipType.ctIntersection, SubjectSquare, SubjectSquare);

        //Assert
        solution.Should().HaveCount(1);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(10000d);
    }

    [Fact]
    public void Execute_even_odd_treats_a_nested_square_as_a_hole()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true);
        polyClip.AddPath(PolygonFactory.Square(25, 25, 50), PolyType.ptSubject, true);
        var solution = new List<List<IntPoint>>();

        //Act
        polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

        //Assert
        solution.Should().HaveCount(2);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(12500d);
    }

    [Fact]
    public void Execute_non_zero_absorbs_a_nested_square_wound_the_same_way()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true);
        polyClip.AddPath(PolygonFactory.Square(25, 25, 50), PolyType.ptSubject, true);
        var solution = new List<List<IntPoint>>();

        //Act
        polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        solution.Should().HaveCount(1);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(10000d);
    }

    [Fact]
    public void Execute_with_polytree_reports_the_hole_nesting()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true);
        polyClip.AddPath(PolygonFactory.ReversedSquare(25, 25, 50), PolyType.ptSubject, true);
        var tree = new PolyTree();

        //Act
        var result = polyClip.Execute(ClipType.ctUnion, tree, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

        //Assert
        result.Should().BeTrue();
        tree.Total.Should().Be(2);
        tree.GetFirst().IsHole.Should().BeFalse();
        tree.GetFirst().Childs[0].IsHole.Should().BeTrue();
    }

    [Fact]
    public void Execute_can_be_called_again_after_Clear()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(SubjectSquare, PolyType.ptSubject, true);
        polyClip.AddPath(ClipSquare, PolyType.ptClip, true);
        var first = new List<List<IntPoint>>();
        polyClip.Execute(ClipType.ctUnion, first, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Act
        polyClip.Clear();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 10), PolyType.ptSubject, true);
        var second = new List<List<IntPoint>>();
        polyClip.Execute(ClipType.ctUnion, second, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        PolygonFactory.TotalAbsoluteArea(first).Should().Be(17500d);
        PolygonFactory.TotalAbsoluteArea(second).Should().Be(100d);
    }

    [Fact]
    public void Execute_throws_for_open_paths_when_a_flat_solution_is_requested()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath([new IntPoint(0, 0), new IntPoint(10, 10)], PolyType.ptSubject, false);
        polyClip.AddPath(SubjectSquare, PolyType.ptClip, true);

        //Act
        Action act = () => polyClip.Execute(ClipType.ctIntersection, new List<List<IntPoint>>());

        //Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Execute_accepts_open_paths_when_a_polytree_is_requested()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath([new IntPoint(-50, 50), new IntPoint(150, 50)], PolyType.ptSubject, false);
        polyClip.AddPath(SubjectSquare, PolyType.ptClip, true);
        var tree = new PolyTree();

        //Act
        var result = polyClip.Execute(ClipType.ctIntersection, tree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        result.Should().BeTrue();
        PolyClip.OpenPathsFromPolyTree(tree).Should().HaveCount(1);
    }

    [Fact]
    public void ReverseSolution_defaults_to_false()
        => new PolyClip().ReverseSolution.Should().BeFalse();

    [Fact]
    public void StrictlySimple_defaults_to_false()
        => new PolyClip().StrictlySimple.Should().BeFalse();

    [Fact]
    public void PolyClip_constructor_applies_the_reverse_solution_option()
        => new PolyClip(PolyClip.ioReverseSolution).ReverseSolution.Should().BeTrue();

    [Fact]
    public void PolyClip_constructor_applies_the_strictly_simple_option()
        => new PolyClip(PolyClip.ioStrictlySimple).StrictlySimple.Should().BeTrue();

    [Fact]
    public void PolyClip_constructor_applies_the_preserve_collinear_option()
        => new PolyClip(PolyClip.ioPreserveCollinear).PreserveCollinear.Should().BeTrue();

    [Fact]
    public void PolyClip_constructor_combines_options()
    {
        //Arrange
        //Act
        var polyClip = new PolyClip(PolyClip.ioReverseSolution | PolyClip.ioStrictlySimple);

        //Assert
        polyClip.ReverseSolution.Should().BeTrue();
        polyClip.StrictlySimple.Should().BeTrue();
        polyClip.PreserveCollinear.Should().BeFalse();
    }

    [Fact]
    public void ReverseSolution_inverts_the_orientation_of_the_result()
    {
        //Arrange
        var polyClip = new PolyClip(PolyClip.ioReverseSolution);
        polyClip.AddPath(SubjectSquare, PolyType.ptSubject, true);
        var solution = new List<List<IntPoint>>();

        //Act
        polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        PolyClip.Orientation(solution[0]).Should().BeFalse();
    }

    //Coordinates beyond loRange (0x3FFFFFFF) switch the algorithm onto its wide
    //arithmetic path, which is where System.Int128 replaced the upstream helper.
    //Small polygons placed far from the origin exercise that path while keeping
    //the resulting areas small enough to assert exactly.
    private const long FarFromOrigin = 5_000_000_000L;

    [Fact]
    public void Execute_uses_the_wide_arithmetic_path_for_large_coordinates()
    {
        //Arrange
        var polyClip = new PolyClip();

        //Act
        polyClip.AddPath(PolygonFactory.Square(FarFromOrigin, FarFromOrigin, 100),
            PolyType.ptSubject, true);

        //Assert
        polyClip.m_UseFullRange.Should().BeTrue();
    }

    [Fact]
    public void Execute_stays_on_the_narrow_arithmetic_path_for_small_coordinates()
    {
        //Arrange
        var polyClip = new PolyClip();

        //Act
        polyClip.AddPath(SubjectSquare, PolyType.ptSubject, true);

        //Assert
        polyClip.m_UseFullRange.Should().BeFalse();
    }

    [Theory]
    [InlineData(ClipType.ctUnion, 17500d)]
    [InlineData(ClipType.ctIntersection, 2500d)]
    [InlineData(ClipType.ctDifference, 7500d)]
    [InlineData(ClipType.ctXor, 15000d)]
    public void Execute_on_the_wide_arithmetic_path_matches_the_narrow_path(
        ClipType clipType, double expectedArea)
    {
        //Arrange
        var subject = PolygonFactory.Square(FarFromOrigin, FarFromOrigin, 100);
        var clip = PolygonFactory.Square(FarFromOrigin + 50, FarFromOrigin + 50, 100);
        var polyClip = new PolyClip();
        polyClip.AddPath(subject, PolyType.ptSubject, true);
        polyClip.AddPath(clip, PolyType.ptClip, true);
        var solution = new List<List<IntPoint>>();

        //Act
        polyClip.Execute(clipType, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        polyClip.m_UseFullRange.Should().BeTrue();
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(expectedArea);
    }

    [Fact]
    public void Execute_on_the_wide_arithmetic_path_detects_collinear_edges()
    {
        //Arrange
        //SlopesEqual is the main consumer of the wide arithmetic, so this exercises
        //it directly: two squares sharing an exactly collinear edge must merge.
        var left = PolygonFactory.Rectangle(FarFromOrigin, FarFromOrigin, 100, 100);
        var right = PolygonFactory.Rectangle(FarFromOrigin + 100, FarFromOrigin, 100, 100);
        var polyClip = new PolyClip();
        polyClip.AddPath(left, PolyType.ptSubject, true);
        polyClip.AddPath(right, PolyType.ptClip, true);
        var solution = new List<List<IntPoint>>();

        //Act
        polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Assert
        polyClip.m_UseFullRange.Should().BeTrue();
        solution.Should().HaveCount(1);
        //the shared edge is dissolved, leaving a single 200x100 rectangle
        solution[0].Should().HaveCount(4);
        PolygonFactory.TotalAbsoluteArea(solution).Should().Be(20000d);
    }

    [Fact]
    public void Area_of_a_unit_square_is_its_side_squared()
        => PolyClip.Area(PolygonFactory.Square(0, 0, 100)).Should().Be(10000d);

    [Fact]
    public void Area_is_negative_for_a_reversed_path()
        => PolyClip.Area(PolygonFactory.ReversedSquare(0, 0, 100)).Should().Be(-10000d);

    [Fact]
    public void Area_of_a_degenerate_path_is_zero()
        => PolyClip.Area([new IntPoint(0, 0), new IntPoint(10, 10)]).Should().Be(0d);

    [Fact]
    public void Orientation_is_true_for_a_positive_area_path()
        => PolyClip.Orientation(PolygonFactory.Square(0, 0, 100)).Should().BeTrue();

    [Fact]
    public void Orientation_is_false_for_a_reversed_path()
        => PolyClip.Orientation(PolygonFactory.ReversedSquare(0, 0, 100)).Should().BeFalse();

    [Fact]
    public void ReversePaths_inverts_every_supplied_path()
    {
        //Arrange
        var paths = new List<List<IntPoint>>
        {
            PolygonFactory.Square(0, 0, 100),
            PolygonFactory.Square(500, 500, 100)
        };

        //Act
        PolyClip.ReversePaths(paths);

        //Assert
        PolyClip.Orientation(paths[0]).Should().BeFalse();
        PolyClip.Orientation(paths[1]).Should().BeFalse();
    }

    [Theory]
    [InlineData(50, 50, 1)]
    [InlineData(500, 500, 0)]
    [InlineData(0, 50, -1)]
    [InlineData(0, 0, -1)]
    public void PointInPolygon_classifies_inside_outside_and_boundary(long x, long y, int expected)
        => PolyClip.PointInPolygon(new IntPoint(x, y), PolygonFactory.Square(0, 0, 100))
            .Should().Be(expected);

    [Fact]
    public void SimplifyPolygon_splits_a_self_intersecting_path()
        => PolyClip.SimplifyPolygon(PolygonFactory.SelfIntersecting()).Should().HaveCount(2);

    [Fact]
    public void SimplifyPolygons_splits_a_self_intersecting_path()
    {
        //Arrange
        var polys = new List<List<IntPoint>> { PolygonFactory.SelfIntersecting() };

        //Act
        var simplified = PolyClip.SimplifyPolygons(polys);

        //Assert
        simplified.Should().HaveCount(2);
    }

    [Fact]
    public void CleanPolygon_removes_a_nearly_collinear_vertex()
    {
        //Arrange
        var noisy = new List<IntPoint>
        {
            new IntPoint(0, 0), new IntPoint(1, 0), new IntPoint(100, 0),
            new IntPoint(100, 100), new IntPoint(0, 100)
        };

        //Act
        var cleaned = PolyClip.CleanPolygon(noisy);

        //Assert
        cleaned.Should().HaveCount(4);
    }

    [Fact]
    public void CleanPolygons_cleans_every_supplied_path()
    {
        //Arrange
        var noisy = new List<List<IntPoint>>
        {
            new List<IntPoint>
            {
                new IntPoint(0, 0), new IntPoint(1, 0), new IntPoint(100, 0),
                new IntPoint(100, 100), new IntPoint(0, 100)
            }
        };

        //Act
        var cleaned = PolyClip.CleanPolygons(noisy);

        //Assert
        cleaned[0].Should().HaveCount(4);
    }

    [Fact]
    public void MinkowskiSum_returns_the_swept_region()
        => PolyClip.MinkowskiSum(PolygonFactory.Square(0, 0, 10), PolygonFactory.Square(0, 0, 100), true)
            .Should().NotBeEmpty();

    [Fact]
    public void MinkowskiDiff_returns_the_collision_region()
        => PolyClip.MinkowskiDiff(PolygonFactory.Square(0, 0, 100), PolygonFactory.Square(0, 0, 10))
            .Should().NotBeEmpty();

    [Fact]
    public void PolyTreeToPaths_returns_every_path_in_the_tree()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptSubject, true);
        polyClip.AddPath(PolygonFactory.ReversedSquare(25, 25, 50), PolyType.ptSubject, true);
        var tree = new PolyTree();
        polyClip.Execute(ClipType.ctUnion, tree, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

        //Act
        var paths = PolyClip.PolyTreeToPaths(tree);

        //Assert
        paths.Should().HaveCount(2);
    }

    [Fact]
    public void ClosedPathsFromPolyTree_excludes_open_paths()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath([new IntPoint(-50, 50), new IntPoint(150, 50)], PolyType.ptSubject, false);
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptClip, true);
        var tree = new PolyTree();
        polyClip.Execute(ClipType.ctIntersection, tree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Act
        var closed = PolyClip.ClosedPathsFromPolyTree(tree);

        //Assert
        closed.Should().BeEmpty();
    }

    [Fact]
    public void OpenPathsFromPolyTree_returns_the_clipped_line()
    {
        //Arrange
        var polyClip = new PolyClip();
        polyClip.AddPath([new IntPoint(-50, 50), new IntPoint(150, 50)], PolyType.ptSubject, false);
        polyClip.AddPath(PolygonFactory.Square(0, 0, 100), PolyType.ptClip, true);
        var tree = new PolyTree();
        polyClip.Execute(ClipType.ctIntersection, tree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //Act
        var open = PolyClip.OpenPathsFromPolyTree(tree);

        //Assert
        open.Should().HaveCount(1);
        open[0].Should().ContainInOrder(new IntPoint(100, 50), new IntPoint(0, 50));
    }
}
