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
using CodeBrix.PolygonTools.Enumerations;
using CodeBrix.PolygonTools.Models;
using Path = System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>>;

// ReSharper disable RedundantAssignment
// ReSharper disable InconsistentNaming

namespace CodeBrix.PolygonTools; //was previously: ClipperLib;

/// <summary>
/// Offsets - that is, inflates or deflates - the paths that have been added to it, using
/// the join and end styles supplied with each path.
/// </summary>
/// <remarks>
/// The usual sequence is to add paths with <see cref="AddPath"/> or <see cref="AddPaths"/>,
/// call one of the <c>Execute</c> overloads with the required delta, and then call
/// <see cref="Clear"/> before reusing the instance. Self-intersecting paths should be
/// simplified with <see cref="PolyClip.SimplifyPolygons"/> before they are offset, because
/// offsetting assumes its input is already simple.
/// </remarks>
public class PolyClipOffset
{
  private Paths m_destPolys;
  private Path m_srcPoly;
  private Path m_destPoly;
  private List<DoublePoint> m_normals = new List<DoublePoint>();
  private double m_delta, m_sinA, m_sin, m_cos;
  private double m_miterLim, m_StepsPerRad;

  private IntPoint m_lowest;
  private PolyNode m_polyNodes = new PolyNode();

  /// <summary>
  /// Gets or sets the maximum distance by which a rounded join may deviate from the true arc.
  /// Smaller values produce smoother arcs at the cost of more vertices. The default is 0.25.
  /// </summary>
  public double ArcTolerance { get; set; }
  /// <summary>
  /// Gets or sets the limit beyond which a mitered join is squared off instead, expressed as
  /// a multiple of the offset delta. The default is 2.0.
  /// </summary>
  public double MiterLimit { get; set; }

  private const double two_pi = Math.PI * 2;
  private const double def_arc_tolerance = 0.25;

