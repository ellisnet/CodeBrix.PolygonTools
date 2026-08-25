================================================================================
AGENT-README: CodeBrix.PolygonTools
A Guide for AI Coding Agents — CONSUMING the CodeBrix.PolygonTools.MitLicenseForever NuGet package
================================================================================


OVERVIEW
========

CodeBrix.PolygonTools is a fully managed, dependency-free, cross-platform 2D
polygon clipping and offsetting library for .NET 10 or later. It performs the
four boolean set operations - intersection, union, difference and
exclusive-or - on arbitrary sets of polygons and open paths, and it inflates or
deflates polygons with miter, round or square joins.

What it supports:

  * All four boolean clipping operations on sets of polygons: intersection,
    union, difference and exclusive-or.
  * Four filling rules - even-odd, non-zero, positive and negative - so the
    library interoperates with GDI+, Cairo, OpenGL, SVG and similar rendering
    models.
  * Polygon offsetting (inflating and deflating) with miter, round and square
    joins, and butt, square or round line ends.
  * Open-path (polyline) clipping as well as closed-polygon clipping.
  * Self-intersecting polygons, holes, and polygons with coincident or
    collinear edges.
  * Results returned either as a flat list of paths or as a `PolyTree` that
    preserves the parent/child nesting of outer polygons and their holes.
  * Supporting operations: `SimplifyPolygon`, `SimplifyPolygons`,
    `CleanPolygon`, `CleanPolygons`, `Orientation`, `Area`, `PointInPolygon`,
    `ReversePaths`, `MinkowskiSum`, `MinkowskiDiff`, and the three PolyTree
    flattening helpers.
  * Exact 64-bit integer arithmetic, with no floating-point rounding artifacts
    in the clipping result.

Provenance: this library is derived from the Clipper library ("polyclipping")
by Angus Johnson, version 6.4.2, which is licensed under the Boost Software
License 1.0. The single upstream source file was split into one file per type
and the types were renamed and re-rooted under `CodeBrix.PolygonTools`. Do not
write `using ClipperLib;` -- that namespace does not exist in this package. The
upstream type names map as follows:

    ClipperLib.Clipper          ->  CodeBrix.PolygonTools.PolyClip
    ClipperLib.ClipperBase      ->  CodeBrix.PolygonTools.PolyClipBase
    ClipperLib.ClipperOffset    ->  CodeBrix.PolygonTools.PolyClipOffset
    ClipperLib.ClipperException ->  CodeBrix.PolygonTools.PolyClipException
    everything else             ->  same simple name, new namespace

Enumeration member names are retained verbatim from upstream, Delphi-flavoured
prefixes included (`ctUnion`, `ptSubject`, `pftNonZero`, `jtMiter`,
`etClosedPolygon`). Do not "modernize" them in your code - they are the actual
member names.


INSTALLATION
============

NuGet package id:  CodeBrix.PolygonTools.MitLicenseForever

    dotnet add package CodeBrix.PolygonTools.MitLicenseForever

The package id carries the ".MitLicenseForever" suffix but the assembly and the
namespaces do NOT - they are simply `CodeBrix.PolygonTools`. The suffix is a
CodeBrix family convention that records the license the package will always be
published under.

  Assembly:      CodeBrix.PolygonTools.dll
  Root namespace: CodeBrix.PolygonTools
  Target:        .NET 10 or later (net10.0). No multi-targeting.
  Dependencies:  none. The package references no other NuGet package and no
                 native library.
  License:       MIT AND BSL-1.0. The CodeBrix additions and the combined work
                 are MIT; the derived Clipper code remains under the Boost
                 Software License 1.0. Both license texts ship inside the
                 package (LICENSE and license-boost.txt), together with
                 THIRD-PARTY-NOTICES.txt. `PackageRequireLicenseAcceptance` is
                 true, so a restore will ask the user to accept.
  Platforms:     any platform .NET 10 runs on. There is no OS-specific code, no
                 P/Invoke, no `unsafe` block and no reflection.

There is nothing to initialize, register or configure at start-up. Construct
`PolyClip` or `PolyClipOffset` and use it.


KEY NAMESPACES / USINGS
=======================

    using CodeBrix.PolygonTools;                //PolyClip, PolyClipBase,
                                                //PolyClipOffset
    using CodeBrix.PolygonTools.Enumerations;   //ClipType, PolyType,
                                                //PolyFillType, JoinType,
                                                //EndType
    using CodeBrix.PolygonTools.Models;         //IntPoint, IntRect,
                                                //DoublePoint, PolyNode,
                                                //PolyTree, IntersectNode,
                                                //MyIntersectNodeSort

Almost every real program needs all three, plus
`using System.Collections.Generic;` for the `List<>` types that make up paths.

A fourth namespace, `CodeBrix.PolygonTools.Internal`, holds the implementation
detail of the clipping algorithm (TEdge, OutRec, OutPt, Join, Scanbeam, Maxima,
LocalMinima, EdgeSide, Direction). Everything in it is `internal` and is not
part of the public API. Never write `using CodeBrix.PolygonTools.Internal;`.


THE PATH MODEL
==============

There is no `Path` or `Paths` type in the public API.

    a path  (one polygon or one polyline)  ==  List<IntPoint>
    a set of paths                         ==  List<List<IntPoint>>

The library's own source files use file-level
`using Path = System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>;`
and the matching `Paths` alias for brevity, but those aliases are compile-time
only and never appear in a public signature. Everything you pass in and get
back is a plain `List<IntPoint>` / `List<List<IntPoint>>`.

You may of course declare the same aliases in your own files:

    using Path = System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>;
    using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>>;

A closed polygon is NOT repeated at the end: do not append a copy of the first
vertex to close the ring. A square is four points, not five.

Vertex order determines orientation. With the non-zero, positive or negative
filling rules, orientation decides whether a ring is an outer boundary or a
hole. `PolyClip.Orientation(path)` returns true for a path whose area is
positive (counter-clockwise in a conventional Y-up coordinate system,
clockwise when Y points down as it does on most screens).


