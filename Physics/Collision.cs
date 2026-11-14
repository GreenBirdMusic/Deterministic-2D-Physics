using System;
using System.Collections.Generic;
using System.Linq;

namespace Physics;

public static class Collision
{
    // Used primarily to handle the tolerance for circle collision
    public const double StepTolerance = 1e-6;

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

    public static Hit[]? CircleCircle(Circle a, Circle b, Vec2 v)
    {
        Circle collider = new Circle(b.Center, b.Radius + a.Radius);
        Hit? hit = PointCircle(a.Center, collider, v);
        if (hit != null)
        {
            return [new Hit(hit.Value.T, hit.Value.Point - a.Radius * hit.Value.Normal, hit.Value.Normal)];
        }
        return null;
    }

    public static Hit[]? CircleSegment(Circle c, Segment s, Vec2 v)
    {
        Vec2 q = s.ClosestPoint(c.Center);
        double d = (c.Center - q).Length;
        if (d < c.Radius - StepTolerance) {return null;}
        if (d <= c.Radius)
        {
            Vec2 n = (c.Center - q) / d;
            if (s.BothSolid == true || Vec2.Dot(n, s.Normal) >= 0)
            {
                if (Vec2.Dot(v, n) < 0)
                {
                    return [new Hit(0, c.Center - c.Radius * n, n)];
                }
            }
            return null;
        }
        Hit? best = PointSegment(c.Center, new Segment(s.Start + c.Radius * s.Normal, s.End + c.Radius * s.Normal), v);
        
        void Consider(Hit? h)
        {
            if (h != null && (s.BothSolid || Vec2.Dot(h.Value.Normal, s.Normal) >= 0))
            {
                if (best == null || h.Value.T < best.Value.T)
                {
                    best = h;
                }
            }
        }

        if (s.BothSolid)
        {
            Hit? hitc = PointSegment(c.Center, new Segment(s.End - c.Radius * s.Normal, s.Start - c.Radius * s.Normal), v);
            // hitc because hit is used itself later
            Consider(hitc);
        }
        Hit? hit = PointCircle(c.Center, new Circle(s.Start, c.Radius), v);
        Consider(hit);
        hit = PointCircle(c.Center, new Circle(s.End, c.Radius), v);
        Consider(hit);
        if (best != null)
        {
            // Correct best
            best = new Hit(best.Value.T, best.Value.Point - c.Radius * best.Value.Normal, best.Value.Normal);
            return [best.Value];
        }
        return null;
    }

    public static Hit[]? CircleArc(Circle c, Arc a, Vec2 v)
    {
        Vec2 q = a.ClosestPoint(c.Center);
        double d = (c.Center - q).Length;
        if (d < c.Radius - StepTolerance) {return null;}
        if (d <= c.Radius)
        {
            Vec2 n = (c.Center - q) / d;
            if (a.BothSolid == true || Vec2.Dot(n, a.NormalAt(q)) >= 0)
            {
                if (Vec2.Dot(v, n) < 0)
                {
                    return [new Hit(0, c.Center - c.Radius * n, n)];
                }
            }
            return null;
        }
        List<Hit>? best = null;
        double cRad = a.Radius + c.Radius * Math.Sign(a.HalfAngle);
        if (cRad > 0)
        {
            Hit? hitc = PointArc(c.Center, new Arc(a.Center, cRad, a.Direction, a.HalfAngle), v);
            if (hitc != null)
            {
                best = [hitc.Value];
            }
        }

        void Consider(Hit? h, Vec2 capPoint)
        {
            if (h != null && (a.BothSolid || Vec2.Dot(h.Value.Normal, a.NormalAt(capPoint)) >= 0))
            {
                if (best != null && Math.Abs(h.Value.T - best[0].T) < Vec2.Epsilon)
                {
                    foreach (Hit hit in best)
                    {
                        if ((h.Value.Normal - hit.Normal).LengthSquared < Vec2.Epsilon * Vec2.Epsilon) {return;}
                    }
                    best.Add(h.Value);
                }
                else if (best == null || h.Value.T < best[0].T)
                {
                    best = [h.Value];
                }
            }
        }

        if (a.BothSolid)
        {
            cRad = a.Radius - c.Radius * Math.Sign(a.HalfAngle);
            if (cRad > 0)
            {
                Hit? hitc = PointArc(c.Center, new Arc(a.Center, cRad, a.Direction, -a.HalfAngle), v);
                // hitc because hit is used itself later
                if (hitc != null) {Consider(hitc, hitc.Value.Point);}
            }
        }
        Hit? hit = PointCircle(c.Center, new Circle(a.Start, c.Radius), v);
        Consider(hit, a.Start);
        hit = PointCircle(c.Center, new Circle(a.End, c.Radius), v);
        Consider(hit, a.End);
        if (best != null)
        {
            // Correct best
            for (int i = 0; i < best.Count; i++)
            {
                best[i] = new Hit(best[i].T, best[i].Point - c.Radius * best[i].Normal, best[i].Normal);
            }
            return best.ToArray();
        }
        return null;
    }

