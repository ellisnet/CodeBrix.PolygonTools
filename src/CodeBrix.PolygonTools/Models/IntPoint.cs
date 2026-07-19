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
using System.Collections.Generic;
using CodeBrix.PolygonTools;
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Internal;
using Path = System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>>;

namespace CodeBrix.PolygonTools.Models; //was previously: ClipperLib;

/// <summary>
/// An integer coordinate pair, and the vertex type from which every path handled by
/// this library is built. A path is a <see cref="System.Collections.Generic.List{T}"/>
/// of <see cref="IntPoint"/>, and a set of paths is a list of those lists.
/// </summary>
/// <remarks>
/// Coordinates are 64-bit integers rather than floating-point values so that the
/// clipping arithmetic is exact and the results are robust. Scale your floating-point
/// coordinates up by a suitable factor before converting them, and scale the results
/// back down afterwards. Coordinate values must not exceed
/// <see cref="PolyClipBase.hiRange"/> in magnitude.
/// </remarks>
public struct IntPoint
{
  /// <summary>
  /// The X coordinate.
  /// </summary>
  public long X;
  /// <summary>
  /// The Y coordinate.
  /// </summary>
  public long Y;
  /// <summary>
  /// Initializes a new <see cref="IntPoint"/> with the supplied coordinates.
  /// </summary>
  /// <param name="X">The X coordinate.</param>
  /// <param name="Y">The Y coordinate.</param>
  public IntPoint(long X, long Y)
  {
      this.X = X; this.Y = Y;
  }
  /// <summary>
  /// Initializes a new <see cref="IntPoint"/> from floating-point coordinates, truncating
  /// each of them toward zero.
  /// </summary>
  /// <param name="x">The X coordinate.</param>
  /// <param name="y">The Y coordinate.</param>
  public IntPoint(double x, double y)
  {
    this.X = (long)x; this.Y = (long)y;
  }

  /// <summary>
  /// Initializes a new <see cref="IntPoint"/> that is a copy of another one.
  /// </summary>
  /// <param name="pt">The point to copy.</param>
  public IntPoint(IntPoint pt)
  {
      this.X = pt.X; this.Y = pt.Y;
  }

  /// <summary>
  /// Determines whether two points have the same coordinates.
  /// </summary>
  /// <param name="a">The first point to compare.</param>
  /// <param name="b">The second point to compare.</param>
  /// <returns><c>true</c> when both coordinates are equal; otherwise <c>false</c>.</returns>
  public static bool operator ==(IntPoint a, IntPoint b)
  {
    return a.X == b.X && a.Y == b.Y;
  }

  /// <summary>
  /// Determines whether two points differ in either coordinate.
  /// </summary>
  /// <param name="a">The first point to compare.</param>
  /// <param name="b">The second point to compare.</param>
  /// <returns><c>true</c> when either coordinate differs; otherwise <c>false</c>.</returns>
  public static bool operator !=(IntPoint a, IntPoint b)
  {
    return a.X != b.X  || a.Y != b.Y; 
  }

  /// <summary>
  /// Determines whether this point is equal to the supplied object.
  /// </summary>
  /// <param name="obj">The object to compare against.</param>
  /// <returns>
  /// <c>true</c> when <paramref name="obj"/> is an <see cref="IntPoint"/> with the same
  /// coordinates; otherwise <c>false</c>.
  /// </returns>
  public override bool Equals(object obj)
  {
    if (obj == null) return false;
    if (obj is IntPoint)
    {
      IntPoint a = (IntPoint)obj;
      return (X == a.X) && (Y == a.Y);
    }
    else return false;
  }

  /// <summary>
  /// Returns a hash code derived from both coordinates.
  /// </summary>
  /// <returns>A hash code for this point.</returns>
  public override int GetHashCode()
  {
    //simply prevents a compiler warning
    return base.GetHashCode();
  }

}// end struct IntPoint
