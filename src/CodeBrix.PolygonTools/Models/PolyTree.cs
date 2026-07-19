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

using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace CodeBrix.PolygonTools.Models; //was previously: ClipperLib;

/// <summary>
/// The root of a tree of <see cref="PolyNode"/> objects that expresses a clipping result
/// while preserving the parent/child relationships between outer polygons and their holes.
/// </summary>
/// <remarks>
/// Pass an instance to one of the <see cref="PolyClip.Execute(Enumerations.ClipType, PolyTree, Enumerations.PolyFillType)"/>
/// overloads instead of a flat list of paths when that nesting matters, or when the
/// subject contains open paths. Nodes representing open paths are always children of the
/// root node.
/// </remarks>
public class PolyTree : PolyNode
{
    internal List<PolyNode> m_AllPolys = [];

    //The GC probably handles this cleanup more efficiently ...
    //~PolyTree(){Clear();}
      
    /// <summary>
    /// Removes every node from the tree, returning it to its initial empty state.
    /// </summary>
    public void Clear() 
    {
        for (var i = 0; i < m_AllPolys.Count; i++)
        {
            m_AllPolys[i] = null;
        }
        m_AllPolys.Clear(); 
        m_Childs.Clear(); 
    }
      
    /// <summary>
    /// Gets the first node in the tree, from which the whole tree can be walked with
    /// <see cref="PolyNode.GetNext"/>.
    /// </summary>
    /// <returns>The first child of the root node, or <c>null</c> when the tree is empty.</returns>
    public PolyNode GetFirst() => (m_Childs.Count > 0) 
        ? m_Childs[0] 
        : null;

    /// <summary>
    /// Gets the total number of nodes in the tree, excluding the root node itself.
    /// </summary>
    public int Total
    {
        get 
        { 
          var result = m_AllPolys.Count;
          //with negative offsets, ignore the hidden outer polygon ...
          if (result > 0 && m_Childs[0] != m_AllPolys[0]) result--;
          return result;
        }
    }
}