COORDINATE MODEL AND SCALING
============================

Coordinates are 64-bit signed integers (`IntPoint.X`, `IntPoint.Y` are `long`).
That is what makes the clipping arithmetic exact and the results robust; it is
also the single largest source of consumer mistakes.

Two range constants are exposed on `PolyClipBase` (so they are reachable as
`PolyClip.loRange` and `PolyClip.hiRange` too):

    public const long loRange = 0x3FFFFFFF;             //1,073,741,823
    public const long hiRange = 0x3FFFFFFFFFFFFFFFL;    //4,611,686,018,427,387,903

  * Any coordinate whose absolute value exceeds `hiRange` is rejected:
    `PolyClipBase.AddPath` throws `PolyClipException("Coordinate outside
    allowed range")`.
  * Inside `loRange` the engine uses ordinary 64-bit arithmetic. Beyond
    `loRange` (but within `hiRange`) it switches to wide arithmetic
    automatically; that path is correct but slower, and offsetting in
    particular is only fully reliable inside `loRange`.
  * Practical rule: keep every coordinate, and every coordinate a computed
    result could reach (offsetting grows the bounds by the delta), inside
    `loRange`.

To use floating-point geometry, multiply by a fixed scale, convert to `long`,
clip, then divide back down. Pick the scale from the precision you need: a
scale of 1,000 keeps three decimal places, a scale of 1,000,000 keeps six.

`IntPoint` has a `double` constructor, `new IntPoint(double x, double y)`, but
it TRUNCATES each component toward zero (`X = (long)x`) - it does NOT round.
That is asymmetric about the origin: 1.9 becomes 1 and -1.9 becomes -1. When
you want nearest-value behaviour, round yourself before converting:

    new IntPoint((long)Math.Round(x * scale, MidpointRounding.AwayFromZero),
                 (long)Math.Round(y * scale, MidpointRounding.AwayFromZero))

The COMPLETE EXAMPLES section below shows a full round trip.


CORE API REFERENCE
==================

MODEL TYPES
-----------

IntPoint  (struct, CodeBrix.PolygonTools.Models)
    A single 2D vertex with 64-bit integer coordinates. Public fields, not
    properties, so they are directly assignable.

        public long X;
        public long Y;

        public IntPoint(long x, long y)
        public IntPoint(double x, double y)   //TRUNCATES toward zero
        public IntPoint(IntPoint pt)          //copy constructor

        public static bool operator ==(IntPoint a, IntPoint b)
        public static bool operator !=(IntPoint a, IntPoint b)
        public override bool Equals(object obj)
        public override int GetHashCode()

    Equality is value equality on X and Y. `Equals(object)` also accepts a
    boxed `IntPoint`. Because it is a struct, `default(IntPoint)` is (0, 0) and
    assigning one `IntPoint` to another copies it. The `double` constructor
    truncates toward zero rather than rounding - see COORDINATE MODEL AND
    SCALING above.

IntRect  (struct, CodeBrix.PolygonTools.Models)
    An axis-aligned bounding box. Public fields, and note the lower-case names
    - they are retained verbatim from upstream.

        public long left;
        public long top;
        public long right;
        public long bottom;

        public IntRect(long l, long t, long r, long b)
        public IntRect(IntRect ir)            //copy constructor

    Produced by `PolyClipBase.GetBounds(List<List<IntPoint>> paths)`. It is a
    plain value carrier: there is no Width, Height, Contains or Intersects
    member, and no equality operator. Compute those yourself
    (`rect.right - rect.left`, and so on). The bounds of an empty path set are
    all zeros.

DoublePoint  (struct, CodeBrix.PolygonTools.Models)
    A floating-point 2D point, used internally for edge normals and available
    to you as a scratch type when scaling.

        public double X;
        public double Y;

        public DoublePoint(double x = 0, double y = 0)
        public DoublePoint(DoublePoint dp)    //copy constructor
        public DoublePoint(IntPoint ip)       //widens a vertex to double

    There is no implicit conversion back to `IntPoint`; construct one with
    `new IntPoint(dp.X, dp.Y)`, which truncates toward zero.

PolyNode  (class, CodeBrix.PolygonTools.Models)
    One node of a `PolyTree`. Nodes are created by the engine; you read them.

        public List<IntPoint> Contour { get; }   //this node's path
        public List<PolyNode> Childs { get; }    //note the upstream spelling
        public int ChildCount { get; }           //== Childs.Count
        public PolyNode Parent { get; }          //null for the tree root
        public bool IsHole { get; }              //computed from nesting depth
        public bool IsOpen { get; set; }         //an open path, not a polygon
        public PolyNode GetNext()                //next node in a depth-first
                                                 //walk, or null at the end

    `Contour` returns the live list the engine built - it is not a defensive
    copy, so do not mutate it in place if you intend to keep walking the tree.
    `IsHole` is derived by counting parents up to the root: a node at odd depth
    is a hole, a node at even depth is an outer polygon, so holes and outer
    polygons alternate with each level of nesting. `Childs` is spelled without
    the "r" - that is the upstream name and it is part of the public API.

PolyTree : PolyNode  (class, CodeBrix.PolygonTools.Models)
    The root of a solution tree, and the type you pass to the PolyTree
    overloads of `Execute`. Construct it yourself with `new PolyTree()`.

        public PolyNode GetFirst()   //first child, or null when empty
        public int Total { get; }    //node count, excluding the root
        public void Clear()          //empties the tree

    `PolyTree` inherits every `PolyNode` member, so the root also has
    `Childs`, `ChildCount` and `Contour` - but the root's own `Contour` is
    empty and its `IsHole` is meaningless. Start from `GetFirst()` and walk
    with `GetNext()`, or recurse over `Childs`.

    `Total` compensates for the hidden outer polygon that a negative offset can
    introduce, so it can be one less than the raw node count.

