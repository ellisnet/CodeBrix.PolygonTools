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

namespace CodeBrix.PolygonTools.Enumerations; //was previously: ClipperLib;

/// <summary>
/// Specifies the filling rule (winding rule) used to determine which regions bounded by
/// a set of paths are inside the polygon and which are outside.
/// </summary>
/// <remarks>
/// By far the most widely used winding rules for polygon filling are
/// <see cref="pftEvenOdd"/> and <see cref="pftNonZero"/> (GDI, GDI+, XLib, OpenGL, Cairo,
/// AGG, Quartz, SVG, Gr32). Other rules include Positive, Negative and ABS_GTR_EQ_TWO
/// (the last of which is only found in OpenGL).
/// See http://glprogramming.com/red/chapter11.html for a fuller discussion.
/// </remarks>
public enum PolyFillType
{
    /// <summary>
    /// A region is inside the polygon when the number of edge crossings from that region
    /// to infinity is odd.
    /// </summary>
    pftEvenOdd,

    /// <summary>
    /// A region is inside the polygon when its winding number is non-zero.
    /// </summary>
    pftNonZero,

    /// <summary>
    /// A region is inside the polygon when its winding number is greater than zero.
    /// </summary>
    pftPositive,

    /// <summary>
    /// A region is inside the polygon when its winding number is less than zero.
    /// </summary>
    pftNegative
}
