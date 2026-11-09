using System;
using System.Collections.Generic;

namespace Physics;

public static class Overlap
{
    /// <summary>
    /// Returns whether or not a given Vec2 point is in a given circle
    /// </summary>
    public static bool PointInCircle(Vec2 p, Circle c)
    {
        return (p - c.Center).LengthSquared < c.Radius * c.Radius;
    }
    /// <summary>
    /// Returns whether or not two given circles overlap
    /// </summary>
    public static bool CircleCircle(Circle a, Circle b)
    {
        return (b.Center - a.Center).LengthSquared < (a.Radius + b.Radius) * (a.Radius + b.Radius);
    }
    /// <summary>
    /// Returns whether or not a given circle and a given line segment overlap
    /// </summary>
    public static bool CircleSegment(Circle c, Segment s)
    {
        Vec2 closestPointToCenter = s.ClosestPoint(c.Center);
        return PointInCircle(closestPointToCenter, c);
    }
    /// <summary>
    /// Returns whether or not two segments overlap, as well as where
    /// </summary>
    public static double? SegmentSegment(Segment P, Segment Q)
    {
        Vec2 r = P.End - P.Start;
        Vec2 s = Q.End - Q.Start;
        Vec2 qp = Q.Start - P.Start;
        if (r.LengthSquared <= Vec2.Epsilon * Vec2.Epsilon) {return null;}
        double crossRS = Vec2.Cross(r, s);
        if (Math.Abs(crossRS) <= Vec2.Epsilon * r.Length * s.Length)
        {
            // Parallel line segments
            if (Math.Abs(Vec2.Cross(qp, r)) > Vec2.Epsilon * r.Length)
            {
                // No overlap
                return null;
            }
            else
            {
                // Find if they overlap
                double t0 = Vec2.Dot(qp, r) / r.LengthSquared;
                double t1 = Vec2.Dot(Q.End - P.Start, r) / r.LengthSquared;
                if ((t0 < 0 && t1 >= 0) || (t0 > 1 && t1 <= 1) || (0 <= t0 && t0 <= 1))
                {
                    // They overlap
                    double t = Math.Min(t0, t1);
                    if (t < 0) {t = 0;}
                    return t;
                }
                else
                {
                    return null;
                }
            }
        }
        else
        {
            // Non-parallel line segments
            double t = Vec2.Cross(qp, s) / crossRS;
            double u = Vec2.Cross(qp, r) / crossRS;
            if (0 <= t && t <= 1 && 0 <= u && u <= 1)
            {
                // Overlap, return first segment t-value
                return t;
            }
            else
            {
                // No overlap
                return null;
            }
        }
    }

    /// <summary>
    /// Finds whether or not a point is in a segment loop
    /// </summary>
    public static bool PointInLoop(Vec2 p, IReadOnlyList<Segment> loop)
    {
        bool inside = false;

        foreach (Segment segment in loop)
        {
            if ((segment.Start.Y > p.Y) != (segment.End.Y > p.Y))
            {
                // Verifies that the segment has a part above and below point p
                double t = (p.Y - segment.Start.Y) / (segment.End.Y - segment.Start.Y); 
                double x = segment.Start.X + t * (segment.End.X - segment.Start.X);
                if (x > p.X)
                {
                    // The line overlaps a ray to the right from p
                    inside = !inside;
                }
            }
        }

        return inside;
    }

    /// <summary>
    /// Returns whether or not a given circle overlaps a given arc
    /// </summary>
    public static bool CircleArc(Circle c, Arc a)
    {
        Vec2 closestPointToCenter = a.ClosestPoint(c.Center);
        return PointInCircle(closestPointToCenter, c);
    }
}