IntersectNode  (class) and MyIntersectNodeSort  (class)
    `IntersectNode` is public but opaque: every field on it is `internal`, and
    instances are created and consumed by the clipping algorithm itself.
    `MyIntersectNodeSort` is the `IComparer<IntersectNode>` the engine uses to
    order intersections within a scanbeam
    (`public int Compare(IntersectNode node1, IntersectNode node2)`; it throws
    `ArgumentNullException` on a null argument). Neither type is useful to a
    consumer; they are public only because the upstream types were.

ENUMERATIONS
------------

    ClipType      ctIntersection, ctUnion, ctDifference, ctXor
    PolyType      ptSubject, ptClip
    PolyFillType  pftEvenOdd, pftNonZero, pftPositive, pftNegative
    JoinType      jtSquare, jtRound, jtMiter
    EndType       etClosedPolygon, etClosedLine, etOpenButt, etOpenSquare,
                  etOpenRound

  * `ctDifference` subtracts the clip paths from the subject paths, in that
    order. Swapping which set you add as `ptSubject` reverses the result.
  * `pftEvenOdd` is the default for every `Execute` and `SimplifyPolygon`
    overload that takes a single fill type. It ignores orientation. Use
    `pftNonZero` when your rings carry meaningful winding, which is what you
    want for most union and offsetting work.
  * `etClosedPolygon` offsets a closed ring outward/inward. `etClosedLine`
    treats a closed path as a line and offsets both sides of it, producing a
    ring-shaped result. The three `etOpen*` values offset an open polyline and
    differ only in how the two ends are capped.

POLYCLIPBASE
------------

`PolyClipBase` is the base class of `PolyClip`. It has an internal constructor,
so you never instantiate it directly, but it carries the members that add
geometry and you will call them through a `PolyClip` instance.

    public bool AddPath(List<IntPoint> pg, PolyType polyType, bool Closed)
    public bool AddPaths(List<List<IntPoint>> ppg, PolyType polyType,
                         bool closed)
    public virtual void Clear()
    public static IntRect GetBounds(List<List<IntPoint>> paths)
    public bool PreserveCollinear { get; set; }
    public void Swap(ref long val1, ref long val2)
    public const long loRange = 0x3FFFFFFF;
    public const long hiRange = 0x3FFFFFFFFFFFFFFFL;

  * `AddPath` RETURNS FALSE - it does not throw - when a path has too few
    distinct vertices to contribute to the result (fewer than 2 after duplicate
    stripping, or fewer than 3 for a closed path). Check the return value if a
    silently-ignored input would be a bug in your program.
  * `AddPath` THROWS `PolyClipException` when an open path is supplied as clip
    geometry ("AddPath: Open paths must be subject.") and when a coordinate
    exceeds `hiRange` ("Coordinate outside allowed range").
  * `AddPaths` returns true when at least one of its paths was accepted.
  * `Clear()` removes every subject and clip path. `PolyClip` does not override
    it; the inherited implementation is what runs.
  * `PreserveCollinear` keeps vertices that lie on a straight line between
    their neighbours instead of removing them. Set it before adding paths.
  * `GetBounds` is static and takes a path SET, not a single path. Wrap a lone
    path: `PolyClipBase.GetBounds(new List<List<IntPoint>> { path })`.

POLYCLIP
--------

`PolyClip : PolyClipBase` is the clipping engine.

    public PolyClip(int InitOptions = 0)

        public const int ioReverseSolution  = 1;
        public const int ioStrictlySimple   = 2;
        public const int ioPreserveCollinear = 4;

    `InitOptions` is a bitwise OR of those three constants, e.g.
    `new PolyClip(PolyClip.ioStrictlySimple | PolyClip.ioPreserveCollinear)`.
    Each simply sets the matching property, so the property setters are an
    equally good way to configure an instance.

    Properties
        public bool ReverseSolution { get; set; }
            Reverses the orientation of the solution paths. Default false.
        public bool StrictlySimple { get; set; }
            Guarantees that no polygon touches or overlaps another at a vertex.
            Default false, because enforcing it is comparatively expensive.
        public bool PreserveCollinear { get; set; }   //inherited

    Execute overloads
        public bool Execute(ClipType clipType,
                            List<List<IntPoint>> solution,
                            PolyFillType FillType = PolyFillType.pftEvenOdd)
        public bool Execute(ClipType clipType, PolyTree polytree,
                            PolyFillType FillType = PolyFillType.pftEvenOdd)
        public bool Execute(ClipType clipType,
                            List<List<IntPoint>> solution,
                            PolyFillType subjFillType,
                            PolyFillType clipFillType)
        public bool Execute(ClipType clipType, PolyTree polytree,
                            PolyFillType subjFillType,
                            PolyFillType clipFillType)

    The two-fill-type overloads are the primitives; the single-fill-type
    overloads forward to them, passing the one value for both. The solution
    collection (list or tree) is CLEARED before it is populated, so an
    already-populated container is replaced, not appended to. `Execute` returns
    false rather than throwing when the same instance is already inside an
    `Execute` call. The `List<List<IntPoint>>` overloads THROW
    `PolyClipException("Error: PolyTree struct is needed for open path
    clipping.")` when any open subject path was added - use a `PolyTree`
    overload in that case.

    Static helpers (all on `PolyClip`)
        public static void ReversePaths(List<List<IntPoint>> polys)
        public static bool Orientation(List<IntPoint> poly)
        public static double Area(List<IntPoint> poly)
        public static int PointInPolygon(IntPoint pt, List<IntPoint> path)
            Returns 0 outside, -1 on an edge or vertex, +1 inside.
        public static List<List<IntPoint>> SimplifyPolygon(
            List<IntPoint> poly,
            PolyFillType fillType = PolyFillType.pftEvenOdd)
        public static List<List<IntPoint>> SimplifyPolygons(
            List<List<IntPoint>> polys,
            PolyFillType fillType = PolyFillType.pftEvenOdd)
        public static List<IntPoint> CleanPolygon(
            List<IntPoint> path, double distance = 1.415)
        public static List<List<IntPoint>> CleanPolygons(
            List<List<IntPoint>> polys, double distance = 1.415)
        public static List<List<IntPoint>> MinkowskiSum(
            List<IntPoint> pattern, List<IntPoint> path, bool pathIsClosed)
        public static List<List<IntPoint>> MinkowskiSum(
            List<IntPoint> pattern, List<List<IntPoint>> paths,
            bool pathIsClosed)
        public static List<List<IntPoint>> MinkowskiDiff(
            List<IntPoint> poly1, List<IntPoint> poly2)
        public static List<List<IntPoint>> PolyTreeToPaths(PolyTree polytree)
        public static List<List<IntPoint>> OpenPathsFromPolyTree(
            PolyTree polytree)
        public static List<List<IntPoint>> ClosedPathsFromPolyTree(
            PolyTree polytree)

  * `Area` is signed: negative for a path wound the other way. Take
    `Math.Abs(...)` when you want magnitude.
  * `SimplifyPolygon(s)` internally runs a union with `StrictlySimple` set, so
    it both removes self-intersections and merges overlapping regions.
  * `CleanPolygon(s)` removes vertices that are closer together than
    `distance` (the default 1.415 is just over the diagonal of a unit cell) and
    vertices that are effectively collinear. It is a cleanup pass, not a
    simplification pass.
  * The three PolyTree flattening helpers are how you get a flat list back out
    of a tree: `PolyTreeToPaths` returns everything,
    `ClosedPathsFromPolyTree` only the closed polygons, and
    `OpenPathsFromPolyTree` only the open paths.

