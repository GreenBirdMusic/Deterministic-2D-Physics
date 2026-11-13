// AI-GEN AI-013: tests for Christopher's moving circle checks (Collision.CircleCircle, CircleSegment), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Hit.Point for a circle is the contact point, not the center.
public class CircleCollisionTests
{
    static void AssertNear(Vec2 expected, Vec2 actual, double tol = 1e-9)
    {
        Assert.Equal(expected.X, actual.X, tol);
        Assert.Equal(expected.Y, actual.Y, tol);
    }

    static void AssertHit(double t, Vec2 point, Vec2 normal, Hit? actual, double tol = 1e-9)
    {
        Assert.True(actual.HasValue, "expected a hit");
        Hit h = actual.Value;
        Assert.Equal(t, h.T, tol);
        AssertNear(point, h.Point, tol);
        AssertNear(normal, h.Normal, tol);
    }

    static Circle C(double x, double y, double r) => new(new Vec2(x, y), r);
    static readonly Circle Target = C(10, 0, 3);   // static circle, radius 3

    // ---- CircleCircle ----

    [Fact]
    public void HeadOn()
    {
        // Radius 2 from (0, 0) moving right 10: touches when the centers are 5 apart, center at (5, 0).
        // Contact point: on the line between centers, 2 past the mover's center.
        AssertHit(0.5, new Vec2(7, 0), new Vec2(-1, 0), Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(10, 0)));
    }

    [Fact]
    public void HeadOn_ContactIsOnBothCircles()
    {
        Hit h = Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(10, 0))!.Value;
        Assert.Equal(3, (h.Point - Target.Center).Length, 1e-9);            // on the static circle
        Assert.Equal(2, (h.Point - new Vec2(5, 0)).Length, 1e-9);           // on the mover at the hit
    }

    [Fact]
    public void Glancing()
    {
        // Moving right along y = -4: touches when the centers are 5 apart, so x offset 3, center at (7, -4).
        // Normal from the static center to the mover's center: (-3, -4) / 5.
        var n = new Vec2(-0.6, -0.8);
        AssertHit(0.7, Target.Center + 3 * n, n, Collision.CircleCircle(C(0, -4, 2), Target, new Vec2(10, 0)));
    }

    [Fact]
    public void JustMisses()
    {
        Assert.Null(Collision.CircleCircle(C(0, -5.000001, 2), Target, new Vec2(20, 0)));
    }

    [Fact]
    public void JustTouches_Grazing_NoHit()
    {
        // Exactly tangent pass (centers' closest distance = 5): a graze, not a hit (same as PointCircle).
        Assert.Null(Collision.CircleCircle(C(0, -5, 2), Target, new Vec2(20, 0)));
    }

    [Fact]
    public void StopsShort_NoHit() =>
        Assert.Null(Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(4, 0)));

    [Fact]
    public void EndsExactlyTouching_Hits() =>
        AssertHit(1, new Vec2(7, 0), new Vec2(-1, 0), Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(5, 0)));

    [Fact]
    public void StartsTouching_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(7, 0), new Vec2(-1, 0), Collision.CircleCircle(C(5, 0, 2), Target, new Vec2(1, 0)));

    [Fact]
    public void StartsOverlapping_MovingOut_NoHit() =>
        Assert.Null(Collision.CircleCircle(C(6, 0, 2), Target, new Vec2(-5, 0)));

    [Fact]
    public void StartsOverlapping_MovingIn_HitAtZero() =>
        // Filled circle: nothing goes deeper. Contact point: 2 past the mover's center, toward the target.
        AssertHit(0, new Vec2(8, 0), new Vec2(-1, 0), Collision.CircleCircle(C(6, 0, 2), Target, new Vec2(1, 0)));

    [Fact]
    public void ZeroRadius_SameAsPointCircle()
    {
        var p = new Vec2(0, -1);
        var v = new Vec2(12, 1);
        Hit? a = Collision.CircleCircle(new Circle(p, 0), Target, v);
        Hit? b = Collision.PointCircle(p, Target, v);
        Assert.True(a.HasValue && b.HasValue);
        AssertHit(b.Value.T, b.Value.Point, b.Value.Normal, a);
    }

    [Fact]
    public void NoMove_NoHit() =>
        Assert.Null(Collision.CircleCircle(C(0, 0, 2), Target, Vec2.Zero));
    // ---- CircleSegment ----
    // Floor from (0, 0) to (10, 0): solid below, normal up (0, -1). Mover radius 1 unless noted.

    static readonly Segment Floor = new(new Vec2(0, 0), new Vec2(10, 0));
    static readonly Segment FloorBoth = new(new Vec2(0, 0), new Vec2(10, 0), true);
    static readonly Vec2 Up = new(0, -1);
    static readonly Vec2 Down = new(0, 1);

    [Fact]
    public void Seg_FallsOntoMiddle() =>
        AssertHit(0.4, new Vec2(5, 0), Up, Collision.CircleSegment(C(5, -5, 1), Floor, new Vec2(0, 10)));

    [Fact]
    public void Seg_Diagonal() =>
        AssertHit(0.5, new Vec2(4, 0), Up, Collision.CircleSegment(C(0, -5, 1), Floor, new Vec2(8, 8)));

    [Fact]
    public void Seg_HitAtEndOfMove() =>
        AssertHit(1, new Vec2(5, 0), Up, Collision.CircleSegment(C(5, -3, 1), Floor, new Vec2(0, 2)));

    [Fact]
    public void Seg_StopsShort_NoHit() =>
        Assert.Null(Collision.CircleSegment(C(5, -3, 1), Floor, new Vec2(0, 1.9)));

    [Fact]
    public void Seg_FromBelow_OneSided_PassesThrough() =>
        Assert.Null(Collision.CircleSegment(C(5, 5, 1), Floor, new Vec2(0, -10)));

    [Fact]
    public void Seg_FromBelow_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(5, 0), Down, Collision.CircleSegment(C(5, 5, 1), FloorBoth, new Vec2(0, -10)));

    [Fact]
    public void Seg_FromAbove_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(5, 0), Up, Collision.CircleSegment(C(5, -5, 1), FloorBoth, new Vec2(0, 10)));

    [Fact]
    public void Seg_EndCap_FromOpenSide()
    {
        // Moving right along y = -0.5 into the left end (0, 0): touches the cap at x = -sqrt(0.75).
        double x = -Math.Sqrt(0.75);
        AssertHit((x + 5) / 10, new Vec2(0, 0), new Vec2(x, -0.5), Collision.CircleSegment(C(-5, -0.5, 1), Floor, new Vec2(10, 0)));
    }

    [Fact]
    public void Seg_EndCap_FromBackSide_OneSided_NoHit() =>
        Assert.Null(Collision.CircleSegment(C(-5, 0.5, 1), Floor, new Vec2(10, 0)));

    [Fact]
    public void Seg_EndCap_FromBackSide_BothSolid_Hits()
    {
        double x = -Math.Sqrt(0.75);
        AssertHit((x + 5) / 10, new Vec2(0, 0), new Vec2(x, 0.5), Collision.CircleSegment(C(-5, 0.5, 1), FloorBoth, new Vec2(10, 0)));
    }

    [Fact]
    public void Seg_EndCap_HeadOn() =>
        AssertHit(0.4, new Vec2(10, 0), new Vec2(1, 0), Collision.CircleSegment(C(15, 0, 1), Floor, new Vec2(-10, 0)));

    [Fact]
    public void Seg_WhereOffsetMeetsCap()
    {
        // Falling exactly onto the end: the offset segment and the cap touch at the same T, same normal.
        AssertHit(0.4, new Vec2(0, 0), Up, Collision.CircleSegment(C(0, -5, 1), Floor, new Vec2(0, 10)));
        AssertHit(0.4, new Vec2(10, 0), Up, Collision.CircleSegment(C(10, -5, 1), Floor, new Vec2(0, 10)));
    }

    [Fact]
    public void Seg_JustMissesEnd() =>
        Assert.Null(Collision.CircleSegment(C(11.000001, -5, 1), Floor, new Vec2(0, 10)));

    [Fact]
    public void Seg_Touching_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(5, 0), Up, Collision.CircleSegment(C(5, -1, 1), Floor, new Vec2(0, 1)));

    [Fact]
    public void Seg_Touching_SlidingAlong_NoHit() =>
        Assert.Null(Collision.CircleSegment(C(5, -1, 1), Floor, new Vec2(3, 0)));

    [Fact]
    public void Seg_Touching_MovingAway_NoHit() =>
        Assert.Null(Collision.CircleSegment(C(5, -1, 1), Floor, new Vec2(0, -1)));

    [Fact]
    public void Seg_RoundingOverlap_MovingIn_HitAtZero()
    {
        // Sunk in by less than circleTolerance: still caught.
        AssertHit(0, new Vec2(5, 0), Up, Collision.CircleSegment(C(5, -1 + 1e-7, 1), Floor, new Vec2(0, 1)), 1e-6);
        // Same at the end (the cap region).
        var h = Collision.CircleSegment(C(-0.6, -0.8 + 1e-7, 1), Floor, new Vec2(0.6, 0.8));
        AssertHit(0, new Vec2(0, 0), new Vec2(-0.6, -0.8), h, 1e-6);
    }

    [Fact]
    public void Seg_RoundingOverlap_MovingAway_NoHit() =>
        Assert.Null(Collision.CircleSegment(C(5, -1 + 1e-7, 1), Floor, new Vec2(0, -1)));

    [Fact]
    public void Seg_80PercentThrough_FallsBackOut()
    {
        // Jumped up through the one-way floor until the center is 0.6 above it, now falling: no hit.
        Assert.Null(Collision.CircleSegment(C(5, -0.6, 1), Floor, new Vec2(0, 2)));
        // Same near the end, inside the cap circle (the case the deep-overlap check is there for).
        Assert.Null(Collision.CircleSegment(C(0.2, -0.6, 1), Floor, new Vec2(0, 2)));
    }

    [Fact]
    public void Seg_JumpingUpThrough_OneSided_NoHit() =>
        Assert.Null(Collision.CircleSegment(C(5, 0.5, 1), Floor, new Vec2(0, -3)));

    [Fact]
    public void Seg_TouchingFromBehind_OneSided_NoHit()
    {
        Assert.Null(Collision.CircleSegment(C(5, 1, 1), Floor, new Vec2(0, -3)));
        Assert.Null(Collision.CircleSegment(C(5, 1 - 1e-7, 1), Floor, new Vec2(0, -3)));
    }

    [Fact]
    public void Seg_ZeroRadius_SameAsPointSegment()
    {
        var p = new Vec2(3, -2);
        var v = new Vec2(1, 5);
        Hit? a = Collision.CircleSegment(new Circle(p, 0), Floor, v);
        Hit? b = Collision.PointSegment(p, Floor, v);
        Assert.True(a.HasValue && b.HasValue);
        AssertHit(b.Value.T, b.Value.Point, b.Value.Normal, a);
    }

    [Fact]
    public void Seg_RandomBothSolid_TouchesAtTAndNotBefore()
    {
        // Random circles moving past random BothSolid segments, starting clear of them. If there's a hit,
        // the circle at T must just touch the segment, with no sampled moment before T where it overlaps.
        // If there's no hit, it never overlaps along the way.
        var rng = new Random(7);
        int hits = 0;
        for (int i = 0; i < 5000; i++)
        {
            Vec2 a = new(rng.NextDouble() * 20, rng.NextDouble() * 20), b = new(rng.NextDouble() * 20, rng.NextDouble() * 20);
            if ((b - a).Length < 0.5) continue;
            var seg = new Segment(a, b, true);
            double r = 0.2 + rng.NextDouble() * 3;
            Vec2 p = new(rng.NextDouble() * 30 - 5, rng.NextDouble() * 30 - 5);
            if ((p - seg.ClosestPoint(p)).Length <= r + 1e-3) continue;
            Vec2 v = new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
            Hit? h = Collision.CircleSegment(new Circle(p, r), seg, v);
            double end = h?.T ?? 1;
            for (int k = 0; k < 400; k++)
            {
                Vec2 at = p + (end * k / 400) * v;
                Assert.True((at - seg.ClosestPoint(at)).Length >= r - 1e-9, $"case {i}: overlaps before T");
            }
            if (h is Hit hit)
            {
                hits++;
                Vec2 center = p + hit.T * v;
                Assert.Equal(r, (center - seg.ClosestPoint(center)).Length, 1e-7);
                Assert.Equal(0, (seg.ClosestPoint(center) - hit.Point).Length, 1e-7);
                Assert.True(Vec2.Dot(v, hit.Normal) < 0, $"case {i}: normal doesn't face the mover");
            }
        }
        Assert.True(hits > 500, $"only {hits} hits");
    }
}
