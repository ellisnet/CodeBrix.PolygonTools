================================================================================
EXTRAS-README: CodeBrix.PolygonTools
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This repository ships no samples, no demo applications and no tools. It
contains exactly one packable project and one test project.


THE TEST PROJECT
================

  Path:  tests/CodeBrix.PolygonTools.Tests/

The unit-test project is the only non-package content in the repository. It is
not packed and not published; it exists to lock down clipping and offsetting
behaviour, and it doubles as the worked-example set that AGENT-README.txt
points consumers at under "WORKING EXAMPLES ON GITHUB".

  dotnet test CodeBrix.PolygonTools.slnx

It needs no test-data files, no environment variables, no opt-in switches and
no network access, and it writes nothing into the working tree. Roughly 120
tests, all deterministic.

Files:

  PolyClipTests.cs
      The four boolean operations, the four filling rules, the constructor
      options and their matching properties, and every static helper.
  PolyClipBaseTests.cs
      Path input, acceptance and rejection, coordinate range validation, Clear
      semantics and GetBounds.
  PolyClipOffsetTests.cs
      Inflating and deflating with each join and end style, MiterLimit and
      ArcTolerance, the by-ref solution overloads, and reuse after Clear.
  PolyTreeTests.cs
      Nesting, hole detection, tree walking and the tree's own members.
  IntPointTests.cs, IntRectTests.cs, DoublePointTests.cs
      Constructors, fields, equality and default-struct behaviour of the three
      value types.
  UpstreamInt128Tests.cs
      The differential suite described below.
  PolygonFactory.cs
      Not a test. The shared geometry helper the suite builds its squares,
      rectangles, reversed squares and self-intersecting bow-ties with, plus an
      absolute-area sum used in assertions. It is `internal static` and lives
      in the test assembly only.
  UpstreamInt128Reference.cs
      Not a test. See below.


THE UPSTREAM INT128 ORACLE
==========================

  Path:  tests/CodeBrix.PolygonTools.Tests/UpstreamInt128Reference.cs

This one file is a verbatim copy of the upstream Clipper Int128 helper struct,
renamed to UpstreamInt128. It is NOT referenced by the library and never has
been. It exists solely as the reference oracle for UpstreamInt128Tests, the
differential test that verified replacing the upstream helper with the
framework's System.Int128 reaches identical results.

Never reference it from library code. Never delete it either - without it the
differential test has nothing to compare against.

See THIRD-PARTY-NOTICES.txt for the full rationale, and MAINTAINER-README.txt
for the rules around it.


WHAT IS NOT IN THIS REPOSITORY
==============================

No samples/, libs/ or tools/ folder, no benchmark project, no optional
test-data set, no scripts and no rendering or visualization harness. bin/ and
obj/ folders that appear after a build are ignored build output, not repository
content - note that because GeneratePackageOnBuild is true, those folders also
accumulate .nupkg files.

The two sample snippets that a reader will find are in README.md (union of two
squares, growing a polygon by 10 units). They are documentation, not a project;
larger versions of both appear in AGENT-README.txt under COMPLETE EXAMPLES.


================================================================================
END OF EXTRAS-README
================================================================================