POLYCLIPOFFSET
--------------

`PolyClipOffset` is the offsetting (inflate/deflate) engine. It does NOT derive
from `PolyClipBase`, so it has its own `AddPath` / `AddPaths` / `Clear` with
different signatures.

    public PolyClipOffset(double miterLimit = 2.0, double arcTolerance = 0.25)

    Properties
        public double MiterLimit { get; set; }
            The limit beyond which a mitered join is squared off instead,
            expressed as a multiple of the offset delta. Default 2.0.
        public double ArcTolerance { get; set; }
            The maximum distance by which a rounded join may deviate from the
            true arc. Default 0.25. Smaller values produce more segments.

    Methods
        public void AddPath(List<IntPoint> path, JoinType joinType,
                            EndType endType)
        public void AddPaths(List<List<IntPoint>> paths, JoinType joinType,
                             EndType endType)
        public void Clear()
        public void Execute(ref List<List<IntPoint>> solution, double delta)
        public void Execute(ref PolyTree solution, double delta)

  * BOTH `Execute` overloads take their solution BY REF, and both clear it
    first. The `ref` is a quirk inherited from upstream; you still pass a
    constructed instance, and the same instance comes back populated.
  * `delta` is in the same integer units as the coordinates. Positive inflates
    a closed polygon, negative deflates it. For the `etOpen*` end types the
    magnitude is the half-width of the resulting ribbon.
  * `AddPath` throws `PolyClipException` when an open end style is combined
    with a closed path (and vice versa) or when a coordinate exceeds the
    supported range - but see the pitfall below: its range checking is weaker
    than `PolyClipBase.AddPath`'s.
  * `Clear()` removes every added path and returns the instance to its initial
    state, keeping `MiterLimit` and `ArcTolerance`.
  * Offsetting assumes its input is already simple. Run self-intersecting
    input through `PolyClip.SimplifyPolygons` first.
  * `ArcTolerance` only matters for `jtRound` joins and `etOpenRound` /
    `etClosedLine` ends. `MiterLimit` only matters for `jtMiter`.

POLYCLIPEXCEPTION
-----------------

`PolyClipException` is declared with NO access modifier, so it is internal to
the assembly - matching upstream, where the type is declared the same way. You
cannot write `catch (PolyClipException)` in your own code; catch
`System.Exception` (or inspect `ex.GetType().Name` if you must distinguish it)
and read `ex.Message`, which carries the upstream diagnostic strings quoted
above.


COMPLETE EXAMPLES
=================

Example 1 -- union of two overlapping squares
---------------------------------------------

    using System;
    using System.Collections.Generic;
    using CodeBrix.PolygonTools;
    using CodeBrix.PolygonTools.Enumerations;
    using CodeBrix.PolygonTools.Models;

    var subject = new List<IntPoint>
    {
        new IntPoint(0, 0), new IntPoint(100, 0),
        new IntPoint(100, 100), new IntPoint(0, 100)
    };

    var clip = new List<IntPoint>
    {
        new IntPoint(50, 50), new IntPoint(150, 50),
        new IntPoint(150, 150), new IntPoint(50, 150)
    };

    var solution = new List<List<IntPoint>>();

    var polyClip = new PolyClip();
    polyClip.AddPath(subject, PolyType.ptSubject, true);
    polyClip.AddPath(clip, PolyType.ptClip, true);
    var ok = polyClip.Execute(ClipType.ctUnion, solution,
                              PolyFillType.pftNonZero, PolyFillType.pftNonZero);

    Console.WriteLine($"{ok} - {solution.Count} path(s), "
        + $"area {Math.Abs(PolyClip.Area(solution[0]))}");
    //True - 1 path(s), area 17500
    //(two 100x100 squares, 10000 + 10000, less the 2500 they share)

Do not assume a particular starting vertex or winding direction in the
solution: the engine emits the ring it computed, and only the SET of vertices
and the enclosed area are contractual. Normalize yourself if you need a
canonical order.

