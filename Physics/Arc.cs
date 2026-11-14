using System;

namespace Physics;

/// <summary>
/// A segment of a circle, determined by Center and Radius to define the circle,
/// and Direction with HalfAngle to determine the segment.
/// Half angle determines how collision works (read Half Angle parameter for explanation).
/// </summary>
/// <param name="Direction">Unit vector from the center to the middle of the arc.</param>
/// <param name="HalfAngle">Angle (in radians, -pi to pi) from the middle of the arc to each end.
/// Sign determines interaction:
/// positive prevents movement from outside to inside.
/// negative prevents movement from inside to outside.
/// BothSolid overrides this behavior and prevents movement from both directions.
/// </param>
public readonly record struct Arc(Vec2 Center, double Radius, Vec2 Direction, double HalfAngle, bool BothSolid = false) : IEdge
{
    public Vec2 Start
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

    public Vec2 End
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
            if (IsWithinAngle(d))
            {
                // The point is within the arc's angle
                return Center + d.Normalized() * Radius;
            }
            else
            {
                // The point is outside of the arc's angle
                if ((p - Start).LengthSquared < (p - End).LengthSquared)
                {
                    return Start;
                }
                return End;
            }
        }
    }

    /// <summary>
    /// Returns whether d is in the arc's angle
    /// </summary>
    public bool IsWithinAngle(Vec2 d)
    {
        return Vec2.Dot(d, Direction) >= (Math.Cos(HalfAngle) - Vec2.Epsilon) * d.Length;
    }

    /// <summary>
    /// Returns the normal given a point p (which must be on the arc for this to make sense)
    /// </summary>
    public Vec2 NormalAt(Vec2 p)
    {
        Vec2 r = (p - Center).Normalized();
        if (HalfAngle > 0) {return r;}
        return -r;
    }

    /// <summary>
    /// Returns whether or not the given Vec2 p is towards the solid side of the circle containing this arc
    /// </summary>
    public bool IsOnSolidSide(Vec2 p)
    {
        if (BothSolid) {return true;}
        if (HalfAngle > 0)
        {
            return (p - Center).LengthSquared < Radius * Radius;
        }
        return (p - Center).LengthSquared > Radius * Radius;
    }

    /// <summary>
    /// Used for point in loop detection.
    /// Returns whether a point is in the bounds of the shape enclosed by the arc and a line from Start to End
    /// </summary>
    public bool IsInCap(Vec2 p)
    {
        Vec2 d = p - Center;
        if (d.LengthSquared < Radius * Radius)
        {
            return Vec2.Dot(d, Direction) > Radius * Math.Cos(HalfAngle);
        }
        return false;
    }

    public bool IsBothSolid() {return BothSolid;}

    IEdge IEdge.Transformed(Transform t) => Transformed(t);
    public Arc Transformed(Transform t)
    {
        return new Arc(t.Apply(Center), Radius, t.ApplyDirection(Direction), HalfAngle, BothSolid);
    }

    public double AreaPiece()
    {
        return Vec2.Cross(Start, End)/2 + Radius * Radius * (HalfAngle * 2 - Math.Sin(HalfAngle * 2)) / 2;
    }

    public Vec2 Moment()
    {
        double sin = Math.Sin(HalfAngle);
        return Vec2.Cross(Start, End) * (Start + End) / 6 + Center * 
        Radius * Radius * (HalfAngle * 2 - Math.Sin(HalfAngle * 2)) / 2 + Direction * 2/3 *
        Radius * Radius * Radius * sin * sin * sin;
    }

    public double RotationalInertia()
    {
        double sin = Math.Sin(HalfAngle);
        double r4 = Radius * Radius * Radius * Radius;
        return Vec2.Cross(Start, End) * (Vec2.Dot(Start, Start) + Vec2.Dot(Start, End) + Vec2.Dot(End, End)) / 12 +
        r4 * HalfAngle / 2 - r4 * Math.Sin(2 * HalfAngle) * (2 + Math.Cos(2 * HalfAngle)) / 12 + Radius * Radius * (HalfAngle * 2 - Math.Sin(HalfAngle * 2)) / 2 * Center.LengthSquared + 4 *
        Radius * Radius * Radius * sin * sin * sin * Vec2.Dot(Center, Direction) / 3;
    }
}