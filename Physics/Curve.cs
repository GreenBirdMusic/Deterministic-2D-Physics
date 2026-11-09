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

    /// <summary>
    /// Gets the closest point along the quadratic curve to a given Vec2 point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 p)
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
            return PointAt(best);
        }
        else
        {
            return new Segment(Start, End).ClosestPoint(p);
        }
    }
}