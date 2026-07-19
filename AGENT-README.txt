================================================================================
                      AGENT-README: CodeBrix.PolygonTools
                   A Comprehensive Guide for AI Coding Agents
================================================================================


OVERVIEW
--------------------------------------------------------------------------------

CodeBrix.PolygonTools is a fully managed, cross-platform 2D polygon clipping and
offsetting library for .NET. It performs the four boolean set operations -
intersection, union, difference and exclusive-or - on arbitrary sets of polygons
and open paths, and it inflates or deflates polygons with miter, round or square
joins. It operates on 64-bit integer coordinates so that its arithmetic is exact
and its results are robust, and it correctly handles self-intersecting polygons,
holes, and polygons with coincident or collinear edges.

The library has no dependencies other than .NET itself.


INSTALLATION
--------------------------------------------------------------------------------

NuGet package:  CodeBrix.PolygonTools.MitLicenseForever

    dotnet add package CodeBrix.PolygonTools.MitLicenseForever

Note that the NuGet package id carries the ".MitLicenseForever" suffix but the
assembly and the namespaces do NOT - they are simply CodeBrix.PolygonTools. The
suffix is a CodeBrix family convention that records the license the package will
always be published under.

Target framework: .NET 10.0 or higher. The library targets net10.0 only.


KEY NAMESPACES
--------------------------------------------------------------------------------

    using CodeBrix.PolygonTools;                //PolyClip, PolyClipOffset, PolyClipBase
    using CodeBrix.PolygonTools.Enumerations;   //ClipType, PolyType, PolyFillType,
                                                //JoinType, EndType
    using CodeBrix.PolygonTools.Models;         //IntPoint, IntRect, DoublePoint,
                                                //PolyNode, PolyTree, IntersectNode

A fourth namespace, CodeBrix.PolygonTools.Internal, holds the implementation
detail of the clipping algorithm (TEdge, OutRec, OutPt, Scanbeam and friends).
Everything in it is internal and is not part of the public API.

There is no "Path" or "Paths" type in the public API. A path is simply a
List<IntPoint>, and a set of paths is a List<List<IntPoint>>. The source files
use file-level `using Path = ...` / `using Paths = ...` aliases for brevity, but
those aliases are compile-time only and never appear in a public signature.


CORE API REFERENCE
--------------------------------------------------------------------------------

PolyClip : PolyClipBase
    The clipping engine. Typical sequence:

        var polyClip = new PolyClip();
        polyClip.AddPath(subject, PolyType.ptSubject, true);
        polyClip.AddPaths(clips, PolyType.ptClip, true);
        polyClip.Execute(ClipType.ctUnion, solution,
                         PolyFillType.pftNonZero, PolyFillType.pftNonZero);
        polyClip.Clear();   //before reusing the instance

    Constructor
        PolyClip(int InitOptions = 0)
            InitOptions is a bitwise OR of the constants ioReverseSolution,
            ioStrictlySimple and ioPreserveCollinear.

    Properties
        ReverseSolution     bool   - reverse the orientation of solution paths
        StrictlySimple      bool   - no polygon touches or overlaps another at a
                                     vertex; comparatively expensive
        PreserveCollinear   bool   - inherited; keep collinear vertices

    Execute overloads
        bool Execute(ClipType, List<List<IntPoint>> solution,
                     PolyFillType fillType = pftEvenOdd)
        bool Execute(ClipType, PolyTree polytree,
                     PolyFillType fillType = pftEvenOdd)
        bool Execute(ClipType, List<List<IntPoint>> solution,
                     PolyFillType subjFillType, PolyFillType clipFillType)
        bool Execute(ClipType, PolyTree polytree,
                     PolyFillType subjFillType, PolyFillType clipFillType)

        The solution collection is cleared before it is populated. Execute
        returns false rather than throwing when the instance is already
        executing.

    Static helpers
        void   ReversePaths(List<List<IntPoint>>)
        bool   Orientation(List<IntPoint>)
        double Area(List<IntPoint>)
        int    PointInPolygon(IntPoint, List<IntPoint>)   //0 out, 1 in, -1 on edge
        List<List<IntPoint>> SimplifyPolygon(List<IntPoint>, PolyFillType)
        List<List<IntPoint>> SimplifyPolygons(List<List<IntPoint>>, PolyFillType)
        List<IntPoint>       CleanPolygon(List<IntPoint>, double distance = 1.415)
        List<List<IntPoint>> CleanPolygons(List<List<IntPoint>>, double = 1.415)
        List<List<IntPoint>> MinkowskiSum(pattern, path, bool pathIsClosed)
        List<List<IntPoint>> MinkowskiSum(pattern, paths, bool pathIsClosed)
        List<List<IntPoint>> MinkowskiDiff(poly1, poly2)
        List<List<IntPoint>> PolyTreeToPaths(PolyTree)
        List<List<IntPoint>> OpenPathsFromPolyTree(PolyTree)
        List<List<IntPoint>> ClosedPathsFromPolyTree(PolyTree)