  /// <summary>
  /// Initializes a new <see cref="PolyClipOffset"/> instance.
  /// </summary>
  /// <param name="miterLimit">
  /// The limit beyond which a mitered join is squared off instead, expressed as a multiple
  /// of the offset delta. The default is 2.0.
  /// </param>
  /// <param name="arcTolerance">
  /// The maximum distance by which a rounded join may deviate from the true arc. The default
  /// is 0.25.
  /// </param>
  public PolyClipOffset(
    double miterLimit = 2.0, double arcTolerance = def_arc_tolerance)
  {
    MiterLimit = miterLimit;
    ArcTolerance = arcTolerance;
    m_lowest.X = -1;
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Removes every path that has been added, returning this instance to its initial state so
  /// that it can be reused for another operation.
  /// </summary>
  public void Clear()
  {
    m_polyNodes.Childs.Clear();
    m_lowest.X = -1;
  }
  //------------------------------------------------------------------------------

  internal static long Round(double value)
  {
    return value < 0 ? (long)(value - 0.5) : (long)(value + 0.5);
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Adds a single path to be offset, together with the styles used for its joins and its ends.
  /// </summary>
  /// <param name="path">The path to offset.</param>
  /// <param name="joinType">The style used where two offset segments meet.</param>
  /// <param name="endType">The style used at the ends of the path, which also determines
  /// whether the path is treated as closed or open.</param>
  /// <exception cref="PolyClipException">
  /// An open end style was combined with a closed path, or a coordinate exceeded the
  /// supported range.
  /// </exception>
  public void AddPath(Path path, JoinType joinType, EndType endType)
  {
    var highI = path.Count - 1;
    if (highI < 0) return;
    var newNode = new PolyNode();
    newNode.m_jointype = joinType;
    newNode.m_endtype = endType;

    //strip duplicate points from path and also get index to the lowest point ...
    if (endType == EndType.etClosedLine || endType == EndType.etClosedPolygon)
      while (highI > 0 && path[0] == path[highI]) highI--;
    newNode.m_polygon.Capacity = highI + 1;
    newNode.m_polygon.Add(path[0]);
    int j = 0, k = 0;
    for (var i = 1; i <= highI; i++)
      if (newNode.m_polygon[j] != path[i])
      {
        j++;
        newNode.m_polygon.Add(path[i]);
        if (path[i].Y > newNode.m_polygon[k].Y ||
          (path[i].Y == newNode.m_polygon[k].Y &&
          path[i].X < newNode.m_polygon[k].X)) k = j;
      }
    if (endType == EndType.etClosedPolygon && j < 2) return;

    m_polyNodes.AddChild(newNode);

    //if this path's lowest pt is lower than all the others then update m_lowest
    if (endType != EndType.etClosedPolygon) return;
    if (m_lowest.X < 0)
      m_lowest = new IntPoint(m_polyNodes.ChildCount - 1, k);
    else
    {
      var ip = m_polyNodes.Childs[(int)m_lowest.X].m_polygon[(int)m_lowest.Y];
      if (newNode.m_polygon[k].Y > ip.Y ||
        (newNode.m_polygon[k].Y == ip.Y &&
        newNode.m_polygon[k].X < ip.X))
        m_lowest = new IntPoint(m_polyNodes.ChildCount - 1, k);
    }
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Adds several paths to be offset, together with the styles used for their joins and
  /// their ends.
  /// </summary>
  /// <param name="paths">The paths to offset.</param>
  /// <param name="joinType">The style used where two offset segments meet.</param>
  /// <param name="endType">The style used at the ends of each path, which also determines
  /// whether the paths are treated as closed or open.</param>
  public void AddPaths(Paths paths, JoinType joinType, EndType endType)
  {
    foreach (var p in paths)
      AddPath(p, joinType, endType);
  }
  //------------------------------------------------------------------------------

  private void FixOrientations()
  {
    //fixup orientations of all closed paths if the orientation of the
    //closed path with the lowermost vertex is wrong ...
    if (m_lowest.X >= 0 && 
      !PolyClip.Orientation(m_polyNodes.Childs[(int)m_lowest.X].m_polygon))
    {
      for (var i = 0; i < m_polyNodes.ChildCount; i++)
      {
        var node = m_polyNodes.Childs[i];
        if (node.m_endtype == EndType.etClosedPolygon ||
          (node.m_endtype == EndType.etClosedLine && 
          PolyClip.Orientation(node.m_polygon)))
          node.m_polygon.Reverse();
      }
    }
    else
    {
      for (var i = 0; i < m_polyNodes.ChildCount; i++)
      {
        var node = m_polyNodes.Childs[i];
        if (node.m_endtype == EndType.etClosedLine &&
          !PolyClip.Orientation(node.m_polygon))
        node.m_polygon.Reverse();
      }
    }
  }
  //------------------------------------------------------------------------------

  internal static DoublePoint GetUnitNormal(IntPoint pt1, IntPoint pt2)
  {
    double dx = (pt2.X - pt1.X);
    double dy = (pt2.Y - pt1.Y);
    if ((dx == 0) && (dy == 0)) return new DoublePoint();

    var f = 1 * 1.0 / Math.Sqrt(dx * dx + dy * dy);
    dx *= f;
    dy *= f;

    return new DoublePoint(dy, -dx);
  }
  //------------------------------------------------------------------------------

  private void DoOffset(double delta)
  {
    m_destPolys = new Paths();
    m_delta = delta;

    //if Zero offset, just copy any CLOSED polygons to m_p and return ...
    if (PolyClipBase.near_zero(delta)) 
    {
      m_destPolys.Capacity = m_polyNodes.ChildCount;
      for (var i = 0; i < m_polyNodes.ChildCount; i++)
      {
        var node = m_polyNodes.Childs[i];
        if (node.m_endtype == EndType.etClosedPolygon)
          m_destPolys.Add(node.m_polygon);
      }
      return;
    }

    //see offset_triginometry3.svg in the documentation folder ...
    if (MiterLimit > 2) m_miterLim = 2 / (MiterLimit * MiterLimit);
    else m_miterLim = 0.5;

    double y;
    if (ArcTolerance <= 0.0) 
      y = def_arc_tolerance;
    else if (ArcTolerance > Math.Abs(delta) * def_arc_tolerance)
      y = Math.Abs(delta) * def_arc_tolerance;
    else 
      y = ArcTolerance;
    //see offset_triginometry2.svg in the documentation folder ...
    var steps = Math.PI / Math.Acos(1 - y / Math.Abs(delta));
    m_sin = Math.Sin(two_pi / steps);
    m_cos = Math.Cos(two_pi / steps);
    m_StepsPerRad = steps / two_pi;
    if (delta < 0.0) m_sin = -m_sin;

    m_destPolys.Capacity = m_polyNodes.ChildCount * 2;
    for (var i = 0; i < m_polyNodes.ChildCount; i++)
    {
      var node = m_polyNodes.Childs[i];
      m_srcPoly = node.m_polygon;

      var len = m_srcPoly.Count;

      if (len == 0 || (delta <= 0 && (len < 3 || 
        node.m_endtype != EndType.etClosedPolygon)))
          continue;

      m_destPoly = new Path();

      if (len == 1)
      {
        if (node.m_jointype == JoinType.jtRound)
        {
          double X = 1.0, Y = 0.0;
          for (var j = 1; j <= steps; j++)
          {
            m_destPoly.Add(new IntPoint(
              Round(m_srcPoly[0].X + X * delta),
              Round(m_srcPoly[0].Y + Y * delta)));
            var X2 = X;
            X = X * m_cos - m_sin * Y;
            Y = X2 * m_sin + Y * m_cos;
          }
        }
        else
        {
          double X = -1.0, Y = -1.0;
          for (var j = 0; j < 4; ++j)
          {
            m_destPoly.Add(new IntPoint(
              Round(m_srcPoly[0].X + X * delta),
              Round(m_srcPoly[0].Y + Y * delta)));
            if (X < 0) X = 1;
            else if (Y < 0) Y = 1;
            else X = -1;
          }
        }
        m_destPolys.Add(m_destPoly);
        continue;
      }

      //build m_normals ...
      m_normals.Clear();
      m_normals.Capacity = len;
      for (var j = 0; j < len - 1; j++)
        m_normals.Add(GetUnitNormal(m_srcPoly[j], m_srcPoly[j + 1]));
      if (node.m_endtype == EndType.etClosedLine || 
        node.m_endtype == EndType.etClosedPolygon)
        m_normals.Add(GetUnitNormal(m_srcPoly[len - 1], m_srcPoly[0]));
      else
        m_normals.Add(new DoublePoint(m_normals[len - 2]));

      if (node.m_endtype == EndType.etClosedPolygon)
      {
        var k = len - 1;
        for (var j = 0; j < len; j++)
          OffsetPoint(j, ref k, node.m_jointype);
        m_destPolys.Add(m_destPoly);
      }
      else if (node.m_endtype == EndType.etClosedLine)
      {
        var k = len - 1;
        for (var j = 0; j < len; j++)
          OffsetPoint(j, ref k, node.m_jointype);
        m_destPolys.Add(m_destPoly);
        m_destPoly = new Path();
        //re-build m_normals ...
        var n = m_normals[len - 1];
        for (var j = len - 1; j > 0; j--)
          m_normals[j] = new DoublePoint(-m_normals[j - 1].X, -m_normals[j - 1].Y);
        m_normals[0] = new DoublePoint(-n.X, -n.Y);
        k = 0;
        for (var j = len - 1; j >= 0; j--)
          OffsetPoint(j, ref k, node.m_jointype);
        m_destPolys.Add(m_destPoly);
      }
      else
      {
        var k = 0;
        for (var j = 1; j < len - 1; ++j)
          OffsetPoint(j, ref k, node.m_jointype);

        IntPoint pt1;
        if (node.m_endtype == EndType.etOpenButt)
        {
          var j = len - 1;
          pt1 = new IntPoint(Round(m_srcPoly[j].X + m_normals[j].X *
              delta), Round(m_srcPoly[j].Y + m_normals[j].Y * delta));
          m_destPoly.Add(pt1);
          pt1 = new IntPoint(Round(m_srcPoly[j].X - m_normals[j].X *
              delta), Round(m_srcPoly[j].Y - m_normals[j].Y * delta));
          m_destPoly.Add(pt1);
        }
        else
        {
          var j = len - 1;
          k = len - 2;
          m_sinA = 0;
          m_normals[j] = new DoublePoint(-m_normals[j].X, -m_normals[j].Y);
          if (node.m_endtype == EndType.etOpenSquare)
            DoSquare(j, k);
          else
            DoRound(j, k);
        }

        //re-build m_normals ...
        for (var j = len - 1; j > 0; j--)
          m_normals[j] = new DoublePoint(-m_normals[j - 1].X, -m_normals[j - 1].Y);

        m_normals[0] = new DoublePoint(-m_normals[1].X, -m_normals[1].Y);

        k = len - 1;
        for (var j = k - 1; j > 0; --j)
          OffsetPoint(j, ref k, node.m_jointype);

        if (node.m_endtype == EndType.etOpenButt)
        {
          pt1 = new IntPoint(Round(m_srcPoly[0].X - m_normals[0].X * delta),
            Round(m_srcPoly[0].Y - m_normals[0].Y * delta));
          m_destPoly.Add(pt1);
          pt1 = new IntPoint(Round(m_srcPoly[0].X + m_normals[0].X * delta),
            Round(m_srcPoly[0].Y + m_normals[0].Y * delta));
          m_destPoly.Add(pt1);
        }
        else
        {
          k = 1;
          m_sinA = 0;
          if (node.m_endtype == EndType.etOpenSquare)
            DoSquare(0, 1);
          else
            DoRound(0, 1);
        }
        m_destPolys.Add(m_destPoly);
      }
    }
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Offsets the paths that have been added and returns the result as a flat list of paths.
  /// </summary>
  /// <param name="solution">The list that receives the offset paths. It is cleared first.</param>
  /// <param name="delta">
  /// The distance by which to offset. A positive value inflates a closed polygon and a
  /// negative value deflates it.
  /// </param>
  public void Execute(ref Paths solution, double delta)
  {
    solution.Clear();
    FixOrientations();
    DoOffset(delta);
    //now clean up 'corners' ...
    var clpr = new PolyClip();
    clpr.AddPaths(m_destPolys, PolyType.ptSubject, true);
    if (delta > 0)
    {
      clpr.Execute(ClipType.ctUnion, solution,
        PolyFillType.pftPositive, PolyFillType.pftPositive);
    }
    else
    {
      // ReSharper disable once AccessToStaticMemberViaDerivedType
      var r = PolyClip.GetBounds(m_destPolys);
      var outer = new Path(4);

      outer.Add(new IntPoint(r.left - 10, r.bottom + 10));
      outer.Add(new IntPoint(r.right + 10, r.bottom + 10));
      outer.Add(new IntPoint(r.right + 10, r.top - 10));
      outer.Add(new IntPoint(r.left - 10, r.top - 10));

      clpr.AddPath(outer, PolyType.ptSubject, true);
      clpr.ReverseSolution = true;
      clpr.Execute(ClipType.ctUnion, solution, PolyFillType.pftNegative, PolyFillType.pftNegative);
      if (solution.Count > 0) solution.RemoveAt(0);
    }
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Offsets the paths that have been added and returns the result as a
  /// <see cref="PolyTree"/> that preserves polygon nesting.
  /// </summary>
  /// <param name="solution">The tree that receives the offset paths. It is cleared first.</param>
  /// <param name="delta">
  /// The distance by which to offset. A positive value inflates a closed polygon and a
  /// negative value deflates it.
  /// </param>
  public void Execute(ref PolyTree solution, double delta)
  {
    solution.Clear();
    FixOrientations();
    DoOffset(delta);

    //now clean up 'corners' ...
    var clpr = new PolyClip();
    clpr.AddPaths(m_destPolys, PolyType.ptSubject, true);
    if (delta > 0)
    {
      clpr.Execute(ClipType.ctUnion, solution,
        PolyFillType.pftPositive, PolyFillType.pftPositive);
    }
    else
    {
      // ReSharper disable once AccessToStaticMemberViaDerivedType
      var r = PolyClip.GetBounds(m_destPolys);
      var outer = new Path(4);

      outer.Add(new IntPoint(r.left - 10, r.bottom + 10));
      outer.Add(new IntPoint(r.right + 10, r.bottom + 10));
      outer.Add(new IntPoint(r.right + 10, r.top - 10));
      outer.Add(new IntPoint(r.left - 10, r.top - 10));

      clpr.AddPath(outer, PolyType.ptSubject, true);
      clpr.ReverseSolution = true;
      clpr.Execute(ClipType.ctUnion, solution, PolyFillType.pftNegative, PolyFillType.pftNegative);
      //remove the outer PolyNode rectangle ...
      if (solution.ChildCount == 1 && solution.Childs[0].ChildCount > 0)
      {
        var outerNode = solution.Childs[0];
        solution.Childs.Capacity = outerNode.ChildCount;
        solution.Childs[0] = outerNode.Childs[0];
        solution.Childs[0].m_Parent = solution;
        for (var i = 1; i < outerNode.ChildCount; i++)
          solution.AddChild(outerNode.Childs[i]);
      }
      else
        solution.Clear();
    }
  }
  //------------------------------------------------------------------------------

  void OffsetPoint(int j, ref int k, JoinType jointype)
  {
    //cross product ...
    m_sinA = (m_normals[k].X * m_normals[j].Y - m_normals[j].X * m_normals[k].Y);

    if (Math.Abs(m_sinA * m_delta) < 1.0) 
    {
      //dot product ...
      var cosA = (m_normals[k].X * m_normals[j].X + m_normals[j].Y * m_normals[k].Y); 
      if (cosA > 0) // angle ==> 0 degrees
      {
        m_destPoly.Add(new IntPoint(Round(m_srcPoly[j].X + m_normals[k].X * m_delta),
          Round(m_srcPoly[j].Y + m_normals[k].Y * m_delta)));
        return; 
      }
      //else angle ==> 180 degrees   
    }
    else if (m_sinA > 1.0) m_sinA = 1.0;
    else if (m_sinA < -1.0) m_sinA = -1.0;
    
    if (m_sinA * m_delta < 0)
    {
      m_destPoly.Add(new IntPoint(Round(m_srcPoly[j].X + m_normals[k].X * m_delta),
        Round(m_srcPoly[j].Y + m_normals[k].Y * m_delta)));
      m_destPoly.Add(m_srcPoly[j]);
      m_destPoly.Add(new IntPoint(Round(m_srcPoly[j].X + m_normals[j].X * m_delta),
        Round(m_srcPoly[j].Y + m_normals[j].Y * m_delta)));
    }
    else
      switch (jointype)
      {
        case JoinType.jtMiter:
          {
            var r = 1 + (m_normals[j].X * m_normals[k].X +
                         m_normals[j].Y * m_normals[k].Y);
            if (r >= m_miterLim) DoMiter(j, k, r); else DoSquare(j, k);
            break;
          }
        case JoinType.jtSquare: DoSquare(j, k); break;
        case JoinType.jtRound: DoRound(j, k); break;
      }
    k = j;
  }
  //------------------------------------------------------------------------------

  internal void DoSquare(int j, int k)
  {
    var dx = Math.Tan(Math.Atan2(m_sinA,
        m_normals[k].X * m_normals[j].X + m_normals[k].Y * m_normals[j].Y) / 4);
    m_destPoly.Add(new IntPoint(
        Round(m_srcPoly[j].X + m_delta * (m_normals[k].X - m_normals[k].Y * dx)),
        Round(m_srcPoly[j].Y + m_delta * (m_normals[k].Y + m_normals[k].X * dx))));
    m_destPoly.Add(new IntPoint(
        Round(m_srcPoly[j].X + m_delta * (m_normals[j].X + m_normals[j].Y * dx)),
        Round(m_srcPoly[j].Y + m_delta * (m_normals[j].Y - m_normals[j].X * dx))));
  }
  //------------------------------------------------------------------------------

  internal void DoMiter(int j, int k, double r)
  {
    var q = m_delta / r;
    m_destPoly.Add(new IntPoint(Round(m_srcPoly[j].X + (m_normals[k].X + m_normals[j].X) * q),
        Round(m_srcPoly[j].Y + (m_normals[k].Y + m_normals[j].Y) * q)));
  }
  //------------------------------------------------------------------------------

  internal void DoRound(int j, int k)
  {
    var a = Math.Atan2(m_sinA,
    m_normals[k].X * m_normals[j].X + m_normals[k].Y * m_normals[j].Y);
    var steps = Math.Max((int)Round(m_StepsPerRad * Math.Abs(a)),1);

    double X = m_normals[k].X, Y = m_normals[k].Y, X2;
    for (var i = 0; i < steps; ++i)
    {
      m_destPoly.Add(new IntPoint(
          Round(m_srcPoly[j].X + X * m_delta),
          Round(m_srcPoly[j].Y + Y * m_delta)));
      X2 = X;
      X = X * m_cos - m_sin * Y;
      Y = X2 * m_sin + Y * m_cos;
    }
    m_destPoly.Add(new IntPoint(
    Round(m_srcPoly[j].X + m_normals[j].X * m_delta),
    Round(m_srcPoly[j].Y + m_normals[j].Y * m_delta)));
  }
  //------------------------------------------------------------------------------
}
