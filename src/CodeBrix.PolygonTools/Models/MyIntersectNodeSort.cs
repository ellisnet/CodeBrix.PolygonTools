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

namespace CodeBrix.PolygonTools.Models; //was previously: ClipperLib;

/// <summary>
/// Orders the intersection nodes found within a scanbeam so that they are processed
/// from the bottom of the scanbeam upwards, which is the order the clipping algorithm
/// requires.
/// </summary>
public class MyIntersectNodeSort : IComparer<IntersectNode>
{
  /// <summary>
  /// Compares two intersection nodes by their vertical position.
  /// </summary>
  /// <param name="node1">The first node to compare.</param>
  /// <param name="node2">The second node to compare.</param>
  /// <returns>
  /// A negative value when <paramref name="node1"/> sorts before <paramref name="node2"/>,
  /// a positive value when it sorts after, and zero when the two are equivalent.
  /// </returns>
  public int Compare(IntersectNode node1, IntersectNode node2)
  {
      ArgumentNullException.ThrowIfNull(node1);
      ArgumentNullException.ThrowIfNull(node2);

      var i = node2.Pt.Y - node1.Pt.Y;
      
      return i switch
      {
          > 0 => 1,
          < 0 => -1,
          _ => 0
      };
  }
}
