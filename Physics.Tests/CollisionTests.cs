// AI-GEN AI-013: tests for Christopher's moving checks (Collision.PointSegment, PointCircle, PointArc, PointCurve), written by Claude. Dev-only, never part of the game.
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

    // ---- PointCircle (filled: blocks entering; anything inside may leave) ----

    static readonly Circle Ball = new(new Vec2(0, 0), 1);

    [Fact]
    public void CircleFromOutside()
    {
        AssertHit(0.4, new Vec2(-1, 0), new Vec2(-1, 0), Collision.PointCircle(new Vec2(-5, 0), Ball, new Vec2(10, 0)));
        AssertHit(0.5, new Vec2(0, -1), new Vec2(0, -1), Collision.PointCircle(new Vec2(0, -2), Ball, new Vec2(0, 2)));
    }

    [Fact]
    public void CircleInside()
    {
        Assert.Null(Collision.PointCircle(new Vec2(0.5, 0), Ball, new Vec2(1, 0)));                                     // moving out: free to leave
        AssertHit(0, new Vec2(0.5, 0), new Vec2(1, 0), Collision.PointCircle(new Vec2(0.5, 0), Ball, new Vec2(-1, 0)));  // moving deeper: stopped, pushed out
        Assert.Null(Collision.PointCircle(new Vec2(0, 0), Ball, new Vec2(0, 3)));                                       // exactly at the center: leaves along v
    }

    [Fact]
    public void CircleMissesAndGrazes()
    {
        Assert.Null(Collision.PointCircle(new Vec2(-2, -1), Ball, new Vec2(4, 0)));   // tangent graze
        Assert.Null(Collision.PointCircle(new Vec2(-5, 3), Ball, new Vec2(10, 0)));   // passes below
        Assert.Null(Collision.PointCircle(new Vec2(-5, 0), Ball, new Vec2(2, 0)));    // stops short
        Assert.Null(Collision.PointCircle(new Vec2(-5, 0), Ball, Vec2.Zero));         // not moving
        AssertHit(1, new Vec2(-1, 0), new Vec2(-1, 0), Collision.PointCircle(new Vec2(-3, 0), Ball, new Vec2(2, 0)));  // ends exactly on it
    }

    // ---- PointArc ----

    // Top half of the unit circle (Y down, so Direction (0, -1) is up). Positive HalfAngle: a bump, solid inside.
    static readonly Arc Bump = new(new Vec2(0, 0), 1, new Vec2(0, -1), Math.PI / 2);

    [Fact]
    public void LandingOnABump()
    {
        AssertHit(0.5, new Vec2(0, -1), new Vec2(0, -1), Collision.PointArc(new Vec2(0, -2), Bump, new Vec2(0, 2)));
        var moved = Bump with { Center = new Vec2(10, 5) };
        AssertHit(0.5, new Vec2(10, 4), new Vec2(0, -1), Collision.PointArc(new Vec2(10, 3), moved, new Vec2(0, 2)));
    }

    [Fact]
    public void BumpOneWay()
    {
        Assert.Null(Collision.PointArc(new Vec2(0, -0.5), Bump, new Vec2(0, -1)));   // from inside, going out
        // From below: crosses the bottom (not part of the arc), then the top from inside (open that way).
        Assert.Null(Collision.PointArc(new Vec2(0, 3), Bump, new Vec2(0, -10)));
    }

    [Fact]
    public void BothSolidArcFromInside()
    {
        var bump = Bump with { BothSolid = true };
        AssertHit(0.5, new Vec2(0, -1), new Vec2(0, 1), Collision.PointArc(new Vec2(0, -0.5), bump, new Vec2(0, -1)));
    }

    [Fact]
    public void ArcFirstCrossingDoesNotCountSecondDoes()
    {
        // Negative HalfAngle (solid outside): most of a circle, gap at the top. Entering from the left
        // crosses the arc from the open side; leaving on the right is blocked.
        var cage = new Arc(new Vec2(0, 0), 1, new Vec2(0, 1), -0.9 * Math.PI);
        AssertHit(4.0 / 6, new Vec2(1, 0), new Vec2(-1, 0), Collision.PointArc(new Vec2(-3, 0), cage, new Vec2(6, 0)));
    }

    [Fact]
    public void FullCircleArcs()
    {
        // HalfAngle = +-pi covers the whole circle: an edge-only ring with a solid side.
        var ring = new Arc(new Vec2(0, 0), 1, new Vec2(0, -1), Math.PI);
        AssertHit(1.0 / 3, new Vec2(-1, 0), new Vec2(-1, 0), Collision.PointArc(new Vec2(-3, 0), ring, new Vec2(6, 0)));
        var trap = new Arc(new Vec2(0, 0), 1, new Vec2(0, -1), -Math.PI);
        Assert.Null(Collision.PointArc(new Vec2(-3, 0), trap, new Vec2(2.5, 0)));                                       // gets in
        AssertHit(1.0 / 3, new Vec2(1, 0), new Vec2(-1, 0), Collision.PointArc(new Vec2(0, 0), trap, new Vec2(3, 0)));  // can't get out
    }

    [Fact]
    public void ArcMissesAndTouching()
    {
        Assert.Null(Collision.PointArc(new Vec2(-2, -1), Bump, new Vec2(4, 0)));   // tangent graze
        Assert.Null(Collision.PointArc(new Vec2(0, -3), Bump, new Vec2(0, 1)));    // stops short
        Assert.Null(Collision.PointArc(new Vec2(0, -3), Bump, Vec2.Zero));         // not moving
        AssertHit(0, new Vec2(0, -1), new Vec2(0, -1), Collision.PointArc(new Vec2(0, -1), Bump, new Vec2(0, 1)));   // starts on it
        AssertHit(1, new Vec2(0, -1), new Vec2(0, -1), Collision.PointArc(new Vec2(0, -2), Bump, new Vec2(0, 1)));   // ends on it
    }

    // ---- PointCurve ----

    // A valley: (-1, 0) down to (0, 1) at its middle and back up to (1, 0). Solid below, normal up.
    static readonly Curve Valley = new(new Vec2(-1, 0), new Vec2(0, 2), new Vec2(1, 0));

    [Fact]
    public void LandingInAValley()
    {
        // Straight down is parallel to Bend: the a = 0 (linear) branch.
        AssertHit(2.0 / 3, new Vec2(0, 1), new Vec2(0, -1), Collision.PointCurve(new Vec2(0, -1), Valley, new Vec2(0, 3)));
    }

    [Fact]
    public void ValleyOneWayAndBothSolid()
    {
        Assert.Null(Collision.PointCurve(new Vec2(0, 3), Valley, new Vec2(0, -3)));
        var both = Valley with { BothSolid = true };
        AssertHit(2.0 / 3, new Vec2(0, 1), new Vec2(0, 1), Collision.PointCurve(new Vec2(0, 3), both, new Vec2(0, -3)));
    }

    [Fact]
    public void CurveFirstCrossingDoesNotCountSecondDoes()
    {
        // Sideways at y = 0.5 (the quadratic branch): out of the solid through the left side, into it through the right.
        double s = Math.Sqrt(0.5);
        AssertHit((2 + s) / 4, new Vec2(s, 0.5), new Vec2(-Math.Sqrt(2.0 / 3), -Math.Sqrt(1.0 / 3)),
            Collision.PointCurve(new Vec2(-2, 0.5), Valley, new Vec2(4, 0)));
        // Same line the other way: blocked at the left side instead.
        AssertHit((2 + s) / 4, new Vec2(-s, 0.5), new Vec2(Math.Sqrt(2.0 / 3), -Math.Sqrt(1.0 / 3)),
            Collision.PointCurve(new Vec2(2, 0.5), Valley, new Vec2(-4, 0)));
    }

    [Fact]
    public void StraightCurveActsLikeASegment()
    {
        var flat = new Curve(new Vec2(0, 0), new Vec2(5, 0), new Vec2(10, 0));
        AssertHit(0.5, new Vec2(5, 0), new Vec2(0, -1), Collision.PointCurve(new Vec2(5, -5), flat, new Vec2(0, 10)));
        AssertHit(0.5, new Vec2(4, 0), new Vec2(0, -1), Collision.PointCurve(new Vec2(0, -4), flat, new Vec2(8, 8)));
    }

    [Fact]
    public void CurveMissesAndTouching()
    {
        Assert.Null(Collision.PointCurve(new Vec2(3, -1), Valley, new Vec2(0, 5)));     // past its end
        Assert.Null(Collision.PointCurve(new Vec2(0, -1), Valley, new Vec2(0, 1)));     // stops short
        Assert.Null(Collision.PointCurve(new Vec2(0, -1), Valley, Vec2.Zero));          // not moving
        AssertHit(0, new Vec2(0, 1), new Vec2(0, -1), Collision.PointCurve(new Vec2(0, 1), Valley, new Vec2(0, 1)));    // starts on it
        AssertHit(1, new Vec2(0, 1), new Vec2(0, -1), Collision.PointCurve(new Vec2(0, -1), Valley, new Vec2(0, 2)));   // ends on it
    }
}