Example 2 -- intersection, difference and xor from one instance
---------------------------------------------------------------

    var polyClip = new PolyClip();
    var result = new List<List<IntPoint>>();

    foreach (var op in new[] { ClipType.ctIntersection, ClipType.ctDifference,
                               ClipType.ctXor })
    {
        polyClip.Clear();                       //MANDATORY between operations
        polyClip.AddPath(subject, PolyType.ptSubject, true);
        polyClip.AddPath(clip, PolyType.ptClip, true);
        polyClip.Execute(op, result, PolyFillType.pftNonZero,
                         PolyFillType.pftNonZero);

        Console.WriteLine($"{op}: {result.Count} path(s)");
    }

Example 3 -- floating-point geometry, scaled and unscaled
----------------------------------------------------------

This is the round trip you need whenever your source geometry is `double`,
`float`, `PointF`, `SKPoint` or similar. Pick a scale, convert, clip, convert
back.

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeBrix.PolygonTools;
    using CodeBrix.PolygonTools.Enumerations;
    using CodeBrix.PolygonTools.Models;

    const double Scale = 1000.0;   //keeps three decimal places

    static List<IntPoint> ToIntPath(IEnumerable<(double X, double Y)> pts) =>
        pts.Select(p => new IntPoint(p.X * Scale, p.Y * Scale)).ToList();

    static List<(double X, double Y)> ToDoublePath(List<IntPoint> path) =>
        path.Select(p => (p.X / Scale, p.Y / Scale)).ToList();

    var subjectF = new[] { (0.0, 0.0), (10.5, 0.0), (10.5, 10.5), (0.0, 10.5) };
    var clipF    = new[] { (5.25, 5.25), (15.0, 5.25), (15.0, 15.0),
                           (5.25, 15.0) };

    var polyClip = new PolyClip();
    polyClip.AddPath(ToIntPath(subjectF), PolyType.ptSubject, true);
    polyClip.AddPath(ToIntPath(clipF), PolyType.ptClip, true);

    var scaled = new List<List<IntPoint>>();
    polyClip.Execute(ClipType.ctIntersection, scaled,
                     PolyFillType.pftNonZero, PolyFillType.pftNonZero);

    foreach (var path in scaled)
    {
        foreach (var (x, y) in ToDoublePath(path))
        {
            Console.WriteLine($"({x}, {y})");
        }
    }
    //the 5.25 x 5.25 overlap square, as the four points
    //(5.25, 5.25) (10.5, 5.25) (10.5, 10.5) (5.25, 10.5) in some rotation

    //Sanity check: the scaled area is Scale*Scale times the real area, so
    //divide by Scale*Scale to get back to source units.
    var realArea = Math.Abs(PolyClip.Area(scaled[0])) / (Scale * Scale);
    Console.WriteLine(realArea);     //27.5625

Note the choice of scale. `Scale` must be large enough for your precision and
small enough that `maxCoordinate * Scale` stays inside `loRange`
(1,073,741,823). With a scale of 1,000 that allows source coordinates up to
about +/-1,073,741.

Example 4 -- inflating and deflating a polygon
-----------------------------------------------

    var offset = new PolyClipOffset();          //miterLimit 2.0, arcTol 0.25
    offset.AddPath(subject, JoinType.jtMiter, EndType.etClosedPolygon);

    var inflated = new List<List<IntPoint>>();
    offset.Execute(ref inflated, 10.0);         //grow by 10 units

    offset.Clear();                             //MANDATORY before reuse
    offset.AddPath(subject, JoinType.jtRound, EndType.etClosedPolygon);

    var deflated = new List<List<IntPoint>>();
    offset.Execute(ref deflated, -10.0);        //shrink by 10 units

    Console.WriteLine($"{inflated.Count} / {deflated.Count}");

Deflating far enough makes a polygon disappear: the solution comes back with
zero paths rather than throwing. Always check `solution.Count` before indexing.

Example 5 -- offsetting an open polyline into a ribbon
-------------------------------------------------------

    var polyline = new List<IntPoint>
    {
        new IntPoint(0, 0), new IntPoint(100, 0), new IntPoint(100, 100)
    };

    var offset = new PolyClipOffset();
    offset.AddPath(polyline, JoinType.jtRound, EndType.etOpenRound);

    var ribbon = new List<List<IntPoint>>();
    offset.Execute(ref ribbon, 5.0);   //a 10-unit-wide stroke with round caps

Swap `EndType.etOpenButt` for flat caps and `EndType.etOpenSquare` for caps
that project half the width past each end. This is how you convert a stroked
line into a fillable polygon.

Example 6 -- holes and nesting with PolyTree
---------------------------------------------

    var outer = new List<IntPoint>
    {
        new IntPoint(0, 0), new IntPoint(300, 0),
        new IntPoint(300, 300), new IntPoint(0, 300)
    };
    var hole = new List<IntPoint>
    {
        new IntPoint(100, 100), new IntPoint(200, 100),
        new IntPoint(200, 200), new IntPoint(100, 200)
    };

    var polyClip = new PolyClip();
    polyClip.AddPath(outer, PolyType.ptSubject, true);
    polyClip.AddPath(hole, PolyType.ptClip, true);

    var tree = new PolyTree();
    polyClip.Execute(ClipType.ctDifference, tree,
                     PolyFillType.pftNonZero, PolyFillType.pftNonZero);

    Console.WriteLine($"nodes: {tree.Total}");

    for (var node = tree.GetFirst(); node != null; node = node.GetNext())
    {
        var kind = node.IsOpen ? "open" : (node.IsHole ? "hole" : "outer");
        Console.WriteLine($"  {kind}, {node.Contour.Count} vertices, "
            + $"{node.ChildCount} child(ren)");
    }

    //Or flatten the tree instead of walking it:
    var closed = PolyClip.ClosedPathsFromPolyTree(tree);

