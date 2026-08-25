================================================================================
MAINTAINER-README: CodeBrix.PolygonTools
Notes for people and agents MAINTAINING this repository — not for package consumers
================================================================================

If you are consuming the NuGet package, stop reading and open AGENT-README.txt
instead. This file is about the repository itself.


PURPOSE AND SCOPE
=================

This repository produces exactly one NuGet package:

  Package id:   CodeBrix.PolygonTools.MitLicenseForever
  Assembly:     CodeBrix.PolygonTools.dll
  Project:      src/CodeBrix.PolygonTools/CodeBrix.PolygonTools.csproj
  Consumer doc: AGENT-README.txt (repo root)

One package, one AGENT-README, no sub-packages and no per-project
AGENT-README files.


REPOSITORY LAYOUT
=================

  CodeBrix.PolygonTools.slnx          solution (Solution Items + Tests folders)
  AGENT-README.txt                    consumer documentation; SHIPS in the
                                      nupkg
  MAINTAINER-README.txt               this file
  EXTRAS-README.txt                   non-package content in this repository
  README-INDEX.txt                    map of the README files
  README.md                           human-facing overview; SHIPS in the nupkg
  LICENSE                             MIT, for the CodeBrix additions and the
                                      combined work
  license-boost.txt                   Boost Software License 1.0, for the
                                      derived code; SHIPS in the nupkg
  THIRD-PARTY-NOTICES.txt             provenance and the full modification
                                      record; SHIPS in the nupkg
  icon-codebrix-128.png               package icon; SHIPS in the nupkg
  src/CodeBrix.PolygonTools/          the one packable project
  tests/CodeBrix.PolygonTools.Tests/  the one test project

Inside src/CodeBrix.PolygonTools/:

  PolyClip.cs               the clipping engine (the largest file by far)
  PolyClipBase.cs           path input, edge-list construction, bounds
  PolyClipOffset.cs         the offsetting engine
  PolyClipException.cs      internal exception type
  InternalsVisibleTo.cs     grants access to CodeBrix.PolygonTools.Tests
  Enumerations/             ClipType, PolyType, PolyFillType, JoinType, EndType
  Models/                   IntPoint, IntRect, DoublePoint, PolyNode, PolyTree,
                            IntersectNode, MyIntersectNodeSort
  Internal/                 TEdge, OutRec, OutPt, Join, Scanbeam, Maxima,
                            LocalMinima, EdgeSide, Direction

The folder layout follows the CodeBrix family convention: entry types at the
project root, everything else in a sub-folder whose name matches the trailing
namespace segment.


ARCHITECTURE
============

The algorithm is a sweep-line (scanbeam) implementation. PolyClipBase turns the
input paths into a linked structure of TEdge records grouped into local minima.
PolyClip sweeps a horizontal scanbeam upward through those edges, maintaining
an active edge list, resolving intersections in order, and emitting
OutRec/OutPt output polygons. PolyClipOffset is independent of the sweep: it
builds offset geometry from edge normals and then runs a union through PolyClip
to resolve the self-intersections that offsetting creates.

PolyClipBase.RangeTest is the mechanism behind the loRange/hiRange split. It
starts in narrow mode and, on the first coordinate beyond loRange, flips
m_UseFullRange and re-tests against hiRange; from then on the slope and
intersection comparisons in PolyClipBase (PointOnLineSegment and the three
SlopesEqual overloads) take the wide-arithmetic branch.

There is no mutable static state anywhere in the library, which is why the
static helpers on PolyClip are concurrency-safe even though an engine instance
is not.


BUILDING
========

  dotnet build CodeBrix.PolygonTools.slnx

Requires the .NET 10 SDK. The library targets net10.0 only - there is no
multi-targeting and no netstandard target. There are no native dependencies, no
code generators and no pre-build steps.

GenerateDocumentationFile is on, so CS1591 (missing XML comment on a public
member) is a build warning. Fix it at the source by writing the doc comment;
never suppress it. The project sets no NoWarn and no WarningLevel override, and
#pragma warning disable is not used anywhere in the repository - keep it that
way.

