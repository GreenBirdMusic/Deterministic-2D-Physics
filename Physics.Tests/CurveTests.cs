// AI-GEN AI-013: tests for Christopher's Curve and Overlap.SegmentCurve, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class CurveTests
{
    static Segment Seg(double ax, double ay, double bx, double by) => new(new Vec2(ax, ay), new Vec2(bx, by));

    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 1e-9 && Math.Abs(expected.Y - actual.Y) < 1e-9,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    static void AssertHit(double expectedU, double? actual)
    {
        Assert.True(actual.HasValue, "expected a hit, got none");
        Assert.Equal(expectedU, actual!.Value, 1e-9);
    }

    // A hump: x = 10t, y = 20t(1 - t). Peak at (5, 5).
    static readonly Curve Hump = new(new Vec2(0, 0), new Vec2(5, 10), new Vec2(10, 0));

    // A "curve" that is really a straight line along the x axis (no bend).
    static readonly Curve Straight = new(new Vec2(0, 0), new Vec2(5, 0), new Vec2(10, 0));

    [Fact]
    public void PointAtEndsAndMiddle()
    {
        AssertNear(new Vec2(0, 0), Hump.PointAt(0));
        AssertNear(new Vec2(10, 0), Hump.PointAt(1));
        AssertNear(new Vec2(5, 5), Hump.PointAt(0.5));       // halfway to the control point, not at it
        AssertNear(new Vec2(2, 3.2), Hump.PointAt(0.2));
    }

    [Fact]
    public void SegmentCurveCrossingTwice()
    {
        // y = 3.2 crosses the hump at x = 2 (t = 0.2) and x = 8 (t = 0.8).
        AssertHit(0.35, Overlap.SegmentCurve(Seg(-5, 3.2, 15, 3.2), Hump));   // first hit x = 2
        AssertHit(0.35, Overlap.SegmentCurve(Seg(15, 3.2, -5, 3.2), Hump));   // other way: first hit x = 8 (the larger t)
    }

    [Fact]
    public void SegmentCurveStraightDown()
    {
        // A vertical line is parallel to this hump's bend: the a = 0 special case.
        AssertHit(0.25, Overlap.SegmentCurve(Seg(5, 10, 5, -10), Hump));      // hits the peak (5, 5)
    }

    [Fact]
    public void SegmentCurveMisses()
    {
        Assert.Null(Overlap.SegmentCurve(Seg(-5, 6, 15, 6), Hump));           // above the peak
        Assert.Null(Overlap.SegmentCurve(Seg(-5, -1.8, 15, -1.8), Hump));     // only crosses the parabola past the curve's ends
    }

    [Fact]
    public void SegmentCurveSegmentEndsMatter()
    {
        Assert.Null(Overlap.SegmentCurve(Seg(-5, 3.2, 1, 3.2), Hump));        // stops short of both hits
        AssertHit(5.0 / 12, Overlap.SegmentCurve(Seg(3, 3.2, 15, 3.2), Hump)); // starts past the x = 2 hit: only x = 8 counts
    }

    [Fact]
    public void SegmentCurveSpecialCasePastTheCurveEnd()
    {
        // Vertical line at x = 12: meets the parabola at t = 1.2, beyond the curve's end.
        Assert.Null(Overlap.SegmentCurve(Seg(12, 10, 12, -10), Hump));
    }

    [Fact]
    public void SegmentCurveStraightCurve()
    {
        AssertHit(0.5, Overlap.SegmentCurve(Seg(3, -5, 3, 5), Straight));     // crosses at (3, 0)
        Assert.Null(Overlap.SegmentCurve(Seg(0, 1, 10, 1), Straight));        // parallel, never meets
    }

    [Fact]
    public void SegmentCurveZeroLength()
    {
        Assert.Null(Overlap.SegmentCurve(Seg(5, 5, 5, 5), Hump));
    }
}
