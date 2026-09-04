# CodeBrix.PolygonTools

A fully managed, cross-platform 2D polygon clipping and offsetting library for .NET.
CodeBrix.PolygonTools performs the four boolean set operations - intersection, union, difference and exclusive-or - on arbitrary sets of polygons and open paths, and inflates or deflates polygons with miter, round or square joins.
It operates on 64-bit integer coordinates so that its arithmetic is exact and its results are robust, and it correctly handles self-intersecting polygons, holes, and polygons with coincident or collinear edges.
CodeBrix.PolygonTools has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.PolygonTools.MitLicenseForever` NuGet package.

CodeBrix.PolygonTools supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.PolygonTools.MitLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.PolygonTools`:

* NuGet package ID: `CodeBrix.PolygonTools.MitLicenseForever`
* Assembly and primary namespace: `CodeBrix.PolygonTools` - i.e. `using CodeBrix.PolygonTools;`

XML documentation (IntelliSense) ships alongside the assembly.

The package has no NuGet dependencies at all - nothing beyond .NET itself is pulled in.

## CodeBrix.PolygonTools supports:

* All four boolean clipping operations on sets of polygons - intersection, union, difference and exclusive-or
* Four filling rules - even-odd, non-zero, positive and negative - so the library interoperates with GDI+, Cairo, OpenGL, SVG and similar rendering models
* Polygon offsetting (inflating and deflating) with miter, round and square joins, and butt, square or round line ends
* Open-path (polyline) clipping as well as closed-polygon clipping
* Self-intersecting polygons, holes, and polygons with coincident or collinear edges
* Results returned either as a flat list of paths or as a `PolyTree` that preserves the parent/child nesting of outer polygons and their holes
* Supporting operations including `SimplifyPolygon`, `CleanPolygon`, `Orientation`, `Area`, `PointInPolygon`, `MinkowskiSum` and `MinkowskiDiff`
* Exact 64-bit integer arithmetic, with no floating-point rounding artifacts in the clipping result

## Sample Code

### Union of two overlapping squares

```csharp
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;

var subject = new List<IntPoint>
{
    new IntPoint(0, 0), new IntPoint(100, 0), new IntPoint(100, 100), new IntPoint(0, 100)
};

var clip = new List<IntPoint>
{
    new IntPoint(50, 50), new IntPoint(150, 50), new IntPoint(150, 150), new IntPoint(50, 150)
};

var solution = new List<List<IntPoint>>();

var polyClip = new PolyClip();
polyClip.AddPath(subject, PolyType.ptSubject, true);
polyClip.AddPath(clip, PolyType.ptClip, true);
polyClip.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

//solution now holds the single L-shaped polygon covering both squares
```

### Growing a polygon by 10 units

```csharp
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;

var offset = new PolyClipOffset();
offset.AddPath(subject, JoinType.jtMiter, EndType.etClosedPolygon);

var inflated = new List<List<IntPoint>>();
offset.Execute(ref inflated, 10.0);

//pass a negative delta to Execute in order to shrink the polygon instead
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.PolygonTools.Tests` project:
https://github.com/ellisnet/CodeBrix.PolygonTools/tree/main/tests/CodeBrix.PolygonTools.Tests

## License

CodeBrix.PolygonTools is licensed under the MIT License, and portions of it remain subject to the
Boost Software License 1.0 - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.PolygonTools/blob/main/LICENSE) file, which carries both
notices, and [license-boost.txt](https://github.com/ellisnet/CodeBrix.PolygonTools/blob/main/license-boost.txt)
for the full Boost licence text.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.PolygonTools/blob/main/THIRD-PARTY-NOTICES.txt).
