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
using CodeBrix.PolygonTools.Internal;
using CodeBrix.PolygonTools.Models;
using Path = System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CodeBrix.PolygonTools.Models.IntPoint>>;
// ReSharper disable InconsistentNaming

namespace CodeBrix.PolygonTools; //was previously: ClipperLib;

/// <summary>
/// The base class of <see cref="PolyClip"/>, responsible for accepting the subject and
/// clip paths and preparing the edge lists that the clipping algorithm consumes.
/// </summary>
/// <remarks>
/// This type is not used directly. Create a <see cref="PolyClip"/> instance, add paths
/// to it with <see cref="AddPath"/> or <see cref="AddPaths"/>, and then call one of the
/// <c>Execute</c> overloads.
/// </remarks>
public class PolyClipBase
{    
  internal const double horizontal = -3.4E+38;
  internal const int Skip = -2;
  internal const int Unassigned = -1;
  internal const double tolerance = 1.0E-20;
  internal static bool near_zero(double val){return val is > -tolerance and < tolerance;}

  /// <summary>
  /// The largest coordinate magnitude that can be used when the clipping result is required
  /// to be free of rounding, equal to 0x3FFFFFFF.
  /// </summary>
  public const long loRange = 0x3FFFFFFF;
  /// <summary>
  /// The largest coordinate magnitude that may be supplied to this library, equal to
  /// 0x3FFFFFFFFFFFFFFF. Coordinates beyond this range cause <see cref="AddPath"/> to throw.
  /// </summary>
  public const long hiRange = 0x3FFFFFFFFFFFFFFFL; 

  internal LocalMinima m_MinimaList;
  internal LocalMinima m_CurrentLM;
  internal List<List<TEdge>> m_edges = new List<List<TEdge>>();
  internal Scanbeam m_Scanbeam;
  internal List<OutRec> m_PolyOuts;
  internal TEdge m_ActiveEdges;
  internal bool m_UseFullRange;
  internal bool m_HasOpenPaths;

  //------------------------------------------------------------------------------

