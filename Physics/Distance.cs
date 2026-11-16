using System;
using System.Collections.Generic;

namespace Physics;

public static class Distance
{
    public static double CircleCircle(Circle a, Circle b)
    {
        return Math.Max(0, (a.Center - b.Center).Length - a.Radius - b.Radius);
    }

    public static double CircleEdge(Circle c, IEdge a)
    {
        Vec2 ClosestPoint = a.ClosestPoint(c.Center);
        return Math.Max(0, (c.Center - ClosestPoint).Length - c.Radius);
    }

    public static double SegmentSegment(Segment s, Segment t)
    {
        if (Overlap.SegmentSegment(s, t) == null)
        {
            double best = (s.Start - t.ClosestPoint(s.Start)).LengthSquared;
            double test = (s.End - t.ClosestPoint(s.End)).LengthSquared;
            best = test < best ? test : best;
            test = (t.Start - s.ClosestPoint(t.Start)).LengthSquared;
            best = test < best ? test : best;
            test = (t.End - s.ClosestPoint(t.End)).LengthSquared;
            best = test < best ? test : best;
            return Math.Sqrt(best); 
        }
        return 0;
    }

    public static double SegmentArc(Segment s, Arc a)
    {
        if (Overlap.SegmentArc(s, a) == null)
        {
            double best = (s.Start - a.ClosestPoint(s.Start)).LengthSquared;
            double test = (s.End - a.ClosestPoint(s.End)).LengthSquared;
            best = test < best ? test : best;
            test = (a.Start - s.ClosestPoint(a.Start)).LengthSquared;
            best = test < best ? test : best;
            test = (a.End - s.ClosestPoint(a.End)).LengthSquared;
            best = test < best ? test : best;
            best = Math.Sqrt(best);
            Vec2 ClosestPoint = s.ClosestPoint(a.Center);
            Vec2 direction = (ClosestPoint - a.Center).Normalized();
            if (direction.LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
            {
                Vec2 arcPoint = a.Center + a.Radius * direction;
                if (a.IsWithinAngle(direction))
                {
                    test = (ClosestPoint - arcPoint).Length;
                    best = test < best ? test : best;
                }
            }
            return best; 
        }
        return 0;
    }

    public static double ArcArc(Arc a, Arc r)
    {
        if (Overlap.ArcArc(a, r) == false)
        {
            double best = (a.Start - r.ClosestPoint(a.Start)).LengthSquared;
            double test = (a.End - r.ClosestPoint(a.End)).LengthSquared;
            best = test < best ? test : best;
            test = (r.Start - a.ClosestPoint(r.Start)).LengthSquared;
            best = test < best ? test : best;
            test = (r.End - a.ClosestPoint(r.End)).LengthSquared;
            best = test < best ? test : best;
            best = Math.Sqrt(best);
            Vec2 ClosestPoint = a.ClosestPoint(r.Center);
            Vec2 direction = (ClosestPoint - r.Center).Normalized();
            if (direction.LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
            {
                Vec2 arcPoint = r.Center + r.Radius * direction;
                if (r.IsWithinAngle(direction))
                {
                    test = (ClosestPoint - arcPoint).Length;
                    best = test < best ? test : best;
                }
            }
            ClosestPoint = r.ClosestPoint(a.Center);
            direction = (ClosestPoint - a.Center).Normalized();
            if (direction.LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
            {
                Vec2 arcPoint = a.Center + a.Radius * direction;
                if (a.IsWithinAngle(direction))
                {
                    test = (ClosestPoint - arcPoint).Length;
                    best = test < best ? test : best;
                }
            }
            return best;
        }
        return 0;
    }

    public static double EdgeEdge(IEdge a, IEdge b) => (a, b) switch
    {
        (Segment s, Segment t) => SegmentSegment(s, t),
        (Segment s, Arc r) => SegmentArc(s, r),
        // (Segment s, Curve c) => SegmentCurve(s, c),
        (Arc r, Segment s) => SegmentArc(s, r),
        (Arc r, Arc ar) => ArcArc(r, ar),
        // (Arc r, Curve c) => ArcCurve(r, c),
        // (Curve c, Segment s) => SegmentCurve(s, c),
        // (Curve c, Arc r) => ArcCurve(r, c),
        // (Curve c, Curve cv) => CurveCurve(c, cv),
        _ => throw new NotImplementedException()
    };

    public static double CircleLoop(Circle c, IReadOnlyList<IEdge> loop, bool closed)
    {
        double best = double.PositiveInfinity;
        if (closed)
        {
            if (Overlap.PointInLoop(c.Center, loop)) {return 0;}
        }
        foreach (IEdge edge in loop)
        {
            double test = CircleEdge(c, edge);
            best = test < best ? test : best;
        }
        return best;
    }

    public static double LoopLoop(IReadOnlyList<IEdge> a, IReadOnlyList<IEdge> b, bool closedA, bool closedB)
    {
        double best = double.PositiveInfinity;
        if (closedB && a.Count > 0)
        {
            if (Overlap.PointInLoop(a[0].Start, b)) {return 0;}
        }
        if (closedA && b.Count > 0)
        {
            if (Overlap.PointInLoop(b[0].Start, a)) {return 0;}
        }
        foreach(IEdge edgeA in a)
        {
            foreach(IEdge edgeB in b)
            {
                double test = EdgeEdge(edgeA, edgeB);
                best = test < best ? test : best;
            }
        }
        return best;
    }
}