Recursion over `Childs` works too, and is easier when you care about depth:

    static void Walk(PolyNode node, int depth)
    {
        Console.WriteLine(new string(' ', depth * 2)
            + (node.IsHole ? "hole" : "outer"));
        foreach (var child in node.Childs) { Walk(child, depth + 1); }
    }
    foreach (var child in tree.Childs) { Walk(child, 0); }

Example 7 -- clipping an open path (polyline) against a polygon
----------------------------------------------------------------

Open subject paths REQUIRE a `PolyTree` solution.

    var line = new List<IntPoint>
    {
        new IntPoint(-50, 50), new IntPoint(350, 50)
    };

    var polyClip = new PolyClip();
    polyClip.AddPath(line, PolyType.ptSubject, false);   //Closed: false
    polyClip.AddPath(outer, PolyType.ptClip, true);

    var tree = new PolyTree();
    polyClip.Execute(ClipType.ctIntersection, tree,
                     PolyFillType.pftNonZero, PolyFillType.pftNonZero);

    var segments = PolyClip.OpenPathsFromPolyTree(tree);
    //segments holds the portion(s) of the line that fall inside the polygon

Passing a `List<List<IntPoint>>` here throws instead:
"Error: PolyTree struct is needed for open path clipping."

Example 8 -- cleaning up self-intersecting input before offsetting
------------------------------------------------------------------

    var bowtie = new List<IntPoint>
    {
        new IntPoint(0, 0), new IntPoint(100, 100),
        new IntPoint(100, 0), new IntPoint(0, 100)
    };

    var simple = PolyClip.SimplifyPolygon(bowtie, PolyFillType.pftNonZero);
    var cleaned = PolyClip.CleanPolygons(simple);      //default distance 1.415

    var offset = new PolyClipOffset();
    offset.AddPaths(cleaned, JoinType.jtMiter, EndType.etClosedPolygon);

    var grown = new List<List<IntPoint>>();
    offset.Execute(ref grown, 5.0);

Example 9 -- bounds, orientation, area and hit-testing
-------------------------------------------------------

    var paths = new List<List<IntPoint>> { outer, hole };

    IntRect bounds = PolyClipBase.GetBounds(paths);
    Console.WriteLine($"{bounds.left},{bounds.top} .. "
        + $"{bounds.right},{bounds.bottom}   "
        + $"{bounds.right - bounds.left} x {bounds.bottom - bounds.top}");

    Console.WriteLine(PolyClip.Orientation(outer));       //winding direction
    Console.WriteLine(PolyClip.Area(outer));              //signed area

    Console.WriteLine(PolyClip.PointInPolygon(new IntPoint(150, 150), outer));
    //  1 = inside, 0 = outside, -1 = exactly on an edge or vertex

    PolyClip.ReversePaths(paths);                         //in place

Example 10 -- Minkowski sum: sweeping a shape along a path
-----------------------------------------------------------

    var brush = new List<IntPoint>
    {
        new IntPoint(-5, -5), new IntPoint(5, -5),
        new IntPoint(5, 5), new IntPoint(-5, 5)
    };
    var stroke = new List<IntPoint>
    {
        new IntPoint(0, 0), new IntPoint(200, 0), new IntPoint(200, 200)
    };

    var swept = PolyClip.MinkowskiSum(brush, stroke, false);  //open path
    var union = PolyClip.SimplifyPolygons(swept, PolyFillType.pftNonZero);

`MinkowskiDiff(poly1, poly2)` gives the difference (erosion) form; it takes two
paths and no closed/open flag.


MINIMUM VIABLE PROJECT
======================

MyPolygonApp.csproj

    <Project Sdk="Microsoft.NET.Sdk">

      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
        <ImplicitUsings>disable</ImplicitUsings>
      </PropertyGroup>

      <ItemGroup>
        <PackageReference Include="CodeBrix.PolygonTools.MitLicenseForever"
                          Version="*" />
      </ItemGroup>

    </Project>

(Replace `Version="*"` with the actual version your restore resolves; a
floating version is shown here only because this document does not pin
versions.)

