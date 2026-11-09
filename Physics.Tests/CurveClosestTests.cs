// AI-GEN AI-013: tests for Christopher's Curve.ClosestPoint and Overlap.CircleCurve, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class CurveClosestTests
{
    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 1e-9 && Math.Abs(expected.Y - actual.Y) < 1e-9,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    // A hump: x = 10t, y = 20t(1 - t). Peak at (5, 5).
    static readonly Curve Hump = new(new Vec2(0, 0), new Vec2(5, 10), new Vec2(10, 0));
    static readonly Curve Straight = new(new Vec2(0, 0), new Vec2(5, 0), new Vec2(10, 0));

    // Brute force: the smallest distance from p to 100001 points along the curve.
    static double BruteDistance(Curve curve, Vec2 p)
    {
        double best = double.MaxValue;
        for (int i = 0; i <= 100000; i++)
            best = Math.Min(best, (curve.PointAt(i / 100000.0) - p).Length);
        return best;
    }

    [Fact]
    public void ClosestPointSimpleCases()
    {
        AssertNear(new Vec2(5, 5), Hump.ClosestPoint(new Vec2(5, 8)));         // straight above the peak
        AssertNear(Hump.PointAt(0.3), Hump.ClosestPoint(Hump.PointAt(0.3)));  // already on the curve
        AssertNear(new Vec2(0, 0), Hump.ClosestPoint(new Vec2(-3, -3)));       // past the start
        AssertNear(new Vec2(10, 0), Hump.ClosestPoint(new Vec2(15, -2)));      // past the end
    }

    [Fact]
    public void ClosestPointIgnoresTheParabolaPastTheEnds()
    {
        // (12, -4.8) is on the parabola at t = 1.2, beyond the curve's end. The closest point
        // on the actual curve is its end.
        AssertNear(new Vec2(10, 0), Hump.ClosestPoint(new Vec2(12, -4.8)));
        AssertNear(new Vec2(0, 0), Hump.ClosestPoint(new Vec2(-2, -4.8)));    // same at the start (t = -0.2)
    }

    [Fact]
    public void ClosestPointMatchesBruteForce()
    {
        // Points all around the hump, including under it where the closest point isn't the peak.
        var points = new[] { new Vec2(5, 1), new Vec2(5, 2.4), new Vec2(2, 1), new Vec2(8, 7), new Vec2(-1, 3),
                             new Vec2(11, 2), new Vec2(5, -3), new Vec2(3, 4.5), new Vec2(12, -4.8) };
        foreach (var p in points)
        {
            double got = (Hump.ClosestPoint(p) - p).Length;
            Assert.True(Math.Abs(got - BruteDistance(Hump, p)) < 1e-6,
                $"from ({p.X}, {p.Y}): got distance {got}, brute force found {BruteDistance(Hump, p)}");
        }
    }

    [Fact]
    public void StraightCurveActsLikeASegment()
    {
        AssertNear(new Vec2(3, 0), Straight.ClosestPoint(new Vec2(3, 4)));
        AssertNear(new Vec2(10, 0), Straight.ClosestPoint(new Vec2(14, 3)));
    }

    [Fact]
    public void CircleCurve()
    {
        Assert.True(Overlap.CircleCurve(new Circle(new Vec2(5, 6), 1.5), Hump));    // 1 above the peak
        Assert.False(Overlap.CircleCurve(new Circle(new Vec2(5, 6), 0.5), Hump));
        Assert.False(Overlap.CircleCurve(new Circle(new Vec2(5, 2), 1), Hump));     // under the hump, not touching
        Assert.False(Overlap.CircleCurve(new Circle(new Vec2(12, -4.8), 1), Hump)); // on the parabola past the end
        Assert.True(Overlap.CircleCurve(new Circle(new Vec2(10.5, 0.5), 1), Hump)); // near the end
    }
}
