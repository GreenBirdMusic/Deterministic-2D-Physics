using System;

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

    
}