    static Hit[]? CircleEdge(Circle c, IEdge a, Vec2 v) => a switch
    {
        Segment s => CircleSegment(c, s, v),
        Arc r => CircleArc(c, r, v),
        Curve cr => throw new NotImplementedException(),
        _ => throw new NotImplementedException()
    };

    public static Hit[]? CircleLoop(Circle c, IReadOnlyList<IEdge> loop, Vec2 v)
    {
        List<Hit>? best = null;
        foreach (IEdge edge in loop)
        {
            Hit[]? hits = CircleEdge(c, edge, v);
            if (hits != null)
            {
                if (best == null)
                {
                    best = hits.ToList();
                }
                else
                {
                    if (Math.Abs(hits[0].T - best[0].T) < Vec2.Epsilon)
                    {
                        foreach (Hit h in hits)
                        {
                            bool unique = true;
                            foreach (Hit hit in best)
                            {
                                if ((h.Normal - hit.Normal).LengthSquared < Vec2.Epsilon * Vec2.Epsilon) 
                                {
                                    unique = false;
                                    break;
                                }
                            }
                            if (unique) {best.Add(h);}
                        }
                    }
                    else if (hits[0].T < best[0].T)
                    {
                        best = hits.ToList();
                    }
                }
            }
        }
        if (best != null)
        {
            return best.ToArray();
        }
        return null;
    }

    private static List<Hit>? BestCollision(List<Hit>? best, Vec2 p, IEdge edge, Vec2 vel, IEdge source, bool flip)
    {
        Hit? h = PointEdge(p, edge, vel);
        if (h == null && (p - edge.ClosestPoint(p)).LengthSquared <= Vec2.Epsilon * Vec2.Epsilon)
        {
            Vec2 n = edge.NormalAt(p);
            double velDot = Vec2.Dot(vel, n);
            if (edge.IsBothSolid() && velDot > 0)
            {
                n = -n;
                velDot = -velDot;
            }
            if (velDot < 0)
            {
                h = new Hit(0, p, n);
            }
        }
        if (h != null && (source.IsBothSolid() || Vec2.Dot(h.Value.Normal, source.NormalAt(p)) <= 0))
        {
            if (flip) {h = new Hit(h.Value.T, p, -h.Value.Normal);}
            return AddHit(best, h.Value);
        }
        return best;
    }

    private static List<Hit>? AddHit(List<Hit>? best, Hit h)
    {
        if (best != null && Math.Abs(h.T - best[0].T) < Vec2.Epsilon)
        {
            foreach (Hit hit in best)
            {
                if (
                    (h.Normal - hit.Normal).LengthSquared < Vec2.Epsilon * Vec2.Epsilon &&
                    (hit.Point - h.Point).LengthSquared < Vec2.Epsilon * Vec2.Epsilon
                ) {return best;}
            }
            best.Add(new Hit(h.T, h.Point, h.Normal));
        }
        else if (best == null || h.T < best[0].T)
        {
            best = [new Hit(h.T, h.Point, h.Normal)];
        }
        return best;
    }

