// AI-GEN AI-013: tests for Christopher's moving checks (Collision.PointSegment so far), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Solid on the right of Start -> End; normals point to the open side.
public class CollisionTests
{
    static Segment Seg(double ax, double ay, double bx, double by, bool bothSolid = false) =>
        new(new Vec2(ax, ay), new Vec2(bx, by), bothSolid);

    static readonly Segment Floor = Seg(0, 0, 10, 0);   // solid below, normal up
    static readonly Segment Wall = Seg(0, 10, 0, 0);    // drawn upward: solid on the right, normal left

    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.Equal(expected.X, actual.X, 1e-9);
        Assert.Equal(expected.Y, actual.Y, 1e-9);
    }

    static void AssertHit(double t, Vec2 point, Vec2 normal, Hit? actual)
    {
        Assert.True(actual.HasValue, "expected a hit");
        Hit h = actual.Value;
        Assert.Equal(t, h.T, 1e-9);
        AssertNear(point, h.Point);
        AssertNear(normal, h.Normal);
    }

    // ---- PointSegment ----

    [Fact]
    public void LandingOnAFloor()
    {
        AssertHit(0.5, new Vec2(5, 0), new Vec2(0, -1), Collision.PointSegment(new Vec2(5, -5), Floor, new Vec2(0, 10)));
        AssertHit(0.5, new Vec2(4, 0), new Vec2(0, -1), Collision.PointSegment(new Vec2(0, -4), Floor, new Vec2(8, 8)));  // diagonal
    }

    [Fact]
    public void RunningIntoAWall()
    {
        AssertHit(0.5, new Vec2(0, 5), new Vec2(-1, 0), Collision.PointSegment(new Vec2(-5, 5), Wall, new Vec2(10, 0)));
    }

    [Fact]
    public void SlantedSegment()
    {
        // Start (0, 0) to End (10, 10): solid on the lower left, normal up and to the right.
        var slope = Seg(0, 0, 10, 10);
        double k = Math.Sqrt(0.5);
        AssertHit(0.5, new Vec2(2, 2), new Vec2(k, -k), Collision.PointSegment(new Vec2(8, 2), slope, new Vec2(-12, 0)));
    }

    [Fact]
    public void OneWayFromBelow()
    {
        // Moving up through a floor from its solid side: passes through (a one-way platform).
        Assert.Null(Collision.PointSegment(new Vec2(5, 5), Floor, new Vec2(0, -10)));
    }

    [Fact]
    public void BothSolidFromEitherSide()
    {
        var floor = Seg(0, 0, 10, 0, bothSolid: true);
        AssertHit(0.5, new Vec2(5, 0), new Vec2(0, -1), Collision.PointSegment(new Vec2(5, -5), floor, new Vec2(0, 10)));
        // From below the normal flips, so it still faces back at the mover.
        AssertHit(0.5, new Vec2(5, 0), new Vec2(0, 1), Collision.PointSegment(new Vec2(5, 5), floor, new Vec2(0, -10)));
    }

    [Fact]
    public void Misses()
    {
        Assert.Null(Collision.PointSegment(new Vec2(15, -5), Floor, new Vec2(0, 10)));   // past the end
        Assert.Null(Collision.PointSegment(new Vec2(5, -5), Floor, new Vec2(0, 2)));     // stops short
        Assert.Null(Collision.PointSegment(new Vec2(5, -5), Floor, new Vec2(0, -10)));   // moving away
        Assert.Null(Collision.PointSegment(new Vec2(5, 5), Floor, new Vec2(0, 5)));      // already inside, moving deeper
    }

    [Fact]
    public void SlidingAlongIsNotAHit()
    {
        Assert.Null(Collision.PointSegment(new Vec2(-5, 0), Floor, new Vec2(20, 0)));    // on the line, moving along it
        Assert.Null(Collision.PointSegment(new Vec2(-5, -1), Floor, new Vec2(20, 0)));   // just above it, parallel
    }

    [Fact]
    public void TouchingCounts()
    {
        AssertHit(0, new Vec2(5, 0), new Vec2(0, -1), Collision.PointSegment(new Vec2(5, 0), Floor, new Vec2(0, 5)));     // starts on it, moving in
        Assert.Null(Collision.PointSegment(new Vec2(5, 0), Floor, new Vec2(0, -5)));                                       // starts on it, moving away
        AssertHit(1, new Vec2(5, 0), new Vec2(0, -1), Collision.PointSegment(new Vec2(5, -5), Floor, new Vec2(0, 5)));    // ends exactly on it
        AssertHit(0.5, new Vec2(10, 0), new Vec2(0, -1), Collision.PointSegment(new Vec2(10, -5), Floor, new Vec2(0, 10))); // exactly at its end point
    }

    [Fact]
    public void NothingToHit()
    {
        Assert.Null(Collision.PointSegment(new Vec2(5, -5), Floor, Vec2.Zero));                         // not moving
        Assert.Null(Collision.PointSegment(new Vec2(5, -5), Seg(5, 0, 5, 0), new Vec2(0, 10)));          // zero-length segment
    }
}
