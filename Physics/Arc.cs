using System;

namespace Physics;

public readonly record struct Arc(Vec2 Center, double Radius, Vec2 Direction, double HalfAngle)
{
    public Vec2 StartPoint
    {
        get
        {
            double cos = Math.Cos(HalfAngle);
            double sin = Math.Sin(HalfAngle);
            return Center + new Vec2(
                Direction.X * cos - Direction.Y * sin,
                Direction.X * sin + Direction.Y * cos
            ) * Radius;
        }
    }

    public Vec2 EndPoint
    {
        get
        {
            double cos = Math.Cos(HalfAngle);
            double sin = -Math.Sin(HalfAngle);
            return Center + new Vec2(
                Direction.X * cos - Direction.Y * sin,
                Direction.X * sin + Direction.Y * cos
            ) * Radius;
        }
    }

    /// <summary>
    /// Gets the closest point on the arc to a given Vec2 point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 p)
    {
        Vec2 d = p - Center;
        if (d.LengthSquared < Vec2.Epsilon * Vec2.Epsilon)
        {
            // The point and the center are functionally the same point
            return Center + Direction * Radius;
        }
        else
        {
            if (Vec2.Dot(d, Direction) >= Math.Cos(HalfAngle) * d.Length)
            {
                // The point is within the arc's angle
                return Center + d.Normalized() * Radius;
            }
            else
            {
                // The point is outside of the arc's angle
                if ((p - StartPoint).LengthSquared < (p - EndPoint).LengthSquared)
                {
                    return StartPoint;
                }
                else
                {
                    return EndPoint;
                }
            }
        }
    }
}