    public static Hit[]? SegmentSegment(Segment s, Segment t, Vec2 v)
    {
        if (Overlap.SegmentSegment(s, t) != null &&
            (s.Start - t.ClosestPoint(s.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (s.End - t.ClosestPoint(s.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (t.Start - s.ClosestPoint(t.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (t.End - s.ClosestPoint(t.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon
        ) {return null;}
        List<Hit>? best = null;
        best = BestCollision(best, s.Start, t, v, s, false);
        best = BestCollision(best, s.End, t, v, s, false);
        best = BestCollision(best, t.Start, s, -v, t, true);
        best = BestCollision(best, t.End, s, -v, t, true);
        if (best != null)
        {
            return best.ToArray();
        }
        return null;
    }

    public static Hit[]? SegmentArc(Segment s, Arc a, Vec2 v)
    {
        Vec2 ext1 = a.Center + a.Radius * s.Normal;
        Vec2 ext2 = a.Center - a.Radius * s.Normal;
        if (Overlap.SegmentArc(s, a) != null &&
            (s.Start - a.ClosestPoint(s.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (s.End - a.ClosestPoint(s.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (a.Start - s.ClosestPoint(a.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (a.End - s.ClosestPoint(a.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (a.IsWithinAngle(ext1 - a.Center) == false || (ext1 - s.ClosestPoint(ext1)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon) &&
            (a.IsWithinAngle(ext2 - a.Center) == false || (ext2 - s.ClosestPoint(ext2)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
        ) {return null;}
        List<Hit>? best = null;
        best = BestCollision(best, s.Start, a, v, s, false);
        best = BestCollision(best, s.End, a, v, s, false);
        best = BestCollision(best, a.Start, s, -v, a, true);
        best = BestCollision(best, a.End, s, -v, a, true);
        if (a.IsWithinAngle(ext1 - a.Center))
        {
            best = BestCollision(best, ext1, s, -v, a, true);
        }
        if (a.IsWithinAngle(ext2 - a.Center))
        {
            best = BestCollision(best, ext2, s, -v, a, true);
        }
        if (best != null)
        {
            return best.ToArray();
        }
        return null;
    }

    public static Hit[]? ArcArc(Arc a, Arc b, Vec2 v)
    {
        Vec2 u = (b.Center - a.Center).Normalized();
        Vec2 ext1 = a.Center + a.Radius * u;
        Vec2 ext2 = a.Center - a.Radius * u;
        Vec2 ext3 = b.Center + b.Radius * u;
        Vec2 ext4 = b.Center - b.Radius * u;
        if (Overlap.ArcArc(a, b) &&
            (a.Start - b.ClosestPoint(a.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (a.End - b.ClosestPoint(a.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (b.Start - a.ClosestPoint(b.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (b.End - a.ClosestPoint(b.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (a.IsWithinAngle(ext1 - a.Center) == false || (ext1 - b.ClosestPoint(ext1)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon) &&
            (a.IsWithinAngle(ext2 - a.Center) == false || (ext2 - b.ClosestPoint(ext2)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon) &&
            (b.IsWithinAngle(ext3 - b.Center) == false || (ext3 - a.ClosestPoint(ext3)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon) &&
            (b.IsWithinAngle(ext4 - b.Center) == false || (ext4 - a.ClosestPoint(ext4)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
        ) {return null;}
        List<Hit>? best = null;
        best = BestCollision(best, a.Start, b, v, a, false);
        best = BestCollision(best, a.End, b, v, a, false);
        best = BestCollision(best, b.Start, a, -v, b, true);
        best = BestCollision(best, b.End, a, -v, b, true);
        
        double A = v.LengthSquared; // Dot(v, v) = v^2

        if (A > Vec2.Epsilon * Vec2.Epsilon)
        {
            Vec2 f = a.Center - b.Center;
            double B = Vec2.Dot(f, v);

            void handleT(double t, int sP, int sN)
            {
                if (0 <= t && t <= 1)
                {
                    Vec2 ca = a.Center + t * v;
                    Vec2 cu = (ca - b.Center).Normalized();
                    Vec2 P = b.Center + sP * b.Radius * cu;
                    Vec2 n = sN * cu;
                    if (Vec2.Dot(v, n) < 0 &&
                        b.IsWithinAngle(P - b.Center) &&
                        a.IsWithinAngle(P - ca) &&
                        (b.BothSolid || Vec2.Dot(n, b.NormalAt(P)) >= 0) &&
                        (a.BothSolid || Vec2.Dot(-n, new Arc(ca, a.Radius, a.Direction, a.HalfAngle).NormalAt(P)) >= 0)
                    )
                    {
                        best = AddHit(best, new Hit(t, P, n));
                    }
                }
            }
            void Tangent(double R, int sP, int sN)
            {
                if (Math.Abs(f.Length - R) <= Vec2.Epsilon) {
                    handleT(0, sP, sN);
                }
                double C = f.LengthSquared - R * R;
                double disc = B * B - A * C;
                if (disc >= 0)
                {
                    double t1 = (-B - Math.Sqrt(disc)) / A;
                    double t2 = (-B + Math.Sqrt(disc)) / A;
                    
                    handleT(t1, sP, sN);
                    handleT(t2, sP, sN);
                }
            }

            Tangent(a.Radius + b.Radius, 1, 1);

            double R = Math.Abs(a.Radius - b.Radius);
            if (R > Vec2.Epsilon)
            {
                if (b.Radius > a.Radius)
                {
                    Tangent(R, 1, -1);
                }
                else
                {
                    // Note that we must have a.Radius > b.Radius
                    Tangent(R, -1, -1);
                }
            }
        }

        if (best != null)
        {
            return best.ToArray();
        }
        return null;
    }

    public static Hit[]? SegmentCurve(Segment s, Curve c, Vec2 v)
    {
        double? t = null;
        Vec2? ext = null;
        if (Math.Abs(Vec2.Dot(c.Bend, s.Normal)) > Vec2.Epsilon * Vec2.Epsilon) 
        {
            t = -Vec2.Dot(c.Control - c.Start, s.Normal) / Vec2.Dot(c.Bend, s.Normal);
            ext = c.PointAt(t.Value);
        }
        if (Overlap.SegmentCurve(s, c) != null &&
            (s.Start - c.ClosestPoint(s.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (s.End - c.ClosestPoint(s.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (c.Start - s.ClosestPoint(c.Start)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (c.End - s.ClosestPoint(c.End)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon &&
            (t == null || ext == null || t.Value < 0 || 1 < t.Value || (ext.Value - s.ClosestPoint(ext.Value)).LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
        ) {return null;}
        List<Hit>? best = null;
        best = BestCollision(best, s.Start, c, v, s, false);
        best = BestCollision(best, s.End, c, v, s, false);
        best = BestCollision(best, c.Start, s, -v, c, true);
        best = BestCollision(best, c.End, s, -v, c, true);
        if (t != null && ext != null && 0 <= t.Value && t.Value <= 1)
        {
            best = BestCollision(best, ext.Value, s, -v, c, true);
        }
        if (best != null)
        {
            return best.ToArray();
        }
        return null;
    }

    static Hit[]? ReverseHits(Hit[]? hits, Vec2 v)
    {
        if (hits == null) {return hits;}
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i] = hits[i] with {Normal = -hits[i].Normal, Point = hits[i].Point + hits[i].T * v};
        }
        return hits;
    }

    static Hit[]? EdgesCollide(IEdge a, IEdge b, Vec2 v) => (a, b) switch
    {
        (Segment s, Segment t) => SegmentSegment(s, t, v),
        (Segment s, Arc r) => SegmentArc(s, r, v),
        (Segment s, Curve c) => SegmentCurve(s, c, v),
        (Arc r, Segment s) => ReverseHits(SegmentArc(s, r, -v), v),
        (Arc r, Arc ar) => ArcArc(r, ar, v),
        (Arc r, Curve c) => throw new NotImplementedException(),
        (Curve c, Segment s) => ReverseHits(SegmentCurve(s, c, -v), v),
        (Curve c, Arc r) => throw new NotImplementedException(),
        (Curve c, Curve cv) => throw new NotImplementedException(),
        _ => throw new NotImplementedException()
    };

    public static Hit[]? LoopLoop(IReadOnlyList<IEdge> a, IReadOnlyList<IEdge> b, Vec2 v)
    {
        List<Hit>? best = null;
        foreach (IEdge edgeA in a)
        {
            foreach (IEdge edgeB in b)
            {
                Hit[]? hits = EdgesCollide(edgeA, edgeB, v);
                if (hits != null)
                {
                    foreach (Hit hit in hits)
                    {
                        best = AddHit(best, hit);
                    }
                }
            }
        }
        if (best != null)
        {
            return best.ToArray();
        }
        return null;
    }
}