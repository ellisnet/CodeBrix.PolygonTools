# CodeBrix.PolygonTools

A fully managed, cross-platform 2D polygon clipping and offsetting library for .NET.
CodeBrix.PolygonTools performs the four boolean set operations - intersection, union, difference and exclusive-or - on arbitrary sets of polygons and open paths, and inflates or deflates polygons with miter, round or square joins.
It operates on 64-bit integer coordinates so that its arithmetic is exact and its results are robust, and it correctly handles self-intersecting polygons, holes, and polygons with coincident or collinear edges.
CodeBrix.PolygonTools has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.PolygonTools.MitLicenseForever` NuGet package.

CodeBrix.PolygonTools supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

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

## Third-party code and licensing

CodeBrix.PolygonTools is derived from the Clipper library - please see the THIRD-PARTY-NOTICES.txt file in this repository for more information.

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License