GeneratePackageOnBuild is true, so EVERY build - Debug or Release - also writes
a .nupkg into the project's bin/<Configuration>/ folder. That is intentional
but it means build output accumulates; see PACKAGING AND PUBLISHING below for
what that implies about versioning.


TESTING
=======

  dotnet test CodeBrix.PolygonTools.slnx

The suite is xUnit v3 with SilverAssertions, roughly 120 tests. It needs no
test-data files, no environment variables, no opt-in switches and no network
access. Nothing is written to the working tree while it runs. Every test is
deterministic.

Test conventions in this repository:

  * One test file per class under test, named <ClassUnderTest>Tests.cs.
  * Test methods are either <MemberName>_snake_case_description or plain
    snake_case.
  * Multi-statement test bodies carry //Arrange //Act //Assert comments;
    single-statement bodies are expression-bodied.
  * Prefer x.Should().Be(y) (SilverAssertions) over Assert.Equal(y, x).
  * Pass TestContext.Current.CancellationToken to any cancellable call inside a
    test (xUnit1051). Nothing in this library is cancellable, so in practice
    this rule only bites if a test starts doing I/O.
  * The library's InternalsVisibleTo.cs grants the test assembly access to
    internal types, so internal helpers can be tested directly.

UpstreamInt128Tests is the differential suite that justified replacing the
upstream Int128 helper with System.Int128. It asserts that both implementations
reach the same equality decision over every combination of 20 boundary values
(160,000 comparisons, including long.MinValue, where the upstream negation
silently overflows) plus 1,000,000 seeded random products, and cross-checks
System.Int128 against BigInteger as an independent oracle. Its Random seed is
FIXED so that a failure is always reproducible - do not make it time-based, and
do not delete UpstreamInt128Reference.cs, which is the oracle it compares
against.


PACKAGING AND PUBLISHING
========================

Pack is driven by the csproj alone; there is no pack script, no .nuspec and no
Directory.Build.props in this repository.

  dotnet pack src/CodeBrix.PolygonTools/CodeBrix.PolygonTools.csproj -c Release

(and, because GeneratePackageOnBuild is true, a plain build produces one too).

What ships inside the nupkg, from the csproj ItemGroup:

  icon-codebrix-128.png     PackageIcon
  README.md                 PackageReadmeFile
  AGENT-README.txt          consumer documentation for AI coding agents
  THIRD-PARTY-NOTICES.txt   provenance and modification record
  license-boost.txt         the Boost Software License 1.0 text

MAINTAINER-README.txt, EXTRAS-README.txt and README-INDEX.txt are repository
content and are NOT packed - only AGENT-README.txt is.

PackageLicenseExpression is "MIT AND BSL-1.0" and
PackageRequireLicenseAcceptance is true. Both halves of that expression matter:
the MIT half covers the CodeBrix additions and the combined work, the BSL-1.0
half covers the derived Clipper code. Do not simplify the expression to plain
MIT.

Versioning is the CodeBrix date-stamped scheme, computed in the csproj from
System.DateTime.UtcNow:

  1.<years since _VersionBaseYear>.<day of year>.<minute of day>

  * major is pinned to 1;
  * minor is whole years since _VersionBaseYear (currently 2026);
  * build is the 1-based UTC day of year;
  * revision is the UTC minute of day, 0..1439, floored.

The value is therefore strictly increasing over time, it is the same for a
local build and a CI build in the same UTC minute, and it is NOT SemVer -
major/minor do not signal API compatibility. Two builds inside one UTC minute
produce the same version, so never publish two packages from within a single
minute. To re-baseline the minor number, change _VersionBaseYear.

Publishing is done by hand from the produced .nupkg. Tag the repository with
the published version so that the latest git tag and the latest nuget.org
version agree - the family compliance check looks for that.


PROVENANCE AND VENDORED SOURCES
===============================

