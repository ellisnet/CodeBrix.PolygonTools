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

using CodeBrix.PolygonTools.Enumerations;
using System.Collections.Generic;
using Path = System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace CodeBrix.PolygonTools.Models; //was previously: ClipperLib;

/// <summary>
/// A single node in a <see cref="PolyTree"/>, holding one contour together with its
/// parent and its child contours.
/// </summary>
/// <remarks>
/// The nesting expresses containment: the children of an outer polygon are its holes,
/// and the children of a hole are the outer polygons contained within it.
/// </remarks>
public class PolyNode 
{
    internal PolyNode m_Parent;
    internal Path m_polygon = [];
    internal int m_Index;
    internal JoinType m_jointype;
    internal EndType m_endtype;
    internal List<PolyNode> m_Childs = [];

    private bool IsHoleNode()
    {
        var result = true;
        var node = m_Parent;
        while (node != null)
        {
            result = !result;
            node = node.m_Parent;
        }
        return result;
    }

    /// <summary>
    /// Gets the number of child nodes belonging to this node.
    /// </summary>
    public int ChildCount => m_Childs.Count;

    /// <summary>
    /// Gets the path that this node represents.
    /// </summary>
    public Path Contour => m_polygon;

    internal void AddChild(PolyNode Child)
    {
        var cnt = m_Childs.Count;
        m_Childs.Add(Child);
        Child.m_Parent = this;
        Child.m_Index = cnt;
    }

    /// <summary>
    /// Gets the next node in the tree, visiting the first child when this node has children
    /// and otherwise moving on to the next sibling of this node or of an ancestor.
    /// </summary>
    /// <returns>The next node in the traversal, or <c>null</c> once the tree is exhausted.</returns>
    public PolyNode GetNext() => (m_Childs.Count > 0) 
        ? m_Childs[0] 
        : GetNextSiblingUp();

    internal PolyNode GetNextSiblingUp() =>
        m_Parent switch
        {
            null => null,
            _ => (m_Index == m_Parent.m_Childs.Count - 1) 
                ? m_Parent.GetNextSiblingUp() 
                : m_Parent.m_Childs[m_Index + 1]
        };

    /// <summary>
    /// Gets the child nodes of this node.
    /// </summary>
    public List<PolyNode> Childs => m_Childs;

    /// <summary>
    /// Gets the node that contains this one, or <c>null</c> for the root of the tree.
    /// </summary>
    public PolyNode Parent => m_Parent;

    /// <summary>
    /// Gets a value indicating whether this node represents a hole rather than an outer
    /// polygon. Holes and outer polygons alternate with each level of nesting.
    /// </summary>
    public bool IsHole => IsHoleNode();

    /// <summary>
    /// Gets a value indicating whether this node represents an open path rather than a
    /// closed polygon.
    /// </summary>
    public bool IsOpen { get; set; }
}
