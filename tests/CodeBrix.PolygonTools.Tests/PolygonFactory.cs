using System;
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Models;

namespace CodeBrix.PolygonTools.Tests;

/// <summary>
/// Shared geometry builders and measurements used across the test suite.
/// </summary>
internal static class PolygonFactory
{
    /// <summary>
    /// An axis-aligned square whose lower-left corner is at the supplied coordinates.
    /// The vertices are wound so that <c>PolyClip.Orientation</c> returns true.
    /// </summary>
    internal static List<IntPoint> Square(long x, long y, long size) =>
    [
        new IntPoint(x, y),
        new IntPoint(x + size, y),
        new IntPoint(x + size, y + size),
        new IntPoint(x, y + size)
    ];

    /// <summary>
    /// An axis-aligned rectangle described by its lower-left corner, width and height.
    /// </summary>
    internal static List<IntPoint> Rectangle(long x, long y, long width, long height) =>
    [
        new IntPoint(x, y),
        new IntPoint(x + width, y),
        new IntPoint(x + width, y + height),
        new IntPoint(x, y + height)
    ];

    /// <summary>
    /// The same square as <see cref="Square"/> but wound the other way, which is how a
    /// hole is expressed when the even-odd filling rule is not being used.
    /// </summary>
    internal static List<IntPoint> ReversedSquare(long x, long y, long size)
    {
        var square = Square(x, y, size);
        square.Reverse();
        return square;
    }

    /// <summary>
    /// A bow-tie: four vertices whose edges cross, so the path is self-intersecting.
    /// </summary>
    internal static List<IntPoint> SelfIntersecting() =>
    [
        new IntPoint(0, 0),
        new IntPoint(100, 100),
        new IntPoint(100, 0),
        new IntPoint(0, 100)
    ];

    /// <summary>
    /// The sum of the absolute areas of every supplied path.
    /// </summary>
    internal static double TotalAbsoluteArea(List<List<IntPoint>> paths)
    {
        double total = 0;
        foreach (var path in paths)
        {
            total += Math.Abs(PolyClip.Area(path));
        }

        return total;
    }

    /// <summary>
    /// Runs a single boolean operation over one subject path and one clip path.
    /// </summary>
    internal static List<List<IntPoint>> Clip(
        Enumerations.ClipType clipType,
        List<IntPoint> subject,
        List<IntPoint> clip,
        Enumerations.PolyFillType fillType = Enumerations.PolyFillType.pftNonZero)
    {
        var polyClip = new PolyClip();
        polyClip.AddPath(subject, Enumerations.PolyType.ptSubject, true);
        polyClip.AddPath(clip, Enumerations.PolyType.ptClip, true);

        var solution = new List<List<IntPoint>>();
        polyClip.Execute(clipType, solution, fillType, fillType);
        return solution;
    }
}