Substantially all of the production source is derived from the Clipper library
("polyclipping") by Angus Johnson, version 6.4.2 dated 27 February 2017 - the
final 1.x release - obtained as clipper_ver6.4.2.zip, file
C#/clipper_library/clipper.cs. That code remains under the Boost Software
License 1.0.

THIRD-PARTY-NOTICES.txt is the authoritative record and lists every derived
file plus the seven categories of modification (type renames, file split,
removal of the use_lines/use_int32/use_xyz conditional compilation, the
System.Int128 substitution, added XML doc comments, the net10.0 retarget, and
C# 14 syntax modernization). Read it before changing anything under src/.

Rules for maintaining the vendored code:

  * Every derived file carries the upstream copyright/license/attribution
    header verbatim and a "//was previously: ClipperLib;" provenance comment on
    its namespace declaration. Keep both.
  * Only five identifiers were renamed (Clipper -> PolyClip, ClipperBase ->
    PolyClipBase, ClipperOffset -> PolyClipOffset, ClipperException ->
    PolyClipException, and the namespace). Do NOT rename anything else. Enum
    member names, IntRect's lower-case fields and PolyNode.Childs are upstream
    names and are part of the public API; "fixing" them is a breaking change.
  * Occurrences of "Clipper" in upstream COMMENTS were deliberately left
    intact. Leave them.
  * If you update the vendored code against a newer upstream, update
    THIRD-PARTY-NOTICES.txt in the same change and re-run the differential
    Int128 suite.


CODING CONVENTIONS
==================

These CodeBrix family conventions apply to every file in this repository.

  * Target framework is net10.0 only. No multi-targeting.
  * Nullable reference types are OFF. Never write `?` on a reference type
    (`string?`, `MyClass?`, `object?`) and never use the null-forgiving `!`
    operator. Value-type nullables (`int?`, `double?`, `MyEnum?`) are fine.
  * No `global using` directives and no `#nullable enable` anywhere.
  * No `<ImplicitUsings>`. Every using is written out explicitly.
  * File-scoped namespaces only (`namespace X;`), never block-scoped.
  * All using directives sit above the namespace declaration, in one contiguous
    block, System.* first and alphabetical within each group, with aliases
    last.
  * GenerateDocumentationFile is on. Every public type and member carries an
    XML doc comment. CS1591 is fixed at source - never suppressed.
  * No project-level warning suppression: no <NoWarn>, no <WarningLevel>0</>,
    no #pragma warning disable.
  * When you change a line of vendored code, comment out the original with a
    `//was previously:` note rather than deleting it, wherever that stays
    readable.

Note that the vendored files do not follow CodeBrix brace and indentation style
- they carry upstream's two-space indentation and Allman-with-exceptions
bracing. That is deliberate: keeping them close to upstream makes diffing
against a future Clipper release possible. New, non-derived files should follow
the family style.


NOTES
=====

  * The eight AI-agent pointer stubs (AGENTS.md, CLAUDE.md, .clinerules,
    .cursorrules, .cursor/rules/agent-readme.mdc, .windsurfrules,
    .github/copilot-instructions.md, .junie/guidelines.md) all point at
    README-INDEX.txt. They are maintained centrally across the CodeBrix family
    - do not edit them here.
  * PolyClipException is declared with no access modifier, matching upstream,
    so it is internal to the assembly. Consumers cannot catch it by type. That
    is a known wart inherited from upstream; making it public would be a
    deliberate divergence and would need a note in THIRD-PARTY-NOTICES.txt.
  * PolyClipOffset.AddPath does not range-check coordinates the way
    PolyClipBase.AddPath does. This asymmetry is upstream behaviour and is
    preserved on purpose; it is documented as a pitfall in AGENT-README.txt.
  * The solution file must keep its "Solution Items" folder (carrying
    AGENT-README.txt, LICENSE, license-boost.txt, README.md,
    THIRD-PARTY-NOTICES.txt and the icon) and its "Tests" folder holding the
    test project - the family compliance check verifies both.


================================================================================
END OF MAINTAINER-README
================================================================================