  /// <summary>
  /// Gets or sets a value indicating whether collinear vertices are preserved in the
  /// solution rather than being merged away. The default is <c>false</c>.
  /// </summary>
  public bool PreserveCollinear
  {
    get;
    set;
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Exchanges the values of two coordinates.
  /// </summary>
  /// <param name="val1">The first value, which receives the value of <paramref name="val2"/>.</param>
  /// <param name="val2">The second value, which receives the value of <paramref name="val1"/>.</param>
  public void Swap(ref long val1, ref long val2)
  {
    var tmp = val1;
    val1 = val2;
    val2 = tmp;
  }
  //------------------------------------------------------------------------------

  internal static bool IsHorizontal(TEdge e)
  {
    return e.Delta.Y == 0;
  }
  //------------------------------------------------------------------------------

  internal bool PointIsVertex(IntPoint pt, OutPt pp)
  {
    var pp2 = pp;
    do
    {
      if (pp2.Pt == pt) return true;
      pp2 = pp2.Next;
    }
    while (pp2 != pp);
    return false;
  }
  //------------------------------------------------------------------------------

  internal bool PointOnLineSegment(IntPoint pt, 
      IntPoint linePt1, IntPoint linePt2, bool UseFullRange)
  {
    if (UseFullRange)
      return ((pt.X == linePt1.X) && (pt.Y == linePt1.Y)) ||
        ((pt.X == linePt2.X) && (pt.Y == linePt2.Y)) ||
        (((pt.X > linePt1.X) == (pt.X < linePt2.X)) &&
        ((pt.Y > linePt1.Y) == (pt.Y < linePt2.Y)) &&
        (((Int128)(pt.X - linePt1.X) * (linePt2.Y - linePt1.Y) ==
        (Int128)(linePt2.X - linePt1.X) * (pt.Y - linePt1.Y))));
    else
      return ((pt.X == linePt1.X) && (pt.Y == linePt1.Y)) ||
        ((pt.X == linePt2.X) && (pt.Y == linePt2.Y)) ||
        (((pt.X > linePt1.X) == (pt.X < linePt2.X)) &&
        ((pt.Y > linePt1.Y) == (pt.Y < linePt2.Y)) &&
        ((pt.X - linePt1.X) * (linePt2.Y - linePt1.Y) ==
          (linePt2.X - linePt1.X) * (pt.Y - linePt1.Y)));
  }
  //------------------------------------------------------------------------------

  internal bool PointOnPolygon(IntPoint pt, OutPt pp, bool UseFullRange)
  {
    var pp2 = pp;
    while (true)
    {
      if (PointOnLineSegment(pt, pp2.Pt, pp2.Next.Pt, UseFullRange))
        return true;
      pp2 = pp2.Next;
      if (pp2 == pp) break;
    }
    return false;
  }
  //------------------------------------------------------------------------------

  internal static bool SlopesEqual(TEdge e1, TEdge e2, bool UseFullRange)
  {
      if (UseFullRange)
        return (Int128)e1.Delta.Y * e2.Delta.X ==
            (Int128)e1.Delta.X * e2.Delta.Y;
      else return e1.Delta.Y * (e2.Delta.X) ==
        e1.Delta.X * (e2.Delta.Y);
  }
  //------------------------------------------------------------------------------

  internal static bool SlopesEqual(IntPoint pt1, IntPoint pt2,
      IntPoint pt3, bool UseFullRange)
  {
      if (UseFullRange)
          return (Int128)(pt1.Y - pt2.Y) * (pt2.X - pt3.X) ==
            (Int128)(pt1.X - pt2.X) * (pt2.Y - pt3.Y);
      else return
        (pt1.Y - pt2.Y) * (pt2.X - pt3.X) - (pt1.X - pt2.X) * (pt2.Y - pt3.Y) == 0;
  }
  //------------------------------------------------------------------------------

  internal static bool SlopesEqual(IntPoint pt1, IntPoint pt2,
      IntPoint pt3, IntPoint pt4, bool UseFullRange)
  {
      if (UseFullRange)
          return (Int128)(pt1.Y - pt2.Y) * (pt3.X - pt4.X) ==
            (Int128)(pt1.X - pt2.X) * (pt3.Y - pt4.Y);
      else return
        (pt1.Y - pt2.Y) * (pt3.X - pt4.X) - (pt1.X - pt2.X) * (pt3.Y - pt4.Y) == 0;
  }
  //------------------------------------------------------------------------------

  internal PolyClipBase() //constructor (nb: no external instantiation)
  {
      m_MinimaList = null;
      m_CurrentLM = null;
      m_UseFullRange = false;
      m_HasOpenPaths = false;
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Removes every subject and clip path that has been added, returning this instance to
  /// its initial state so that it can be reused for another operation.
  /// </summary>
  public virtual void Clear()
  {
      DisposeLocalMinimaList();
      for (var i = 0; i < m_edges.Count; ++i)
      {
          for (var j = 0; j < m_edges[i].Count; ++j) m_edges[i][j] = null;
          m_edges[i].Clear();
      }
      m_edges.Clear();
      m_UseFullRange = false;
      m_HasOpenPaths = false;
  }
  //------------------------------------------------------------------------------

  private void DisposeLocalMinimaList()
  {
      while( m_MinimaList != null )
      {
          var tmpLm = m_MinimaList.Next;
          m_MinimaList = null;
          m_MinimaList = tmpLm;
      }
      m_CurrentLM = null;
  }
  //------------------------------------------------------------------------------

  void RangeTest(IntPoint Pt, ref bool useFullRange)
  {
    if (useFullRange)
    {
      if (Pt.X > hiRange || Pt.Y > hiRange || -Pt.X > hiRange || -Pt.Y > hiRange) 
        throw new PolyClipException("Coordinate outside allowed range");
    }
    else if (Pt.X > loRange || Pt.Y > loRange || -Pt.X > loRange || -Pt.Y > loRange) 
    {
      useFullRange = true;
      RangeTest(Pt, ref useFullRange);
    }
  }
  //------------------------------------------------------------------------------

  private void InitEdge(TEdge e, TEdge eNext,
    TEdge ePrev, IntPoint pt)
  {
    e.Next = eNext;
    e.Prev = ePrev;
    e.Curr = pt;
    e.OutIdx = Unassigned;
  }
  //------------------------------------------------------------------------------

  private void InitEdge2(TEdge e, PolyType polyType)
  {
    if (e.Curr.Y >= e.Next.Curr.Y)
    {
      e.Bot = e.Curr;
      e.Top = e.Next.Curr;
    }
    else
    {
      e.Top = e.Curr;
      e.Bot = e.Next.Curr;
    }
    SetDx(e);
    e.PolyTyp = polyType;
  }
  //------------------------------------------------------------------------------

  private TEdge FindNextLocMin(TEdge E)
  {
    TEdge E2;
    for (;;)
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        while (E.Bot != E.Prev.Bot || E.Curr == E.Top) E = E.Next;
        if (E.Dx != horizontal && E.Prev.Dx != horizontal) break;
        while (E.Prev.Dx == horizontal) E = E.Prev;
        E2 = E;
        while (E.Dx == horizontal) E = E.Next;
        if (E.Top.Y == E.Prev.Bot.Y) continue; //ie just an intermediate horz.
        if (E2.Prev.Bot.X < E.Bot.X) E = E2;
        // ReSharper restore CompareOfFloatsByEqualityOperator
        break;
    }
    return E;
  }
  //------------------------------------------------------------------------------

  private TEdge ProcessBound(TEdge E, bool LeftBoundIsForward)
  {
    TEdge EStart, Result = E;
    TEdge Horz;

    if (Result.OutIdx == Skip)
    {
        //check if there are edges beyond the skip edge in the bound and if so
        //create another LocMin and calling ProcessBound once more ...
        // ReSharper disable CompareOfFloatsByEqualityOperator
      E = Result;
      if (LeftBoundIsForward)
      {
        while (E.Top.Y == E.Next.Bot.Y) E = E.Next;
        while (E != Result && E.Dx == horizontal) E = E.Prev;
      }
      else
      {
        while (E.Top.Y == E.Prev.Bot.Y) E = E.Prev;
        while (E != Result && E.Dx == horizontal) E = E.Next;
      }
      if (E == Result)
      {
        if (LeftBoundIsForward) Result = E.Next;
        else Result = E.Prev;
      }
      else
      {
        //there are more edges in the bound beyond result starting with E
        if (LeftBoundIsForward)
          E = Result.Next;
        else
          E = Result.Prev;
        var locMin = new LocalMinima();
        locMin.Next = null;
        locMin.Y = E.Bot.Y;
        locMin.LeftBound = null;
        locMin.RightBound = E;
        E.WindDelta = 0;
        Result = ProcessBound(E, LeftBoundIsForward);
        InsertLocalMinima(locMin);
      }
      // ReSharper restore CompareOfFloatsByEqualityOperator

      return Result;
    }

    // ReSharper disable CompareOfFloatsByEqualityOperator
    if (E.Dx == horizontal)
    {
      //We need to be careful with open paths because this may not be a
      //true local minima (ie E may be following a skip edge).
      //Also, consecutive horz. edges may start heading left before going right.
      if (LeftBoundIsForward) EStart = E.Prev;
      else EStart = E.Next;
      if (EStart.Dx == horizontal) //ie an adjoining horizontal skip edge
      {
          if (EStart.Bot.X != E.Bot.X && EStart.Top.X != E.Bot.X)
              ReverseHorizontal(E);
      }
      else if (EStart.Bot.X != E.Bot.X)
          ReverseHorizontal(E);
    }

    EStart = E;
    if (LeftBoundIsForward)
    {
      while (Result.Top.Y == Result.Next.Bot.Y && Result.Next.OutIdx != Skip)
        Result = Result.Next;
      if (Result.Dx == horizontal && Result.Next.OutIdx != Skip)
      {
        //nb: at the top of a bound, horizontals are added to the bound
        //only when the preceding edge attaches to the horizontal's left vertex
        //unless a Skip edge is encountered when that becomes the top divide
        Horz = Result;
        while (Horz.Prev.Dx == horizontal) Horz = Horz.Prev;
        if (Horz.Prev.Top.X > Result.Next.Top.X) Result = Horz.Prev;
      }
      while (E != Result)
      {
        E.NextInLML = E.Next;
        if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Prev.Top.X) 
          ReverseHorizontal(E);
        E = E.Next;
      }
      if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Prev.Top.X) 
        ReverseHorizontal(E);
      Result = Result.Next; //move to the edge just beyond current bound
    }
    else
    {
      while (Result.Top.Y == Result.Prev.Bot.Y && Result.Prev.OutIdx != Skip)
        Result = Result.Prev;
      if (Result.Dx == horizontal && Result.Prev.OutIdx != Skip)
      {
        Horz = Result;
        while (Horz.Next.Dx == horizontal) Horz = Horz.Next;
        if (Horz.Next.Top.X == Result.Prev.Top.X || 
            Horz.Next.Top.X > Result.Prev.Top.X) Result = Horz.Next;
      }

      while (E != Result)
      {
        E.NextInLML = E.Prev;
        if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Next.Top.X) 
          ReverseHorizontal(E);
        E = E.Prev;
      }
      if (E.Dx == horizontal && E != EStart && E.Bot.X != E.Next.Top.X) 
        ReverseHorizontal(E);
      Result = Result.Prev; //move to the edge just beyond current bound
    }
    // ReSharper restore CompareOfFloatsByEqualityOperator
    
    return Result;
  }
  //------------------------------------------------------------------------------


  /// <summary>
  /// Adds a single path to this instance as either subject or clip geometry.
  /// </summary>
  /// <param name="pg">The path to add.</param>
  /// <param name="polyType">Whether the path is subject or clip geometry.</param>
  /// <param name="Closed">
  /// <c>true</c> when the path is a closed polygon; <c>false</c> when it is an open path.
  /// Clip paths must always be closed.
  /// </param>
  /// <returns>
  /// <c>true</c> when the path was added; <c>false</c> when it was rejected because it had
  /// too few distinct vertices to contribute to the result.
  /// </returns>
  /// <exception cref="PolyClipException">
  /// An open path was supplied as clip geometry, or a coordinate exceeded
  /// <see cref="hiRange"/> in magnitude.
  /// </exception>
  public bool AddPath(Path pg, PolyType polyType, bool Closed)
  {
    if (!Closed && polyType == PolyType.ptClip)
      throw new PolyClipException("AddPath: Open paths must be subject.");

    var highI = pg.Count - 1;
    if (Closed) while (highI > 0 && (pg[highI] == pg[0])) --highI;
    while (highI > 0 && (pg[highI] == pg[highI - 1])) --highI;
    if ((Closed && highI < 2) || (!Closed && highI < 1)) return false;

    //create a new edge array ...
    var edges = new List<TEdge>(highI+1);
    for (var i = 0; i <= highI; i++) edges.Add(new TEdge());
        
    var IsFlat = true;

    //1. Basic (first) edge initialization ...
    edges[1].Curr = pg[1];
    RangeTest(pg[0], ref m_UseFullRange);
    RangeTest(pg[highI], ref m_UseFullRange);
    InitEdge(edges[0], edges[1], edges[highI], pg[0]);
    InitEdge(edges[highI], edges[0], edges[highI - 1], pg[highI]);
    for (var i = highI - 1; i >= 1; --i)
    {
      RangeTest(pg[i], ref m_UseFullRange);
      InitEdge(edges[i], edges[i + 1], edges[i - 1], pg[i]);
    }
    var eStart = edges[0];

    //2. Remove duplicate vertices, and (when closed) collinear edges ...
    TEdge E = eStart, eLoopStop = eStart;
    for (;;)
    {
      //nb: allows matching start and end points when not Closed ...
      if (E.Curr == E.Next.Curr && (Closed || E.Next != eStart))
      {
        if (E == E.Next) break;
        if (E == eStart) eStart = E.Next;
        E = RemoveEdge(E);
        eLoopStop = E;
        continue;
      }
      if (E.Prev == E.Next) 
        break; //only two vertices
      else if (Closed &&
        SlopesEqual(E.Prev.Curr, E.Curr, E.Next.Curr, m_UseFullRange) && 
        (!PreserveCollinear ||
        !Pt2IsBetweenPt1AndPt3(E.Prev.Curr, E.Curr, E.Next.Curr))) 
      {
        //Collinear edges are allowed for open paths but in closed paths
        //the default is to merge adjacent collinear edges into a single edge.
        //However, if the PreserveCollinear property is enabled, only overlapping
        //collinear edges (ie spikes) will be removed from closed paths.
        if (E == eStart) eStart = E.Next;
        E = RemoveEdge(E);
        E = E.Prev;
        eLoopStop = E;
        continue;
      }
      E = E.Next;
      if ((E == eLoopStop) || (!Closed && E.Next == eStart)) break;
    }

    if ((!Closed && (E == E.Next)) || (Closed && (E.Prev == E.Next)))
      return false;

    if (!Closed)
    {
      m_HasOpenPaths = true;
      eStart.Prev.OutIdx = Skip;
    }

    //3. Do second stage of edge initialization ...
    E = eStart;
    do
    {
      InitEdge2(E, polyType);
      E = E.Next;
      if (IsFlat && E.Curr.Y != eStart.Curr.Y) IsFlat = false;
    }
    while (E != eStart);

    //4. Finally, add edge bounds to LocalMinima list ...

    //Totally flat paths must be handled differently when adding them
    //to LocalMinima list to avoid endless loops etc ...
    if (IsFlat) 
    {
      if (Closed) return false;
      E.Prev.OutIdx = Skip;
      var locMin = new LocalMinima();
      locMin.Next = null;
      locMin.Y = E.Bot.Y;
      locMin.LeftBound = null;
      locMin.RightBound = E;
      locMin.RightBound.Side = EdgeSide.esRight;
      locMin.RightBound.WindDelta = 0;
      for ( ; ; )
      {
        if (E.Bot.X != E.Prev.Top.X) ReverseHorizontal(E);
        if (E.Next.OutIdx == Skip) break;
        E.NextInLML = E.Next;
        E = E.Next;
      }
      InsertLocalMinima(locMin);
      m_edges.Add(edges);
      return true;
    }

    m_edges.Add(edges);
    bool leftBoundIsForward;
    TEdge EMin = null;

    //workaround to avoid an endless loop in the while loop below when
    //open paths have matching start and end points ...
    if (E.Prev.Bot == E.Prev.Top) E = E.Next;

    for (;;)
    {
      E = FindNextLocMin(E);
      if (E == EMin) break;
      else if (EMin == null) EMin = E;

      //E and E.Prev now share a local minima (left aligned if horizontal).
      //Compare their slopes to find which starts which bound ...
      var locMin = new LocalMinima();
      locMin.Next = null;
      locMin.Y = E.Bot.Y;
      if (E.Dx < E.Prev.Dx) 
      {
        locMin.LeftBound = E.Prev;
        locMin.RightBound = E;
        leftBoundIsForward = false; //Q.nextInLML = Q.prev
      } else
      {
        locMin.LeftBound = E;
        locMin.RightBound = E.Prev;
        leftBoundIsForward = true; //Q.nextInLML = Q.next
      }
      locMin.LeftBound.Side = EdgeSide.esLeft;
      locMin.RightBound.Side = EdgeSide.esRight;

      if (!Closed) locMin.LeftBound.WindDelta = 0;
      else if (locMin.LeftBound.Next == locMin.RightBound)
        locMin.LeftBound.WindDelta = -1;
      else locMin.LeftBound.WindDelta = 1;
      locMin.RightBound.WindDelta = -locMin.LeftBound.WindDelta;

      E = ProcessBound(locMin.LeftBound, leftBoundIsForward);
      if (E.OutIdx == Skip) E = ProcessBound(E, leftBoundIsForward);

      var E2 = ProcessBound(locMin.RightBound, !leftBoundIsForward);
      if (E2.OutIdx == Skip) E2 = ProcessBound(E2, !leftBoundIsForward);

      if (locMin.LeftBound.OutIdx == Skip)
        locMin.LeftBound = null;
      else if (locMin.RightBound.OutIdx == Skip)
        locMin.RightBound = null;
      InsertLocalMinima(locMin);
      if (!leftBoundIsForward) E = E2;
    }
    return true;

  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Adds several paths to this instance as either subject or clip geometry.
  /// </summary>
  /// <param name="ppg">The paths to add.</param>
  /// <param name="polyType">Whether the paths are subject or clip geometry.</param>
  /// <param name="closed">
  /// <c>true</c> when the paths are closed polygons; <c>false</c> when they are open paths.
  /// Clip paths must always be closed.
  /// </param>
  /// <returns>
  /// <c>true</c> when at least one of the paths was added; otherwise <c>false</c>.
  /// </returns>
  /// <exception cref="PolyClipException">
  /// An open path was supplied as clip geometry, or a coordinate exceeded
  /// <see cref="hiRange"/> in magnitude.
  /// </exception>
  public bool AddPaths(Paths ppg, PolyType polyType, bool closed)
  {
    var result = false;
    for (var i = 0; i < ppg.Count; ++i)
      if (AddPath(ppg[i], polyType, closed)) result = true;
    return result;
  }
  //------------------------------------------------------------------------------

  internal bool Pt2IsBetweenPt1AndPt3(IntPoint pt1, IntPoint pt2, IntPoint pt3)
  {
    if ((pt1 == pt3) || (pt1 == pt2) || (pt3 == pt2)) return false;
    else if (pt1.X != pt3.X) return (pt2.X > pt1.X) == (pt2.X < pt3.X);
    else return (pt2.Y > pt1.Y) == (pt2.Y < pt3.Y);
  }
  //------------------------------------------------------------------------------

  TEdge RemoveEdge(TEdge e)
  {
    //removes e from double_linked_list (but without removing from memory)
    e.Prev.Next = e.Next;
    e.Next.Prev = e.Prev;
    var result = e.Next;
    e.Prev = null; //flag as removed (see PolyClipBase.Clear)
    return result;
  }
  //------------------------------------------------------------------------------

  private void SetDx(TEdge e)
  {
    e.Delta.X = (e.Top.X - e.Bot.X);
    e.Delta.Y = (e.Top.Y - e.Bot.Y);
    if (e.Delta.Y == 0) e.Dx = horizontal;
    else e.Dx = (double)(e.Delta.X) / (e.Delta.Y);
  }
  //---------------------------------------------------------------------------

  private void InsertLocalMinima(LocalMinima newLm)
  {
    if( m_MinimaList == null )
    {
      m_MinimaList = newLm;
    }
    else if( newLm.Y >= m_MinimaList.Y )
    {
      newLm.Next = m_MinimaList;
      m_MinimaList = newLm;
    } else
    {
      var tmpLm = m_MinimaList;
      while( tmpLm.Next != null  && ( newLm.Y < tmpLm.Next.Y ) )
        tmpLm = tmpLm.Next;
      newLm.Next = tmpLm.Next;
      tmpLm.Next = newLm;
    }
  }
  //------------------------------------------------------------------------------

  internal Boolean PopLocalMinima(long Y, out LocalMinima current)
  {
      current = m_CurrentLM;
      if (m_CurrentLM != null && m_CurrentLM.Y == Y)
      {
          m_CurrentLM = m_CurrentLM.Next;
          return true;
      }
      return false;
  }
  //------------------------------------------------------------------------------

  private void ReverseHorizontal(TEdge e)
  {
    //swap horizontal edges' top and bottom x's so they follow the natural
    //progression of the bounds - ie so their xbots will align with the
    //adjoining lower edge. [Helpful in the ProcessHorizontal() method.]
    Swap(ref e.Top.X, ref e.Bot.X);
  }
  //------------------------------------------------------------------------------

  internal virtual void Reset()
  {
    m_CurrentLM = m_MinimaList;
    if (m_CurrentLM == null) return; //ie nothing to process

    //reset all edges ...
    m_Scanbeam = null;
    var lm = m_MinimaList;
    while (lm != null)
    {
      InsertScanbeam(lm.Y);
      var e = lm.LeftBound;
      if (e != null)
      {
        e.Curr = e.Bot;
        e.OutIdx = Unassigned;
      }
      e = lm.RightBound;
      if (e != null)
      {
        e.Curr = e.Bot;
        e.OutIdx = Unassigned;
      }
      lm = lm.Next;
    }
    m_ActiveEdges = null;
  }
  //------------------------------------------------------------------------------

  /// <summary>
  /// Calculates the axis-aligned bounding rectangle that encloses every supplied path.
  /// </summary>
  /// <param name="paths">The paths to measure.</param>
  /// <returns>
  /// The bounding rectangle of the supplied paths, or an empty rectangle when none of them
  /// contains any vertex.
  /// </returns>
  public static IntRect GetBounds(Paths paths)
  {
    int i = 0, cnt = paths.Count;
    while (i < cnt && paths[i].Count == 0) i++;
    if (i == cnt) return new IntRect(0,0,0,0);
    var result = new IntRect();
    result.left = paths[i][0].X;
    result.right = result.left;
    result.top = paths[i][0].Y;
    result.bottom = result.top;
    for (; i < cnt; i++)
      for (var j = 0; j < paths[i].Count; j++)
      {
        if (paths[i][j].X < result.left) result.left = paths[i][j].X;
        else if (paths[i][j].X > result.right) result.right = paths[i][j].X;
        if (paths[i][j].Y < result.top) result.top = paths[i][j].Y;
        else if (paths[i][j].Y > result.bottom) result.bottom = paths[i][j].Y;
      }
    return result;
  }
  //------------------------------------------------------------------------------

  internal void InsertScanbeam(long Y)
  {
      //single-linked list: sorted descending, ignoring dups.
      if (m_Scanbeam == null)
      {
          m_Scanbeam = new Scanbeam();
          m_Scanbeam.Next = null;
          m_Scanbeam.Y = Y;
      }
      else if (Y > m_Scanbeam.Y)
      {
          var newSb = new Scanbeam();
          newSb.Y = Y;
          newSb.Next = m_Scanbeam;
          m_Scanbeam = newSb;
      }
      else
      {
          var sb2 = m_Scanbeam;
          while (sb2.Next != null && (Y <= sb2.Next.Y)) sb2 = sb2.Next;
          if (Y == sb2.Y) return; //ie ignores duplicates
          var newSb = new Scanbeam();
          newSb.Y = Y;
          newSb.Next = sb2.Next;
          sb2.Next = newSb;
      }
  }
  //------------------------------------------------------------------------------

  internal Boolean PopScanbeam(out long Y)
  {
      if (m_Scanbeam == null)
      {
          Y = 0;
          return false;
      }
      Y = m_Scanbeam.Y;
      m_Scanbeam = m_Scanbeam.Next;
      return true;
  }
  //------------------------------------------------------------------------------

  internal Boolean LocalMinimaPending()
  {
      return (m_CurrentLM != null);
  }
  //------------------------------------------------------------------------------

  internal OutRec CreateOutRec()
  {
      var result = new OutRec();
      result.Idx = Unassigned;
      result.IsHole = false;
      result.IsOpen = false;
      result.FirstLeft = null;
      result.Pts = null;
      result.BottomPt = null;
      result.PolyNode = null;
      m_PolyOuts.Add(result);
      result.Idx = m_PolyOuts.Count - 1;
      return result;
  }
  //------------------------------------------------------------------------------

  internal void DisposeOutRec(int index)
  {
      var outRec = m_PolyOuts[index];
      outRec.Pts = null;
      // ReSharper disable once RedundantAssignment
      outRec = null;
      m_PolyOuts[index] = null;
  }
  //------------------------------------------------------------------------------

  internal void UpdateEdgeIntoAEL(ref TEdge e)
  {
      if (e.NextInLML == null)
          throw new PolyClipException("UpdateEdgeIntoAEL: invalid call");
      var AelPrev = e.PrevInAEL;
      var AelNext = e.NextInAEL;
      e.NextInLML.OutIdx = e.OutIdx;
      if (AelPrev != null)
          AelPrev.NextInAEL = e.NextInLML;
      else m_ActiveEdges = e.NextInLML;
      if (AelNext != null)
          AelNext.PrevInAEL = e.NextInLML;
      e.NextInLML.Side = e.Side;
      e.NextInLML.WindDelta = e.WindDelta;
      e.NextInLML.WindCnt = e.WindCnt;
      e.NextInLML.WindCnt2 = e.WindCnt2;
      e = e.NextInLML;
      e.Curr = e.Bot;
      e.PrevInAEL = AelPrev;
      e.NextInAEL = AelNext;
      if (!IsHorizontal(e)) InsertScanbeam(e.Top.Y);
  }
  //------------------------------------------------------------------------------

  internal void SwapPositionsInAEL(TEdge edge1, TEdge edge2)
  {
      //check that one or other edge hasn't already been removed from AEL ...
      if (edge1.NextInAEL == edge1.PrevInAEL ||
        edge2.NextInAEL == edge2.PrevInAEL) return;

      if (edge1.NextInAEL == edge2)
      {
          var next = edge2.NextInAEL;
          if (next != null)
              next.PrevInAEL = edge1;
          var prev = edge1.PrevInAEL;
          if (prev != null)
              prev.NextInAEL = edge2;
          edge2.PrevInAEL = prev;
          edge2.NextInAEL = edge1;
          edge1.PrevInAEL = edge2;
          edge1.NextInAEL = next;
      }
      else if (edge2.NextInAEL == edge1)
      {
          var next = edge1.NextInAEL;
          if (next != null)
              next.PrevInAEL = edge2;
          var prev = edge2.PrevInAEL;
          if (prev != null)
              prev.NextInAEL = edge1;
          edge1.PrevInAEL = prev;
          edge1.NextInAEL = edge2;
          edge2.PrevInAEL = edge1;
          edge2.NextInAEL = next;
      }
      else
      {
          var next = edge1.NextInAEL;
          var prev = edge1.PrevInAEL;
          edge1.NextInAEL = edge2.NextInAEL;
          if (edge1.NextInAEL != null)
              edge1.NextInAEL.PrevInAEL = edge1;
          edge1.PrevInAEL = edge2.PrevInAEL;
          if (edge1.PrevInAEL != null)
              edge1.PrevInAEL.NextInAEL = edge1;
          edge2.NextInAEL = next;
          if (edge2.NextInAEL != null)
              edge2.NextInAEL.PrevInAEL = edge2;
          edge2.PrevInAEL = prev;
          if (edge2.PrevInAEL != null)
              edge2.PrevInAEL.NextInAEL = edge2;
      }

      if (edge1.PrevInAEL == null)
          m_ActiveEdges = edge1;
      else if (edge2.PrevInAEL == null)
          m_ActiveEdges = edge2;
  }
  //------------------------------------------------------------------------------

  internal void DeleteFromAEL(TEdge e)
  {
      var AelPrev = e.PrevInAEL;
      var AelNext = e.NextInAEL;
      if (AelPrev == null && AelNext == null && (e != m_ActiveEdges))
          return; //already deleted
      if (AelPrev != null)
          AelPrev.NextInAEL = AelNext;
      else m_ActiveEdges = AelNext;
      if (AelNext != null)
          AelNext.PrevInAEL = AelPrev;
      e.NextInAEL = null;
      e.PrevInAEL = null;
  }
  //------------------------------------------------------------------------------

} //end PolyClipBase
