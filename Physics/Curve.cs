using System;

namespace Physics;

public readonly record struct Curve(Vec2 Start, Vec2 Control, Vec2 End)
{
    public Vec2 Bend => Start - 2 * Control + End;

    /// <summary>
    /// Returns the point t along the quadratic curve
    /// </summary>
    public Vec2 PointAt(double t)
    {
        return Start + 2 * t * (Control - Start) + t * t * Bend;
    }
}