using System;

namespace Physics;

public readonly record struct Segment(Vec2 Start, Vec2 End, bool BothSolid = false) : IEdge
{
    /// <summary>
    /// Gets the closest point along the segment to a given Vec2 point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 p)
    {
        Vec2 dir = End - Start;
        if (dir.LengthSquared <= Vec2.Epsilon * Vec2.Epsilon) {return Start;}
        double t = Vec2.Dot(p - Start, dir)/dir.LengthSquared;
        t = Math.Clamp(t, 0, 1);
        return Start + dir * t; // Didn't use Lerp because this is technically slightly faster
    }

    /// <summary>
    /// The normal of the segment. Points to your left if you stand at Start and face End.
    /// </summary>
    public Vec2 Normal => (-(End - Start).Perp()).Normalized();

    public Vec2 NormalAt(Vec2 p)
    {
        return Normal;
    }

    /// <summary>
    /// Returns whether or not the given Vec2 p is towards the solid side of the line the segment lies on.
    /// </summary>
    public bool IsOnSolidSide(Vec2 p)
    {
        if (BothSolid) {return true;}
        return Vec2.Cross(End - Start, p - Start) > 0;
    }

    /// <summary>
    /// Used for point in loop detection. Returns false for a segment
    /// </summary>
    public bool IsInCap(Vec2 p)
    {
        return false;
    }

    IEdge IEdge.Transformed(Transform t) => Transformed(t);
    public Segment Transformed(Transform t)
    {
        Vec2 start = t.Apply(Start);
        Vec2 end =  t.Apply(End);

        if (t.Mirrored)
        {
            return new Segment(end, start, BothSolid);
        }
        return new Segment(start, end, BothSolid);
    }

    public bool IsBothSolid() {return BothSolid;}

    public double AreaPiece()
    {
        return Vec2.Cross(Start, End)/2;
    }
    
    public Vec2 Moment()
    {
        return Vec2.Cross(Start, End) * (Start + End) / 6;
    }

    public double RotationalInertia()
    {
        return Vec2.Cross(Start, End) * (Vec2.Dot(Start, Start) + Vec2.Dot(Start, End) + Vec2.Dot(End, End)) / 12;
    }
}