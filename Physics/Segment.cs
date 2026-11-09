using System;

namespace Physics;

public readonly record struct Segment(Vec2 Start, Vec2 End)
{
    /// <summary>
    /// Gets the closest point along the segment to a given Vec2 point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 p)
    {
        Vec2 dir = End - Start;
        if (dir.LengthSquared <= 1e-16) {return Start;}
        double t = Vec2.Dot(p - Start, dir)/dir.LengthSquared;
        t = Math.Clamp(t, 0, 1);
        return Start + dir * t; // Didn't use Lerp because this is technically slightly faster
    }
}