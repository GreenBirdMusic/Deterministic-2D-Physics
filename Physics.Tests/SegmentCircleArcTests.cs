// AI-GEN AI-013: tests for Christopher's Overlap.SegmentCircle and Overlap.SegmentArc, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class SegmentCircleArcTests
{
    static Segment Seg(double ax, double ay, double bx, double by) => new(new Vec2(ax, ay), new Vec2(bx, by));

    static void AssertHit(double expectedT, double? actual)
    {
        Assert.True(actual.HasValue, "expected a hit, got none");
        Assert.Equal(expectedT, actual!.Value, 1e-9);
    }

    static readonly Circle Unit = new(new Vec2(0, 0), 1);

    // Top half of a radius-2 circle around the origin.
    static readonly Arc TopHalf = new(new Vec2(0, 0), 2, new Vec2(0, 1), Math.PI / 2);

    [Fact]
    public void SegmentCircleThroughTheMiddle()
    {
        AssertHit(0.4, Overlap.SegmentCircle(Seg(-5, 0, 5, 0), Unit));      // enters at x = -1
        AssertHit(0.4, Overlap.SegmentCircle(Seg(5, 0, -5, 0), Unit));      // other way: enters at x = 1
        AssertHit(0.8, Overlap.SegmentCircle(Seg(-5, 0, 0, 0), Unit));      // ends inside
        var off = new Circle(new Vec2(10, 10), 2);
        AssertHit(0.4, Overlap.SegmentCircle(Seg(10, 0, 10, 20), off));     // enters at y = 8
    }

    [Fact]
    public void SegmentCircleOffCenter()
    {
        // y = 0.6 crosses the unit circle at x = -0.8 (a 3-4-5 triangle).
        AssertHit(0.42, Overlap.SegmentCircle(Seg(-5, 0.6, 5, 0.6), Unit));
    }

    [Fact]
    public void SegmentCircleStartingInsideIsZero()
    {
        AssertHit(0, Overlap.SegmentCircle(Seg(0, 0, 5, 0), Unit));
        AssertHit(0, Overlap.SegmentCircle(Seg(-0.5, 0, 0.5, 0), Unit));    // entirely inside: the circle is filled
    }

    [Fact]
    public void SegmentCircleMisses()
    {
        Assert.Null(Overlap.SegmentCircle(Seg(-5, 2, 5, 2), Unit));          // line misses
        Assert.Null(Overlap.SegmentCircle(Seg(-5, 0, -2, 0), Unit));         // stops short
        Assert.Null(Overlap.SegmentCircle(Seg(2, 0, 5, 0), Unit));           // circle is behind it
        Assert.Null(Overlap.SegmentCircle(Seg(3, 3, 3, 3), Unit));           // zero length
    }

    [Fact]
    public void SegmentArcFromAbove()
    {
        AssertHit(0.3, Overlap.SegmentArc(Seg(0, 5, 0, -5), TopHalf));      // hits the top at y = 2
        double tExpected = (5 - Math.Sqrt(3)) / 10;                            // y = 1 crosses at x = -sqrt(3)
        AssertHit(tExpected, Overlap.SegmentArc(Seg(-5, 1, 5, 1), TopHalf));
    }

    [Fact]
    public void SegmentArcSkipsTheMissingHalf()
    {
        // Going up: enters the circle at y = -2 (missing half), leaves through the arc at y = 2.
        AssertHit(0.7, Overlap.SegmentArc(Seg(0, -5, 0, 5), TopHalf));
        Assert.Null(Overlap.SegmentArc(Seg(-5, -1, 5, -1), TopHalf));       // only crosses the missing half
    }

    [Fact]
    public void SegmentArcIsOnlyTheEdge()
    {
        // Unlike a circle, starting inside isn't a hit at 0: the first hit is where it reaches the edge.
        AssertHit(0.4, Overlap.SegmentArc(Seg(0, 0, 0, 5), TopHalf));
        Assert.Null(Overlap.SegmentArc(Seg(-0.5, 0.5, 0.5, 0.5), TopHalf)); // entirely inside: touches nothing
        Assert.Null(Overlap.SegmentArc(Seg(0, 5, 0, 3), TopHalf));          // stops short
    }

    [Fact]
    public void SegmentArcNarrowArc()
    {
        // Radius 1 around (5, 5), facing right, 45 degrees each way.
        var quarter = new Arc(new Vec2(5, 5), 1, new Vec2(1, 0), Math.PI / 4);
        AssertHit(1.0 / 3, Overlap.SegmentArc(Seg(5, 5, 8, 5), quarter));   // from the center out to (6, 5)
        Assert.Null(Overlap.SegmentArc(Seg(5, 3, 5, 7), quarter));          // crosses the circle at the top and bottom, outside the angle
    }

    [Fact]
    public void SegmentArcZeroLength()
    {
        Assert.Null(Overlap.SegmentArc(Seg(0, 2, 0, 2), TopHalf));
    }
}