PolyClipBase
    The base class of PolyClip. Not used directly, but it carries the members
    that add geometry:

        bool AddPath(List<IntPoint> pg, PolyType polyType, bool Closed)
        bool AddPaths(List<List<IntPoint>> ppg, PolyType polyType, bool closed)
        virtual void Clear()
        static IntRect GetBounds(List<List<IntPoint>> paths)
        const long loRange = 0x3FFFFFFF
        const long hiRange = 0x3FFFFFFFFFFFFFFF

    AddPath returns false - it does not throw - when a path has too few distinct
    vertices to contribute to the result. It throws PolyClipException when an
    open path is supplied as clip geometry, or when a coordinate exceeds hiRange.

PolyClipOffset
    The offsetting engine. Typical sequence:

        var offset = new PolyClipOffset();   //miterLimit 2.0, arcTolerance 0.25
        offset.AddPaths(polys, JoinType.jtMiter, EndType.etClosedPolygon);
        offset.Execute(ref solution, 10.0);  //negative delta shrinks
        offset.Clear();

    Note that both Execute overloads take their solution by ref:

        void Execute(ref List<List<IntPoint>> solution, double delta)
        void Execute(ref PolyTree solution, double delta)

    Offsetting assumes its input is already simple. Run self-intersecting input
    through PolyClip.SimplifyPolygons first.

PolyTree / PolyNode
    A PolyTree preserves the parent/child nesting of the solution: the children
    of an outer polygon are its holes, and the children of a hole are the outer
    polygons inside it. Walk it with GetFirst() and PolyNode.GetNext(). Use a
    PolyTree overload of Execute whenever the subject contains open paths - the
    flat-list overloads throw PolyClipException in that case.

Enumerations (upstream member names are retained verbatim, Delphi-flavoured
prefixes included)
    ClipType      ctIntersection, ctUnion, ctDifference, ctXor
    PolyType      ptSubject, ptClip
    PolyFillType  pftEvenOdd, pftNonZero, pftPositive, pftNegative
    JoinType      jtSquare, jtRound, jtMiter
    EndType       etClosedPolygon, etClosedLine, etOpenButt, etOpenSquare,
                  etOpenRound

PolyClipException
    Internal, not public - matching upstream, where the type is declared with no
    access modifier. Consumers catch it as System.Exception.


COMMON PITFALLS
--------------------------------------------------------------------------------

* Coordinates are integers. Scale floating-point input up before converting, and
  scale results back down afterwards. Coordinate magnitudes must not exceed
  hiRange, and results are only guaranteed free of rounding artifacts within
  loRange.
* Call Clear() between operations. An instance retains its paths otherwise, and
  the next Execute silently includes them.
* For a union, add the new geometry as ptClip and the existing geometry as
  ptSubject. The library's own documentation is easy to misread on this point.
* PolyClipOffset.Execute takes its solution parameter by ref. Passing an
  already-populated list does not append - the list is replaced.
* Open paths require a PolyTree solution. The List<List<IntPoint>> overloads
  throw when any open subject path was added.
* Range validation is asymmetric. PolyClipBase.AddPath rejects coordinates
  beyond hiRange, but PolyClipOffset.AddPath performs no range check of its own.
  This is upstream behaviour and is preserved here; validate offset input
  yourself if it may be out of range.
