/*******************************************************************************
*                                                                              *
* Author    :  Angus Johnson                                                   *
* Version   :  6.4.2                                                           *
* Date      :  27 February 2017                                                *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2017                                         *
*                                                                              *
* License:                                                                     *
* Use, modification & distribution is subject to Boost Software License Ver 1. *
* http://www.boost.org/LICENSE_1_0.txt                                         *
*                                                                              *
* Attributions:                                                                *
* The code in this library is an extension of Bala Vatti's clipping algorithm: *
* "A generic solution to polygon clipping"                                     *
* Communications of the ACM, Vol 35, Issue 7 (July 1992) pp 56-63.             *
* http://portal.acm.org/citation.cfm?id=129906                                 *
*                                                                              *
* Computer graphics and geometric modeling: implementation and algorithms      *
* By Max K. Agoston                                                            *
* Springer; 1 edition (January 4, 2005)                                        *
* http://books.google.com/books?q=vatti+clipping+agoston                       *
*                                                                              *
* See also:                                                                    *
* "Polygon Offsetting by Computing Winding Numbers"                            *
* Paper no. DETC2005-85513 pp. 565-575                                         *
* ASME 2005 International Design Engineering Technical Conferences             *
* and Computers and Information in Engineering Conference (IDETC/CIE2005)      *
* September 24-28, 2005 , Long Beach, California, USA                          *
* http://www.me.berkeley.edu/~mcmains/pubs/DAC05OffsetPolygon.pdf              *
*                                                                              *
*******************************************************************************/

using System;

namespace CodeBrix.PolygonTools.Tests; //was previously: ClipperLib;

/// <summary>
/// A verbatim copy of the upstream Clipper 6.4.2 Int128 helper, retained ONLY as the
/// reference oracle for the differential test that justified replacing it with the
/// framework's System.Int128. It is not referenced by the library and must not be.
/// </summary>
/// <remarks>
/// The only member the library ever used was Int128Mul, and only inside equality
/// comparisons. UpstreamInt128Tests asserts that System.Int128 reaches the same
/// equality decision as this implementation for every input the clipping algorithm
/// can produce. Renamed from Int128 to UpstreamInt128 so it cannot collide with, or
/// be mistaken for, System.Int128.
/// </remarks>
internal struct UpstreamInt128
{
  private Int64 hi;
  private UInt64 lo;

  public UpstreamInt128(Int64 _lo)
  {
    lo = (UInt64)_lo;
    if (_lo < 0) hi = -1;
    else hi = 0;
  }

  public UpstreamInt128(Int64 _hi, UInt64 _lo)
  {
    lo = _lo;
    hi = _hi;
  }

  public UpstreamInt128(UpstreamInt128 val)
  {
    hi = val.hi;
    lo = val.lo;
  }

  public bool IsNegative()
  {
    return hi < 0;
  }

  public static bool operator ==(UpstreamInt128 val1, UpstreamInt128 val2)
  {
    if ((object)val1 == (object)val2) return true;
    else if ((object)val1 == null || (object)val2 == null) return false;
    return (val1.hi == val2.hi && val1.lo == val2.lo);
  }

  public static bool operator !=(UpstreamInt128 val1, UpstreamInt128 val2)
  {
    return !(val1 == val2);
  }

  public override bool Equals(System.Object obj)
  {
    if (obj == null || !(obj is UpstreamInt128))
      return false;
    UpstreamInt128 i128 = (UpstreamInt128)obj;
    return (i128.hi == hi && i128.lo == lo);
  }

  public override int GetHashCode()
  {
    return hi.GetHashCode() ^ lo.GetHashCode();
  }

  public static bool operator >(UpstreamInt128 val1, UpstreamInt128 val2)
  {
    if (val1.hi != val2.hi)
      return val1.hi > val2.hi;
    else
      return val1.lo > val2.lo;
  }

  public static bool operator <(UpstreamInt128 val1, UpstreamInt128 val2)
  {
    if (val1.hi != val2.hi)
      return val1.hi < val2.hi;
    else
      return val1.lo < val2.lo;
  }

  public static UpstreamInt128 operator +(UpstreamInt128 lhs, UpstreamInt128 rhs)
  {
    lhs.hi += rhs.hi;
    lhs.lo += rhs.lo;
    if (lhs.lo < rhs.lo) lhs.hi++;
    return lhs;
  }

  public static UpstreamInt128 operator -(UpstreamInt128 lhs, UpstreamInt128 rhs)
  {
    return lhs + -rhs;
  }

  public static UpstreamInt128 operator -(UpstreamInt128 val)
  {
    if (val.lo == 0)
      return new UpstreamInt128(-val.hi, 0);
    else
      return new UpstreamInt128(~val.hi, ~val.lo + 1);
  }

  public static explicit operator double(UpstreamInt128 val)
  {
    const double shift64 = 18446744073709551616.0; //2^64
    if (val.hi < 0)
    {
      if (val.lo == 0)
        return (double)val.hi * shift64;
      else
        return -(double)(~val.lo + ~val.hi * shift64);
    }
    else
      return (double)(val.lo + val.hi * shift64);
  }

  //nb: Constructing two new Int128 objects every time we want to multiply longs
  //is slow. So, although calling the Int128Mul method doesn't look as clean, the
  //code runs significantly faster than if we'd used the * operator.

  public static UpstreamInt128 Int128Mul(Int64 lhs, Int64 rhs)
  {
    bool negate = (lhs < 0) != (rhs < 0);
    if (lhs < 0) lhs = -lhs;
    if (rhs < 0) rhs = -rhs;
    UInt64 int1Hi = (UInt64)lhs >> 32;
    UInt64 int1Lo = (UInt64)lhs & 0xFFFFFFFF;
    UInt64 int2Hi = (UInt64)rhs >> 32;
    UInt64 int2Lo = (UInt64)rhs & 0xFFFFFFFF;

    //nb: see comments in clipper.pas
    UInt64 a = int1Hi * int2Hi;
    UInt64 b = int1Lo * int2Lo;
    UInt64 c = int1Hi * int2Lo + int1Lo * int2Hi;

    UInt64 lo;
    Int64 hi;
    hi = (Int64)(a + (c >> 32));

    unchecked { lo = (c << 32) + b; }
    if (lo < b) hi++;
    UpstreamInt128 result = new UpstreamInt128(hi, lo);
    return negate ? -result : result;
  }
};
