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
                return null;
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
            // No overlap
            return null;
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

    /// <summary>
    /// Returns how far along a given segment overlaps with a given circle
    /// </summary>
    public static double? SegmentCircle(Segment s, Circle circ)
    {
        Vec2 r = s.End - s.Start;
        Vec2 f = s.Start - circ.Center;
        double a = r.LengthSquared;
        if (Math.Abs(a) <= Vec2.Epsilon * Vec2.Epsilon) {return null;}
        double b = Vec2.Dot(f, r);
        double c = f.LengthSquared - circ.Radius * circ.Radius;
        double disc = b * b - a * c;
        if (disc >= 0)
        {
            // The line hits the circle
            double sqrtDisc = Math.Sqrt(disc);
            double t1 = (-b - sqrtDisc) / a;
            double t2 = (-b + sqrtDisc) / a;
            if (t1 >= 0 && t1 <= 1)
            {
                // Hits the circle at t1
                return t1;
            }
            else if (t1 < 0 && 0 <= t2)
            {
                // Already inside the circle, return 0
                return 0;
            }
            // Segment doesn't touch the circle
            return null;
        }
        // The line does not hit the circle
        return null;
    }

    /// <summary>
    /// Returns how far along a given segment hits a given arc
    /// </summary>
    public static double? SegmentArc(Segment s, Arc arc)
    {
        Vec2 r = s.End - s.Start;
        Vec2 f = s.Start - arc.Center;
        double a = r.LengthSquared;
        if (a <= Vec2.Epsilon * Vec2.Epsilon) {return null;}
        double b = Vec2.Dot(f, r);
        double c = f.LengthSquared - arc.Radius * arc.Radius;
        double disc = b * b - a * c;
        if (disc >= 0)
        {
            // The line might hit the arc
            double sqrtDisc = Math.Sqrt(disc);
            double t1 = (-b - sqrtDisc) / a;
            double t2 = (-b + sqrtDisc) / a;
            if (t1 >= 0 && t1 <= 1 && arc.IsWithinAngle(f + t1 * r))
            {
                // Hits the arc at t1
                return t1;
            }
            else if (t2 >= 0 && t2 <= 1 && arc.IsWithinAngle(f + t2 * r))
            {
                // Hits the arc at t2
                return t2;
            }
            // Doesn't touch the arc
            return null;
        }
        // The line does not hit the circle containing the arc
        return null;
    }

    /// <summary>
    /// Returns how far along a given segment hits a given quadratic curve
    /// </summary>
    public static double? SegmentCurve(Segment s, Curve curve)
    {
        Vec2 r = s.End - s.Start;
        double a = Vec2.Cross(curve.Bend, r);
        if (Math.Abs(r.LengthSquared) <= Vec2.Epsilon * Vec2.Epsilon) {return null;}
        double b = Vec2.Cross(curve.Control - curve.Start, r);
        double c = Vec2.Cross(curve.Start - s.Start, r);
        if (Math.Abs(a) > Vec2.Epsilon * curve.Bend.Length * r.Length)
        {
            // Not the special case where a is approximately 0
            double disc = b * b - a * c;
            if (disc >= 0)
            {
                // The line might hit the curve
                double sqrtDisc = Math.Sqrt(disc);
                double t1 = (-b - sqrtDisc) / a;
                double t2 = (-b + sqrtDisc) / a;
                double? best = null;
                // Get u1 and u2 values
                if (0 <= t1 && t1 <= 1)
                {
                    Vec2 hit = curve.PointAt(t1);
                    double u = Vec2.Dot(hit - s.Start, r) / r.LengthSquared;
                    if (0 <= u && u <= 1)
                    {
                        if (best == null || u < best) {best = u;}
                    }
                }
                if (0 <= t2 && t2 <= 1)
                {
                    Vec2 hit = curve.PointAt(t2);
                    double u = Vec2.Dot(hit - s.Start, r) / r.LengthSquared;
                    if (0 <= u && u <= 1)
                    {
                        if (best == null || u < best) {best = u;}
                    }
                }

                return best;
            }
            // Doesn't hit the curve
            return null;
        }
        else
        {
            if (Math.Abs(b) <= Vec2.Epsilon * (curve.Control - curve.Start).Length * r.Length) {return null;}
            // Special case: a is approximately 0
            double t = -1 * c / (2 * b);
            if (0 <= t && t <= 1)
            {
                Vec2 hit = curve.PointAt(t);
                double u = Vec2.Dot(hit - s.Start, r) / r.LengthSquared;
                if (0 <= u && u <= 1)
                {
                    // There is a valid hit along the curve
                    return u;
                }
            }
            // The segment does not hit the curve
            return null;
        }
    }

    /// <summary>
    /// Returns whether or not a given circle and quadratic curve overlap
    /// </summary>
    public static bool CircleCurve(Circle circ, Curve curve)
    {
        Vec2 closestPointToCenter = curve.ClosestPoint(circ.Center);
        return PointInCircle(closestPointToCenter, circ);
    }
}