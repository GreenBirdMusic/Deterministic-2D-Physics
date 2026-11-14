using System;

namespace Physics;

public readonly record struct Curve(Vec2 Start, Vec2 Control, Vec2 End, bool BothSolid = false) : IEdge
{
    public Vec2 Bend => Start - 2 * Control + End;

    /// <summary>
    /// Returns the point t along the quadratic curve
    /// </summary>
    public Vec2 PointAt(double t)
    {
        return Start + 2 * t * (Control - Start) + t * t * Bend;
    }

    /// <summary>
    /// Gets the closest point along the quadratic curve to a given Vec2 point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 p)
    {
        return PointAt(ClosestT(p));
    }

    /// <summary>
    /// Gets the t value for the closest point along the quadratic curve to a given Vec2 point
    /// </summary>
    public double ClosestT(Vec2 p)
    {
        if (Bend.LengthSquared > Vec2.Epsilon * Vec2.Epsilon * (End - Start).LengthSquared)
        {
            Vec2 D = Control - Start;
            Vec2 M = Start - p;
            double[] t_set = Solve.SolveCubic(
                Vec2.Dot(Bend, Bend),
                3 * Vec2.Dot(Bend, D),
                2 * Vec2.Dot(D, D) + Vec2.Dot(M, Bend),
                Vec2.Dot(M, D)
            );
            // Find the closest point
            double best = 0;
            double bestLength = (PointAt(0) - p).LengthSquared;

            double testDist = (PointAt(1) - p).LengthSquared;
            if (testDist < bestLength)
            {
                best = 1;
                bestLength = testDist;
            }

            foreach (double t in t_set)
            {
                if (0 <= t && t <= 1)
                {
                    testDist = (PointAt(t) - p).LengthSquared;
                    if (testDist < bestLength)
                    {
                        best = t;
                        bestLength = testDist;
                    }
                }
            }
            return best;
        }
        else
        {
            Vec2 dir = End - Start;
            if (dir.LengthSquared <= Vec2.Epsilon * Vec2.Epsilon) {return 0;}
            double t = Vec2.Dot(p - Start, dir)/dir.LengthSquared;
            t = Math.Clamp(t, 0, 1);
            return t;
        }
    }

    /// <summary>
    /// The direction that the curve is moving at t
    /// </summary>
    public Vec2 TangentAt(double t)
    {
        Vec2 derivative = 2 * (1 - t) * (Control - Start) + 2 * t * (End - Control);
        if (derivative.LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
        {
            return derivative;
        }
        return End - Start;
    }

    /// <summary>
    /// Returns the normal of the curve at t from 0 (start) to 1 (end)
    /// </summary>
    public Vec2 NormalAt(double t)
    {
        return (-TangentAt(t).Perp()).Normalized();
    }

    public Vec2 NormalAt(Vec2 p)
    {
        return NormalAt(ClosestT(p));
    }

    /// <summary>
    /// Returns whether or not the given Vec2 p is towards the solid side of the curve
    /// </summary>
    public bool IsOnSolidSide(Vec2 p)
    {
        if (BothSolid) {return true;}
        double t = ClosestT(p);
        return Vec2.Cross(TangentAt(t), p - PointAt(t)) > 0;
    }

    /// <summary>
    /// Used for point in loop detection.
    /// Returns whether a point is in the bounds of the shape enclosed by the curve and a line from Start to End
    /// </summary>
    public bool IsInCap(Vec2 p)
    {
        Vec2 A = 2 * (Control - Start);
        double crossAB = Vec2.Cross(A, Bend);
        if (Math.Abs(crossAB) > Vec2.Epsilon * (End - Start).LengthSquared)
        {
            Vec2 q = p - Start;
            double u = Vec2.Cross(q, Bend) / crossAB;
            double v = Vec2.Cross(A, q) / crossAB;
            return u * u < v && v < u;
        }
        return false;
    }

    public bool IsBothSolid() {return BothSolid;}
}