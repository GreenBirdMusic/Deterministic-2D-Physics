// AI-GEN AI-013: tests for Christopher's Overlap.SegmentSegment, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class SegmentSegmentTests
{
    static Segment Seg(double ax, double ay, double bx, double by) => new(new Vec2(ax, ay), new Vec2(bx, by));

    static void AssertHit(double expectedT, double? actual)
    {
        Assert.True(actual.HasValue, "expected a hit, got none");
        Assert.Equal(expectedT, actual!.Value, 1e-12);
    }

    [Fact]
    public void CrossingSegments()
    {
        AssertHit(0.5, Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(5, -5, 5, 5)));      // the lesson's example
        AssertHit(0.25, Overlap.SegmentSegment(Seg(0, 0, 8, 8), Seg(0, 4, 4, 0)));       // diagonal
    }

    [Fact]
    public void TIsAlongTheFirstSegment()
    {
        var a = Seg(0, 0, 10, 0);
        var b = Seg(2, -1, 2, 1);
        AssertHit(0.2, Overlap.SegmentSegment(a, b));    // 2 of 10 along a
        AssertHit(0.5, Overlap.SegmentSegment(b, a));    // halfway along b
    }

    [Fact]
    public void MissingSegments()
    {
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(5, 1, 5, 5)));      // would cross if longer
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 4, 0), Seg(5, -5, 5, 5)));      // the other one would need to be longer
    }

    [Fact]
    public void TouchingAtAnEndCounts()
    {
        AssertHit(1, Overlap.SegmentSegment(Seg(0, 0, 5, 0), Seg(5, -5, 5, 5)));     // T-shape: a's end on b
        AssertHit(0.5, Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(5, 0, 5, 5)));   // T-shape: b's end on a
        AssertHit(0, Overlap.SegmentSegment(Seg(0, 0, 5, 0), Seg(0, 0, 0, 5)));      // corner: shared start
    }

    [Fact]
    public void ParallelApartNeverHits()
    {
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(0, 1, 10, 1)));
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(10, 1e-3, 0, 1e-3)));   // close but clearly apart
    }

    [Fact]
    public void CollinearOverlapGivesFirstT()
    {
        var a = Seg(0, 0, 10, 0);
        AssertHit(0.3, Overlap.SegmentSegment(a, Seg(3, 0, 6, 0)));      // inside a
        AssertHit(0.3, Overlap.SegmentSegment(a, Seg(6, 0, 3, 0)));      // same, reversed
        AssertHit(0, Overlap.SegmentSegment(a, Seg(-5, 0, 2, 0)));       // starts before a
        AssertHit(0, Overlap.SegmentSegment(a, Seg(-5, 0, 20, 0)));      // covers all of a
        AssertHit(0.8, Overlap.SegmentSegment(a, Seg(20, 0, 8, 0)));     // comes from beyond a's end
        AssertHit(1, Overlap.SegmentSegment(a, Seg(10, 0, 15, 0)));      // only touching a's end
    }

    [Fact]
    public void CollinearWithoutOverlapMisses()
    {
        var a = Seg(0, 0, 10, 0);
        Assert.Null(Overlap.SegmentSegment(a, Seg(11, 0, 15, 0)));
        Assert.Null(Overlap.SegmentSegment(a, Seg(-5, 0, -1, 0)));
    }

    [Fact]
    public void NearlyParallelFloorStillHitsReliably()
    {
        // A grapple ray along a floor, off by rounding noise: treated as collinear, not as random noise.
        var ray = Seg(0, 0, 100, 1e-14);
        AssertHit(0.2, Overlap.SegmentSegment(ray, Seg(20, 0, 200, 0)));
    }

    [Fact]
    public void LongNearlyParallelWallsBehaveLikeShortOnes()
    {
        // Same angles, 1000 times bigger: same answer (the threshold is relative).
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(0, 1, 10, 1.001)));
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 10000, 0), Seg(0, 1000, 10000, 1001)));
    }

    [Fact]
    public void ZeroLengthSegments()
    {
        Assert.Null(Overlap.SegmentSegment(Seg(3, 0, 3, 0), Seg(0, 0, 10, 0)));     // first is a point: no hit, no NaN
        AssertHit(0.3, Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(3, 0, 3, 0)));  // second is a point on the first
        Assert.Null(Overlap.SegmentSegment(Seg(0, 0, 10, 0), Seg(3, 1, 3, 1)));     // second is a point off it
    }
}
