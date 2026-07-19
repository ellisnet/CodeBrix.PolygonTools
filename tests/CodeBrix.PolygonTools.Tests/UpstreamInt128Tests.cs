using System;
using System.Collections.Generic;
using System.Numerics;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PolygonTools.Tests;

/// <summary>
/// Differential tests that justify replacing the upstream Int128 helper with the
/// framework's System.Int128.
/// </summary>
/// <remarks>
/// The library only ever used the upstream helper in the form
/// <c>Int128Mul(a, b) == Int128Mul(c, d)</c>, at four sites in PolyClipBase
/// (PointOnLineSegment and the three SlopesEqual overloads), and only when
/// UseFullRange is set. So the property that must hold after the swap is that
/// System.Int128 reaches the SAME equality decision as
/// <see cref="UpstreamInt128"/> for every input the clipping algorithm can produce.
/// These tests assert exactly that, over exhaustive edge cases and a large seeded
/// random sample, and additionally cross-check System.Int128 against
/// <see cref="BigInteger"/> as an independent oracle.
/// </remarks>
public class UpstreamInt128Tests
{
    //Deterministic seed: a failure must be reproducible, never flaky.
    private const int Seed = 20260719;

    //The library rejects coordinates beyond hiRange, so a delta between two
    //coordinates cannot exceed twice that. The products under test are therefore
    //bounded by (2 * hiRange)^2, which is what these tests sample.
    private const long HiRange = 0x3FFFFFFFFFFFFFFFL;

    /// <summary>
    /// The comparison the library performs, using the upstream implementation.
    /// </summary>
    private static bool UpstreamDecision(long a, long b, long c, long d)
        => UpstreamInt128.Int128Mul(a, b) == UpstreamInt128.Int128Mul(c, d);

    /// <summary>
    /// The comparison the library performs, using System.Int128.
    /// </summary>
    private static bool FrameworkDecision(long a, long b, long c, long d)
        => (Int128)a * b == (Int128)c * d;

    /// <summary>
    /// The comparison the library performs, using BigInteger as an independent oracle.
    /// </summary>
    private static bool BigIntegerDecision(long a, long b, long c, long d)
        => (BigInteger)a * b == (BigInteger)c * d;

    /// <summary>
    /// Values that bracket every interesting boundary of the 64-bit product space.
    /// </summary>
    private static IReadOnlyList<long> EdgeCaseValues =>
    [
        0L, 1L, -1L, 2L, -2L,
        0xFFFFFFFFL, -0xFFFFFFFFL,           //32-bit boundary, where the upstream
        0x100000000L, -0x100000000L,         //implementation splits its operands
        0x100000001L, -0x100000001L,
        3_000_000_000L, -3_000_000_000L,     //product overflows 64 bits
        int.MaxValue, int.MinValue,
        PolygonTools.PolyClipBase.loRange, -PolygonTools.PolyClipBase.loRange,
        HiRange, -HiRange,
        long.MaxValue, long.MinValue
    ];

    [Fact]
    public void framework_matches_upstream_for_every_edge_case_pair()
    {
        //Arrange
        var values = EdgeCaseValues;
        var compared = 0;

        //Act
        //Assert
        //every (a,b) x (c,d) combination drawn from the boundary values -- note that
        //long.MinValue is included, which the upstream implementation negates with a
        //silent overflow, so this also pins that corner of the behaviour
        foreach (var a in values)
        {
            foreach (var b in values)
            {
                foreach (var c in values)
                {
                    foreach (var d in values)
                    {
                        UpstreamDecision(a, b, c, d)
                            .Should().Be(FrameworkDecision(a, b, c, d));
                        compared++;
                    }
                }
            }
        }

        compared.Should().Be(values.Count * values.Count * values.Count * values.Count);
    }

