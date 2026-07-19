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
/// A floating-point coordinate pair, used internally while offsetting to hold edge
/// normals and other intermediate values that cannot be expressed exactly as integers.
/// </summary>
public struct DoublePoint
{
  /// <summary>
  /// The X coordinate.
  /// </summary>
  public double X;
  /// <summary>
  /// The Y coordinate.
  /// </summary>
  public double Y;

  /// <summary>
  /// Initializes a new <see cref="DoublePoint"/> with the supplied coordinates.
  /// </summary>
  /// <param name="x">The X coordinate. Defaults to zero.</param>
  /// <param name="y">The Y coordinate. Defaults to zero.</param>
  public DoublePoint(double x = 0, double y = 0)
  {
    this.X = x; this.Y = y;
  }
  /// <summary>
  /// Initializes a new <see cref="DoublePoint"/> that is a copy of another one.
  /// </summary>
  /// <param name="dp">The point to copy.</param>
  public DoublePoint(DoublePoint dp)
  {
    this.X = dp.X; this.Y = dp.Y;
  }
  /// <summary>
  /// Initializes a new <see cref="DoublePoint"/> from an integer point, widening both
  /// coordinates to <see cref="double"/>.
  /// </summary>
  /// <param name="ip">The integer point to convert.</param>
  public DoublePoint(IntPoint ip)
  {
    this.X = ip.X; this.Y = ip.Y;
  }
};