* Wide-integer arithmetic uses System.Int128, NOT the upstream Int128 helper,
  which was removed - see THIRD-PARTY-NOTICES.txt for the rationale. A verbatim
  copy of the upstream helper survives in the test project as UpstreamInt128,
  purely as the oracle for the differential test that justified the swap. Never
  reference it from the library.


CODING CONVENTIONS (CodeBrix family)
--------------------------------------------------------------------------------

These conventions apply to every file in this repository. Follow them when
making changes.

* Target framework is net10.0 only. No multi-targeting.
* Nullable reference types are OFF. Never write `?` on a reference type
  (`string?`, `MyClass?`, `object?`) and never use the null-forgiveness `!`
  operator. Value-type nullables (`int?`, `double?`, `MyEnum?`) are fine.
* No `global using` directives and no `#nullable enable` anywhere.
* No `<ImplicitUsings>`. Every using is written out explicitly.
* File-scoped namespaces only (`namespace X;`), never block-scoped.
* All using directives sit above the namespace declaration, in one contiguous
  block, System.* first and alphabetical within each group, with aliases last.
* <GenerateDocumentationFile> is on. Every public type and member carries an XML
  doc comment. CS1591 is fixed at source - never suppressed.
* No project-level warning suppression: no <NoWarn>, no <WarningLevel>0</>, no
  #pragma warning disable.
* Tests are xUnit v3 with SilverAssertions. Prefer `x.Should().Be(y)` over
  `Assert.Equal(y, x)`.
* Test classes are named <ClassUnderTest>Tests. Test methods are either
  <MemberName>_snake_case_description or plain snake_case.
* Multi-statement test bodies carry //Arrange //Act //Assert comments;
  single-statement test bodies are expression-bodied.
* Pass TestContext.Current.CancellationToken to any cancellable call inside a
  test (xUnit1051).


ARCHITECTURE
--------------------------------------------------------------------------------

src/CodeBrix.PolygonTools/
    PolyClip.cs               the clipping engine (the largest file by far)
    PolyClipBase.cs           path input, edge-list construction, bounds
    PolyClipOffset.cs         the offsetting engine
    PolyClipException.cs      internal exception type
    InternalsVisibleTo.cs     grants access to CodeBrix.PolygonTools.Tests
    Enumerations/             ClipType, PolyType, PolyFillType, JoinType, EndType
    Models/                   IntPoint, IntRect, DoublePoint, PolyNode, PolyTree,
                              IntersectNode, MyIntersectNodeSort
    Internal/                 TEdge, OutRec, OutPt, Join, Scanbeam,
                              Maxima, LocalMinima, EdgeSide, Direction

The algorithm is a sweep-line (scanbeam) implementation. PolyClipBase turns the
input paths into a linked structure of TEdge records grouped into local minima.
PolyClip sweeps a horizontal scanbeam upward through those edges, maintaining an
active edge list, resolving intersections in order, and emitting OutRec/OutPt
output polygons. PolyClipOffset is independent of the sweep: it builds offset
geometry from edge normals and then runs a union through PolyClip to resolve the
self-intersections that offsetting creates.


TESTING
--------------------------------------------------------------------------------

    dotnet test CodeBrix.PolygonTools.slnx

Tests live in tests/CodeBrix.PolygonTools.Tests/ and are organized one file per
class under test, plus a geometry helper used across the suite. They cover the
four boolean operations against known-good areas, the filling rules, offsetting
in both directions with each join style, PolyTree nesting and hole detection,
open-path handling, the static helpers, argument and range validation, and the
value semantics of IntPoint and IntRect.

UpstreamInt128Tests is the differential suite that justified replacing the
upstream Int128 helper with System.Int128. It asserts that both implementations
reach the same equality decision over every combination of 20 boundary values
(160,000 comparisons, including long.MinValue, where the upstream negation
silently overflows) plus 1,000,000 seeded random products, and cross-checks
System.Int128 against BigInteger as an independent oracle. Its Random seed is
fixed so a failure is always reproducible - do not make it time-based.


================================================================================
END OF AGENT-README
================================================================================