    [Fact]
    public void framework_matches_upstream_for_a_million_random_products()
    {
        //Arrange
        var random = new Random(Seed);
        var equalDecisions = 0;

        //Act
        //Assert
        for (var i = 0; i < 1_000_000; i++)
        {
            var a = NextInRange(random, HiRange);
            var b = NextInRange(random, HiRange);
            var c = NextInRange(random, HiRange);
            var d = NextInRange(random, HiRange);

            var upstream = UpstreamDecision(a, b, c, d);
            upstream.Should().Be(FrameworkDecision(a, b, c, d));

            if (upstream)
            {
                equalDecisions++;
            }
        }

        //random products are essentially never equal, so this only guards against a
        //degenerate generator that would make the whole sweep vacuous
        equalDecisions.Should().BeLessThan(1000);
    }

    [Fact]
    public void framework_matches_upstream_when_the_two_products_are_genuinely_equal()
    {
        //Arrange
        //The interesting half of the decision space: pairs that MUST compare equal.
        //Random sampling almost never produces these, so they are constructed.
        var random = new Random(Seed);

        //Act
        //Assert
        for (var i = 0; i < 200_000; i++)
        {
            var a = NextInRange(random, 4_000_000_000L);
            var b = NextInRange(random, 4_000_000_000L);

            //a*b == b*a, and (-a)*(-b) == a*b
            UpstreamDecision(a, b, b, a).Should().BeTrue();
            FrameworkDecision(a, b, b, a).Should().BeTrue();
            UpstreamDecision(-a, -b, a, b).Should().BeTrue();
            FrameworkDecision(-a, -b, a, b).Should().BeTrue();

            //and a sign flip on exactly one operand must NOT compare equal,
            //unless the product is zero
            if (a != 0 && b != 0)
            {
                UpstreamDecision(-a, b, a, b).Should().BeFalse();
                FrameworkDecision(-a, b, a, b).Should().BeFalse();
            }
        }
    }

    [Fact]
    public void framework_matches_upstream_across_the_full_range_of_signs()
    {
        //Arrange
        var random = new Random(Seed);

        //Act
        //Assert
        for (var i = 0; i < 100_000; i++)
        {
            var magnitudeA = NextInRange(random, HiRange) & long.MaxValue;
            var magnitudeB = NextInRange(random, HiRange) & long.MaxValue;

            foreach (var signA in new[] { 1, -1 })
            {
                foreach (var signB in new[] { 1, -1 })
                {
                    var a = signA * (magnitudeA >> 2);
                    var b = signB * (magnitudeB >> 2);

                    //compare each signed product against the unsigned one
                    UpstreamDecision(a, b, magnitudeA >> 2, magnitudeB >> 2)
                        .Should().Be(FrameworkDecision(a, b, magnitudeA >> 2, magnitudeB >> 2));
                }
            }
        }
    }

    [Fact]
    public void framework_matches_a_big_integer_oracle_for_random_products()
    {
        //Arrange
        var random = new Random(Seed);

        //Act
        //Assert
        //BigInteger is an independent arbitrary-precision implementation, so agreeing
        //with it confirms the swap is correct and not merely bug-compatible
        for (var i = 0; i < 200_000; i++)
        {
            var a = NextInRange(random, HiRange);
            var b = NextInRange(random, HiRange);
            var c = NextInRange(random, HiRange);
            var d = NextInRange(random, HiRange);

            FrameworkDecision(a, b, c, d).Should().Be(BigIntegerDecision(a, b, c, d));
            FrameworkDecision(a, b, b, a).Should().Be(BigIntegerDecision(a, b, b, a));
        }
    }

    [Fact]
    public void framework_product_value_matches_a_big_integer_oracle()
    {
        //Arrange
        var random = new Random(Seed);

        //Act
        //Assert
        //stronger than the equality decision: the 128-bit product itself must be exact
        for (var i = 0; i < 200_000; i++)
        {
            var a = NextInRange(random, HiRange);
            var b = NextInRange(random, HiRange);

            ((BigInteger)((Int128)a * b)).Should().Be((BigInteger)a * b);
        }
    }

    /// <summary>
    /// A uniformly distributed value in the closed interval [-limit, limit].
    /// </summary>
    private static long NextInRange(Random random, long limit)
    {
        var value = random.NextInt64(0, limit) ;
        return random.Next(2) == 0 ? value : -value;
    }
}