Program.cs

    using System;
    using System.Collections.Generic;
    using CodeBrix.PolygonTools;
    using CodeBrix.PolygonTools.Enumerations;
    using CodeBrix.PolygonTools.Models;

    namespace MyPolygonApp;

    internal static class Program
    {
        private static void Main()
        {
            var subject = new List<IntPoint>
            {
                new IntPoint(0, 0), new IntPoint(100, 0),
                new IntPoint(100, 100), new IntPoint(0, 100)
            };
            var clip = new List<IntPoint>
            {
                new IntPoint(50, 50), new IntPoint(150, 50),
                new IntPoint(150, 150), new IntPoint(50, 150)
            };

            var polyClip = new PolyClip();
            polyClip.AddPath(subject, PolyType.ptSubject, true);
            polyClip.AddPath(clip, PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();
            polyClip.Execute(ClipType.ctIntersection, solution,
                             PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            foreach (var path in solution)
            {
                Console.WriteLine(string.Join(" ",
                    path.ConvertAll(p => $"({p.X},{p.Y})")));
            }
        }
    }

    //Output: the four corners of the 50x50 overlap square -
    //(50,50) (100,50) (100,100) (50,100), in some rotation

`dotnet run` prints the overlap square. Nothing else is required - no
initialization call, no configuration file, no native dependency.


PERFORMANCE TIPS
================

  * Keep coordinates inside `loRange` (0x3FFFFFFF). Below it the engine uses
    ordinary 64-bit arithmetic; above it, it switches to wide arithmetic for
    every slope and intersection comparison, which is substantially slower.
    Choose the smallest scale factor that preserves the precision you need.
  * Leave `StrictlySimple` off unless you actually need the guarantee. The
    library's own documentation calls it "comparatively expensive", and it is
    off by default for that reason.
  * Reuse one `PolyClip` instance across many operations, calling `Clear()`
    between them, rather than constructing a new one each time. Same for
    `PolyClipOffset`.
  * Batch with `AddPaths` instead of looping over `AddPath` where you already
    have a `List<List<IntPoint>>` - it is the same work, but one call.
  * Cost scales with the number of edges and with the number of intersections
    between them (the sweep is roughly O((n + k) log n) for n edges and k
    intersections). Pre-reducing vertex counts with `CleanPolygons` before a
    heavy clip usually pays for itself; do not run it on already-minimal
    geometry.
  * Increase `ArcTolerance` when offsetting with `jtRound` if the arcs are
    finer than your output device can show. The default 0.25 is measured in the
    same integer units as your coordinates, so after scaling up by 1,000 it
    produces 1,000x more segments than it would unscaled. Scale
    `ArcTolerance` along with your coordinates.
  * Prefer `jtMiter` or `jtSquare` over `jtRound` when the join style is not
    visually important - round joins emit many more vertices.
  * The `List<List<IntPoint>>` overloads of `Execute` are cheaper than the
    `PolyTree` overloads, which additionally build the parent/child structure.
    Only ask for a tree when you need the nesting (or when open paths force
    it).
  * `IntPoint` is a struct, so paths are contiguous value storage with no
    per-vertex allocation. Pre-size your `List<IntPoint>` with the capacity
    constructor when you know the vertex count.
  * Neither engine is thread-safe. Give each thread its own instance. The
    static helpers on `PolyClip` hold no shared mutable state - the ones that
    need an engine (`SimplifyPolygon(s)`, `MinkowskiSum`, `MinkowskiDiff`)
    allocate a private `PolyClip` per call - so they are safe to call
    concurrently.


COMMON PITFALLS TO AVOID
========================

  * Coordinates are INTEGERS. Scale floating-point input up before converting,
    and scale results back down afterwards. Forgetting this collapses your
    geometry to a handful of lattice points and produces an empty or nonsense
    result. Coordinate magnitudes must not exceed `hiRange`, and results are
    only guaranteed free of rounding artifacts within `loRange`.
  * Call `Clear()` between operations. An instance retains its paths
    otherwise, and the next `Execute` silently includes them. This is the most
    common bug in code that uses this library.
  * For a union, add the new geometry as `ptClip` and the existing geometry as
    `ptSubject`. The library's own documentation is easy to misread on this
    point.
  * `PolyClipOffset.Execute` takes its solution parameter BY REF. Passing an
    already-populated list does not append - the list is cleared and replaced.
    The same is true of `PolyClip.Execute`, which clears its (non-ref)
    solution container.
  * Open paths require a `PolyTree` solution. The `List<List<IntPoint>>`
    overloads throw when any open subject path was added.
  * Range validation is asymmetric. `PolyClipBase.AddPath` rejects coordinates
    beyond `hiRange`, but `PolyClipOffset.AddPath` performs no equivalent range
    check of its own. This is upstream behaviour and is preserved here;
    validate offset input yourself if it may be out of range.
  * `AddPath` returns `false` for degenerate input instead of throwing. If you
    ignore the return value, a mistyped path simply vanishes from the result.
  * Do not repeat the first vertex at the end of a closed path. The library
    closes rings implicitly; a repeated vertex is stripped as a duplicate at
    best and skews `CleanPolygon` at worst.
  * `PolyClipException` is not public. `catch (PolyClipException)` will not
    compile in your code - catch `System.Exception`.
  * `IntRect` fields are lower-case (`left`, `top`, `right`, `bottom`) and
    `PolyNode.Childs` is spelled without the "r". Both are verbatim upstream
    names, not typos to be corrected.
  * `PolyClip.Area` is SIGNED. Comparing it directly to an expected magnitude
    fails for clockwise input; use `Math.Abs`.
  * `PolyNode.Contour` hands back the engine's live list, not a copy. Mutating
    it while walking the tree corrupts the walk.
  * An offset that shrinks a shape out of existence returns an empty solution
    rather than throwing. Check `Count` before indexing.
  * The default fill rule on the single-fill-type `Execute` overloads is
    `pftEvenOdd`, which ignores orientation. If you are relying on winding to
    distinguish holes from outer rings, pass `pftNonZero` explicitly.
  * Offsetting assumes simple (non-self-intersecting) input. Feed
    self-intersecting geometry through `PolyClip.SimplifyPolygons` first or the
    offset result will be wrong in ways that are hard to spot.
  * `MiterLimit` and `ArcTolerance` are set on the instance, not per path, and
    they survive `Clear()`. Reset them explicitly if a later operation needs
    different values.
  * Wide-integer arithmetic uses `System.Int128`, NOT the upstream `Int128`
    helper, which was removed because its negation silently overflowed at
    `long.MinValue`. See THIRD-PARTY-NOTICES.txt for the rationale. This is a
    behaviour improvement, not a compatibility break, but it is worth knowing
    if you are porting code that referenced the upstream helper directly.


WHAT THIS PACKAGE DOES NOT DO
=============================

  * No rendering. It produces coordinate lists; drawing them is your job.
    There is no dependency on SkiaSharp, System.Drawing, WPF or any other
    graphics stack, and no conversion helper to their point types.
  * No floating-point coordinate API. Everything is `long`. You do the scaling
    (see COORDINATE MODEL AND SCALING).
  * No curves. Beziers and arcs must be flattened to line segments before they
    reach this library, and come back flattened.
  * No 3D, no meshes, no triangulation, no convex hull, no Delaunay, no
    Voronoi, no polygon decomposition into convex parts.
  * No boolean operations on anything but polygons and polylines - no regions,
    no rasters, no distance fields.
  * No SVG, DXF, WKT or GeoJSON parsing or writing. Bring your own I/O.
  * No geographic/geodetic awareness: coordinates are plain Cartesian integers,
    with no projection, datum or spherical geometry.
  * No async API and no cancellation. Every operation is synchronous and runs
    to completion; a very large clip will block its thread.
  * No thread safety on an engine instance, and no internal parallelism.
  * No `IDisposable`, no finalizers, no unmanaged resources. Let instances go
    out of scope.
  * No public exception type, no `Span<T>`/`Memory<T>` overloads, and no
    `IEnumerable<IntPoint>` overloads - `List<>` is the currency throughout.
  * This is the Clipper 1.x (Clipper6) algorithm. It is not Clipper2, and it
    does not expose Clipper2's API shape or its `Rect64`/`PathD` types.


WORKING EXAMPLES ON GITHUB
==========================

The test suite is the executable specification for everything above - roughly
120 tests, one behaviour per test, all named for the behaviour they lock down.

  https://github.com/ellisnet/CodeBrix.PolygonTools/tree/main/tests/CodeBrix.PolygonTools.Tests

  PolyClipTests.cs
      The four boolean operations against known-good areas, all four filling
      rules, `ReverseSolution` / `StrictlySimple` / `PreserveCollinear` and the
      `ioXxx` constructor options, solution-container clearing, the
      already-executing false return, and every static helper:
      `Orientation`, `Area`, `PointInPolygon`, `ReversePaths`,
      `SimplifyPolygon(s)`, `CleanPolygon(s)`, `MinkowskiSum`,
      `MinkowskiDiff`, and the three PolyTree flattening helpers.
  PolyClipBaseTests.cs
      `AddPath` / `AddPaths` acceptance and rejection, the degenerate-path
      false return, the open-path-must-be-subject exception, coordinate range
      validation against `hiRange`, `Clear()` semantics, `PreserveCollinear`,
      and `GetBounds`.
  PolyClipOffsetTests.cs
      Inflating and deflating with each `JoinType`, every `EndType` including
      the open-polyline ribbon cases, `MiterLimit` and `ArcTolerance` effects,
      the by-ref solution overloads for both list and tree, `Clear()` reuse,
      and shrink-to-nothing.
  PolyTreeTests.cs
      Nesting and hole detection, `GetFirst()` / `GetNext()` walking,
      `Childs` / `ChildCount` / `Parent` / `IsHole` / `IsOpen` / `Contour`,
      `Total` (including the negative-offset hidden-outer case), `Clear()`,
      and open-path solutions.
  IntPointTests.cs
      All three constructors, the `double` constructor's truncation
      (`IntPoint_constructor_truncates_floating_point_coordinates`), `==` /
      `!=` / `Equals` / `GetHashCode`, and default-struct behaviour.
  IntRectTests.cs
      Both constructors and the four public fields.
  DoublePointTests.cs
      The default-argument constructor and both copy/convert constructors.
  PolygonFactory.cs
      Not a test - the shared geometry helper the suite builds its squares,
      rectangles, reversed squares and self-intersecting bow-ties with, plus
      an absolute-area sum used for assertions. Useful as a compact worked
      example of constructing paths.

The two sample snippets in the package README (union of two squares, growing a
polygon by 10 units) are smaller versions of Examples 1 and 4 above.


QUICK REFERENCE CARD
====================

    Package     CodeBrix.PolygonTools.MitLicenseForever
    Assembly    CodeBrix.PolygonTools.dll   Target  .NET 10 or later
    License     MIT AND BSL-1.0             Deps    none

    using CodeBrix.PolygonTools;
    using CodeBrix.PolygonTools.Enumerations;
    using CodeBrix.PolygonTools.Models;

    path      List<IntPoint>              paths   List<List<IntPoint>>

    CLIP
      var c = new PolyClip();                     //or new PolyClip(ioXxx | ..)
      c.AddPath(path, PolyType.ptSubject, true);  //true == closed
      c.AddPaths(paths, PolyType.ptClip, true);
      c.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero,
                PolyFillType.pftNonZero);
      c.Clear();                                  //before reuse

    OFFSET
      var o = new PolyClipOffset(miterLimit: 2.0, arcTolerance: 0.25);
      o.AddPath(path, JoinType.jtMiter, EndType.etClosedPolygon);
      o.Execute(ref solution, 10.0);              //negative delta shrinks
      o.Clear();

    TREE
      var t = new PolyTree();
      c.Execute(ClipType.ctDifference, t, pftNonZero, pftNonZero);
      for (var n = t.GetFirst(); n != null; n = n.GetNext()) { ... }
      n.Contour  n.Childs  n.ChildCount  n.Parent  n.IsHole  n.IsOpen
      t.Total    t.Clear()
      PolyClip.PolyTreeToPaths / ClosedPathsFromPolyTree /
      OpenPathsFromPolyTree

    ENUMS
      ClipType      ctIntersection ctUnion ctDifference ctXor
      PolyType      ptSubject ptClip
      PolyFillType  pftEvenOdd(default) pftNonZero pftPositive pftNegative
      JoinType      jtSquare jtRound jtMiter
      EndType       etClosedPolygon etClosedLine etOpenButt etOpenSquare
                    etOpenRound

    STATIC HELPERS (on PolyClip)
      ReversePaths(paths)             Orientation(path) -> bool
      Area(path) -> double (signed)   PointInPolygon(pt, path) -> 0/1/-1
      SimplifyPolygon(s)(.., fillType = pftEvenOdd)
      CleanPolygon(s)(.., distance = 1.415)
      MinkowskiSum(pattern, path|paths, pathIsClosed)
      MinkowskiDiff(poly1, poly2)
      PolyClipBase.GetBounds(paths) -> IntRect (left/top/right/bottom)

    RANGE
      PolyClip.loRange = 0x3FFFFFFF              (fast, artifact-free)
      PolyClip.hiRange = 0x3FFFFFFFFFFFFFFFL     (hard limit; AddPath throws)

    THE FIVE RULES
      1. Integers only - scale doubles up, scale results back down.
      2. Clear() between operations.
      3. Open paths need a PolyTree solution.
      4. PolyClipOffset.Execute takes solution by ref, and clears it.
      5. Simplify before you offset.


================================================================================
END OF AGENT-README
================================================================================
