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
/// Specifies the boolean clipping operation to perform on the subject and clip
/// polygons that have been added to a <see cref="PolyClip"/> instance.
/// </summary>
public enum ClipType
{
    /// <summary>
    /// Creates regions covered by both the subject and the clip polygons.
    /// </summary>
    ctIntersection,

    /// <summary>
    /// Creates regions covered by either the subject or the clip polygons, or by both.
    /// </summary>
    ctUnion,

    /// <summary>
    /// Creates regions covered by the subject polygons but not by the clip polygons.
    /// </summary>
    ctDifference,

    /// <summary>
    /// Creates regions covered by the subject or the clip polygons, but not by both.
    /// </summary>
    ctXor
}
