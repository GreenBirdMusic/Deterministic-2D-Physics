using System;
using System.Collections.Generic;

namespace Physics;

public static class Collision
{
    public static Hit? PointSegment(Vec2 p, Segment s, Vec2 v)
    {
        Vec2 n = s.Normal;
        double d = Vec2.Dot(v, n);
        if (d > 0 && s.BothSolid)
        {
            n = -n;
            d = -d;
        }
        if (d >= 0)
        {
            return null;
        }
        double? T = Overlap.SegmentSegment(new Segment(p, p + v), s);
        if (T == null) {return null;}
        return new Hit(T.Value, p + T.Value * v, n);
    }

    public static Hit? PointCircle(Vec2 p, Circle c, Vec2 v)
    {
        double? T = Overlap.SegmentCircle(new Segment(p,p + v), c);
        // Circles are always solid, so the first hit always counts
        if (T == null) {return null;}
        Vec2 n;
        if ((p - c.Center).LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
        {
            n = (p + T.Value * v - c.Center).Normalized();
        }
        else
        {
            n = v.Normalized();
        }
        if (Vec2.Dot(v, n) < 0)
        {
            return new Hit(T.Value, p + T.Value * v, n);
        }
        return null;
    }

    public static Hit? PointArc(Vec2 p, Arc arc, Vec2 v)
    {
        Vec2 f = p - arc.Center;
        double a = v.LengthSquared;
        if (a <= Vec2.Epsilon * Vec2.Epsilon) {return null;}
        double b = Vec2.Dot(f, v);
        double c = f.LengthSquared - arc.Radius * arc.Radius;
        double disc = b * b - a * c;
        if (disc >= 0)
        {
            // The line might hit the arc
            double sqrtDisc = Math.Sqrt(disc);
            double t1 = (-b - sqrtDisc) / a;
            double t2 = (-b + sqrtDisc) / a;
            Vec2 n = arc.NormalAt(p + t1 * v);
            double nDot = Vec2.Dot(v, n);
            if (arc.BothSolid && nDot > 0) {
                nDot = -nDot;
                n = -n;
            }
            if (t1 >= 0 && t1 <= 1 && arc.IsWithinAngle(f + t1 * v) && nDot < 0)
            {
                // Collides with the arc at t1
                
                return new Hit(t1, p + t1 * v, n);
            }
            n = arc.NormalAt(p + t2 * v);
            nDot = Vec2.Dot(v, n);
            if (arc.BothSolid && nDot > 0) {
                nDot = -nDot;
                n = -n;
            }
            if (t2 >= 0 && t2 <= 1 && arc.IsWithinAngle(f + t2 * v) && nDot < 0)
            {
                // Collides with the arc at t2
                return new Hit(t2, p + t2 * v, n);
            }
            // Doesn't touch the arc
            return null;
        }
        // The line does not hit the circle containing the arc
        return null;
    }

    public static Hit? PointCurve(Vec2 p, Curve curve, Vec2 v)
    {
        // Note: If a major change to this function is ever made, create
        // a helper to find all hit points t and implement it in both
        // Collision.PointCurve and Overlap.SegmentCurve
        double a = Vec2.Cross(curve.Bend, v);
        if (Math.Abs(v.LengthSquared) <= Vec2.Epsilon * Vec2.Epsilon) {return null;}
        double b = Vec2.Cross(curve.Control - curve.Start, v);
        double c = Vec2.Cross(curve.Start - p, v);
        if (Math.Abs(a) > Vec2.Epsilon * curve.Bend.Length * v.Length)
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
                Vec2? nBest = null;
                // Get u1 and u2 values
                Vec2 n = curve.NormalAt(t1);
                double nDot = Vec2.Dot(v, n);
                if (curve.BothSolid && nDot > 0) 
                {
                    nDot = -nDot;
                    n = -n;
                }
                if (-Vec2.Epsilon <= t1 && t1 <= 1 + Vec2.Epsilon && nDot < 0)
                {
                    Vec2 hit = curve.PointAt(t1);
                    double u = Vec2.Dot(hit - p, v) / v.LengthSquared;
                    if (0 <= u && u <= 1)
                    {
                        if (best == null || u < best)
                        {
                            best = u;
                            nBest = n;
                        }
                    }
                }
                n = curve.NormalAt(t2);
                nDot = Vec2.Dot(v, n);
                if (curve.BothSolid && nDot > 0) 
                {
                    nDot = -nDot;
                    n = -n;
                }
                if (-Vec2.Epsilon <= t2 && t2 <= 1 + Vec2.Epsilon && nDot < 0)
                {
                    Vec2 hit = curve.PointAt(t2);
                    double u = Vec2.Dot(hit - p, v) / v.LengthSquared;
                    if (0 <= u && u <= 1)
                    {
                        if (best == null || u < best)
                        {
                            best = u;
                            nBest = n;
                        }
                    }
                }

                if (best != null && nBest != null) // nBest != null just to prevent an annoying warning
                {
                    return new Hit(best.Value, p + best.Value * v, nBest.Value);
                }
            }
            // Doesn't hit the curve
            return null;
        }
        else
        {
            if (Math.Abs(b) <= Vec2.Epsilon * (curve.Control - curve.Start).Length * v.Length) {return null;}
            // Special case: a is approximately 0
            double t = -1 * c / (2 * b);
            Vec2 n = curve.NormalAt(t);
            double nDot = Vec2.Dot(v, n);
            if (curve.BothSolid && nDot > 0) 
            {
                nDot = -nDot;
                n = -n;
            }
            if (-Vec2.Epsilon <= t && t <= 1 + Vec2.Epsilon && nDot < 0)
            {
                Vec2 hit = curve.PointAt(t);
                double u = Vec2.Dot(hit - p, v) / v.LengthSquared;
                if (0 <= u && u <= 1)
                {
                    // There is a valid hit along the curve
                    return new Hit(u, p + u * v, n);
                }
            }
            // The segment does not hit the curve
            return null;
        }
    }

    static Hit? PointEdge(Vec2 p, IEdge a, Vec2 v) => a switch
    {
        Segment s => PointSegment(p, s, v),
        Arc r => PointArc(p, r, v),
        Curve cr => PointCurve(p, cr, v),
        _ => throw new NotImplementedException()
    };

    public static Hit? PointLoop(Vec2 p, IReadOnlyList<IEdge> loop, Vec2 v)
    {
        Hit? firstHit = null;
        foreach (IEdge edge in loop)
        {
            Hit? hit = PointEdge(p, edge, v);
            if (hit != null && firstHit != null && Math.Abs(hit.Value.T - firstHit.Value.T) <= Vec2.Epsilon)
            {
                firstHit = new Hit(hit.Value.T, hit.Value.Point, (hit.Value.Normal + firstHit.Value.Normal).Normalized());
            }
            else if (hit != null && (firstHit == null || hit.Value.T < firstHit.Value.T))
            {
                firstHit = hit;
            }
        }
        return firstHit;
    }
}