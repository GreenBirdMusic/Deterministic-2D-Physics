// AI-GEN AI-013: tests for Christopher's moving circle checks (Collision.CircleCircle, CircleSegment, CircleArc, CircleLoop), written by Claude. Dev-only, never part of the game.
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

    // The circle checks return every contact at the earliest T. Most cases have exactly one.
    static Hit? One(Hit[]? hits)
    {
        if (hits == null) return null;
        Assert.True(hits.Length == 1, $"expected one contact, got {hits.Length}");
        return hits[0];
    }

    static Hit? CC(Circle a, Circle b, Vec2 v) => One(Collision.CircleCircle(a, b, v));
    static Hit? CS(Circle c, Segment s, Vec2 v) => One(Collision.CircleSegment(c, s, v));
    static Hit? ArcHit(Circle c, Arc a, Vec2 v) => One(Collision.CircleArc(c, a, v));
    static readonly Circle Target = C(10, 0, 3);   // static circle, radius 3

    // ---- CircleCircle ----

    [Fact]
    public void HeadOn()
    {
        // Radius 2 from (0, 0) moving right 10: touches when the centers are 5 apart, center at (5, 0).
        // Contact point: on the line between centers, 2 past the mover's center.
        AssertHit(0.5, new Vec2(7, 0), new Vec2(-1, 0), CC(C(0, 0, 2), Target, new Vec2(10, 0)));
    }

    [Fact]
    public void HeadOn_ContactIsOnBothCircles()
    {
        Hit h = CC(C(0, 0, 2), Target, new Vec2(10, 0))!.Value;
        Assert.Equal(3, (h.Point - Target.Center).Length, 1e-9);            // on the static circle
        Assert.Equal(2, (h.Point - new Vec2(5, 0)).Length, 1e-9);           // on the mover at the hit
    }

    [Fact]
    public void Glancing()
    {
        // Moving right along y = -4: touches when the centers are 5 apart, so x offset 3, center at (7, -4).
        // Normal from the static center to the mover's center: (-3, -4) / 5.
        var n = new Vec2(-0.6, -0.8);
        AssertHit(0.7, Target.Center + 3 * n, n, CC(C(0, -4, 2), Target, new Vec2(10, 0)));
    }

    [Fact]
    public void JustMisses()
    {
        Assert.Null(CC(C(0, -5.000001, 2), Target, new Vec2(20, 0)));
    }

    [Fact]
    public void JustTouches_Grazing_NoHit()
    {
        // Exactly tangent pass (centers' closest distance = 5): a graze, not a hit (same as PointCircle).
        Assert.Null(CC(C(0, -5, 2), Target, new Vec2(20, 0)));
    }

    [Fact]
    public void StopsShort_NoHit() =>
        Assert.Null(CC(C(0, 0, 2), Target, new Vec2(4, 0)));

    [Fact]
    public void EndsExactlyTouching_Hits() =>
        AssertHit(1, new Vec2(7, 0), new Vec2(-1, 0), CC(C(0, 0, 2), Target, new Vec2(5, 0)));

    [Fact]
    public void StartsTouching_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(7, 0), new Vec2(-1, 0), CC(C(5, 0, 2), Target, new Vec2(1, 0)));

    [Fact]
    public void StartsOverlapping_MovingOut_NoHit() =>
        Assert.Null(CC(C(6, 0, 2), Target, new Vec2(-5, 0)));

    [Fact]
    public void StartsOverlapping_MovingIn_HitAtZero() =>
        // Filled circle: nothing goes deeper. Contact point: 2 past the mover's center, toward the target.
        AssertHit(0, new Vec2(8, 0), new Vec2(-1, 0), CC(C(6, 0, 2), Target, new Vec2(1, 0)));

    [Fact]
    public void ZeroRadius_SameAsPointCircle()
    {
        var p = new Vec2(0, -1);
        var v = new Vec2(12, 1);
        Hit? a = CC(new Circle(p, 0), Target, v);
        Hit? b = Collision.PointCircle(p, Target, v);
        Assert.True(a.HasValue && b.HasValue);
        AssertHit(b.Value.T, b.Value.Point, b.Value.Normal, a);
    }

    [Fact]
    public void NoMove_NoHit() =>
        Assert.Null(CC(C(0, 0, 2), Target, Vec2.Zero));
    // ---- CircleSegment ----
    // Floor from (0, 0) to (10, 0): solid below, normal up (0, -1). Mover radius 1 unless noted.

    static readonly Segment Floor = new(new Vec2(0, 0), new Vec2(10, 0));
    static readonly Segment FloorBoth = new(new Vec2(0, 0), new Vec2(10, 0), true);
    static readonly Vec2 Up = new(0, -1);
    static readonly Vec2 Down = new(0, 1);

    [Fact]
    public void Seg_FallsOntoMiddle() =>
        AssertHit(0.4, new Vec2(5, 0), Up, CS(C(5, -5, 1), Floor, new Vec2(0, 10)));

    [Fact]
    public void Seg_Diagonal() =>
        AssertHit(0.5, new Vec2(4, 0), Up, CS(C(0, -5, 1), Floor, new Vec2(8, 8)));

    [Fact]
    public void Seg_HitAtEndOfMove() =>
        AssertHit(1, new Vec2(5, 0), Up, CS(C(5, -3, 1), Floor, new Vec2(0, 2)));

    [Fact]
    public void Seg_StopsShort_NoHit() =>
        Assert.Null(CS(C(5, -3, 1), Floor, new Vec2(0, 1.9)));

    [Fact]
    public void Seg_FromBelow_OneSided_PassesThrough() =>
        Assert.Null(CS(C(5, 5, 1), Floor, new Vec2(0, -10)));

    [Fact]
    public void Seg_FromBelow_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(5, 0), Down, CS(C(5, 5, 1), FloorBoth, new Vec2(0, -10)));

    [Fact]
    public void Seg_FromAbove_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(5, 0), Up, CS(C(5, -5, 1), FloorBoth, new Vec2(0, 10)));

    [Fact]
    public void Seg_EndCap_FromOpenSide()
    {
        // Moving right along y = -0.5 into the left end (0, 0): touches the cap at x = -sqrt(0.75).
        double x = -Math.Sqrt(0.75);
        AssertHit((x + 5) / 10, new Vec2(0, 0), new Vec2(x, -0.5), CS(C(-5, -0.5, 1), Floor, new Vec2(10, 0)));
    }

    [Fact]
    public void Seg_EndCap_FromBackSide_OneSided_NoHit() =>
        Assert.Null(CS(C(-5, 0.5, 1), Floor, new Vec2(10, 0)));

    [Fact]
    public void Seg_EndCap_FromBackSide_BothSolid_Hits()
    {
        double x = -Math.Sqrt(0.75);
        AssertHit((x + 5) / 10, new Vec2(0, 0), new Vec2(x, 0.5), CS(C(-5, 0.5, 1), FloorBoth, new Vec2(10, 0)));
    }

    [Fact]
    public void Seg_EndCap_HeadOn() =>
        AssertHit(0.4, new Vec2(10, 0), new Vec2(1, 0), CS(C(15, 0, 1), Floor, new Vec2(-10, 0)));

    [Fact]
    public void Seg_WhereOffsetMeetsCap()
    {
        // Falling exactly onto the end: the offset segment and the cap touch at the same T, same normal.
        AssertHit(0.4, new Vec2(0, 0), Up, CS(C(0, -5, 1), Floor, new Vec2(0, 10)));
        AssertHit(0.4, new Vec2(10, 0), Up, CS(C(10, -5, 1), Floor, new Vec2(0, 10)));
    }

    [Fact]
    public void Seg_JustMissesEnd() =>
        Assert.Null(CS(C(11.000001, -5, 1), Floor, new Vec2(0, 10)));

    [Fact]
    public void Seg_Touching_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(5, 0), Up, CS(C(5, -1, 1), Floor, new Vec2(0, 1)));

    [Fact]
    public void Seg_Touching_SlidingAlong_NoHit() =>
        Assert.Null(CS(C(5, -1, 1), Floor, new Vec2(3, 0)));

    [Fact]
    public void Seg_Touching_MovingAway_NoHit() =>
        Assert.Null(CS(C(5, -1, 1), Floor, new Vec2(0, -1)));

    [Fact]
    public void Seg_RoundingOverlap_MovingIn_HitAtZero()
    {
        // Sunk in by less than circleTolerance: still caught.
        AssertHit(0, new Vec2(5, 0), Up, CS(C(5, -1 + 1e-7, 1), Floor, new Vec2(0, 1)), 1e-6);
        // Same at the end (the cap region).
        var h = CS(C(-0.6, -0.8 + 1e-7, 1), Floor, new Vec2(0.6, 0.8));
        AssertHit(0, new Vec2(0, 0), new Vec2(-0.6, -0.8), h, 1e-6);
    }

    [Fact]
    public void Seg_RoundingOverlap_MovingAway_NoHit() =>
        Assert.Null(CS(C(5, -1 + 1e-7, 1), Floor, new Vec2(0, -1)));

    [Fact]
    public void Seg_80PercentThrough_FallsBackOut()
    {
        // Jumped up through the one-way floor until the center is 0.6 above it, now falling: no hit.
        Assert.Null(CS(C(5, -0.6, 1), Floor, new Vec2(0, 2)));
        // Same near the end, inside the cap circle (the case the deep-overlap check is there for).
        Assert.Null(CS(C(0.2, -0.6, 1), Floor, new Vec2(0, 2)));
    }

    [Fact]
    public void Seg_JumpingUpThrough_OneSided_NoHit() =>
        Assert.Null(CS(C(5, 0.5, 1), Floor, new Vec2(0, -3)));

    [Fact]
    public void Seg_TouchingFromBehind_OneSided_NoHit()
    {
        Assert.Null(CS(C(5, 1, 1), Floor, new Vec2(0, -3)));
        Assert.Null(CS(C(5, 1 - 1e-7, 1), Floor, new Vec2(0, -3)));
    }

    [Fact]
    public void Seg_ZeroRadius_SameAsPointSegment()
    {
        var p = new Vec2(3, -2);
        var v = new Vec2(1, 5);
        Hit? a = CS(new Circle(p, 0), Floor, v);
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
            Hit? h = CS(new Circle(p, r), seg, v);
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
    // ---- CircleArc ----
    // Bump: top half of the circle at (0, 0), radius 5 (Direction up, positive HalfAngle: solid inside).
    // Dent (bowl): bottom half (Direction down, negative HalfAngle: open inside). Mover radius 1.

    static Arc Bump(bool both = false) => new(Vec2.Zero, 5, Up, Math.PI / 2, both);
    static Arc Dent(bool both = false) => new(Vec2.Zero, 5, Down, -Math.PI / 2, both);

    [Fact]
    public void Arc_Bump_FromAbove() =>
        AssertHit(0.4, new Vec2(0, -5), Up, ArcHit(C(0, -10, 1), Bump(), new Vec2(0, 10)));

    [Fact]
    public void Arc_Bump_Slanted()
    {
        // Moving straight down at x = 3: center touches the grown circle (radius 6) at y = -sqrt(27).
        double y = -Math.Sqrt(27);
        var n = new Vec2(3, y) / 6;
        AssertHit((10 + y) / 10, 5 * n, n, ArcHit(C(3, -10, 1), Bump(), new Vec2(0, 10)));
    }

    [Fact]
    public void Arc_Dent_FromInside() =>
        AssertHit(0.4, new Vec2(0, 5), Up, ArcHit(C(0, 0, 1), Dent(), new Vec2(0, 10)));

    [Fact]
    public void Arc_Bump_FromInside_OneSided_NoHit() =>
        Assert.Null(ArcHit(C(0, 0, 1), Bump(), new Vec2(0, -10)));

    [Fact]
    public void Arc_Dent_FromOutside_OneSided_NoHit() =>
        Assert.Null(ArcHit(C(0, 10, 1), Dent(), new Vec2(0, -10)));

    [Fact]
    public void Arc_Bump_FromInside_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(0, -5), Down, ArcHit(C(0, 0, 1), Bump(true), new Vec2(0, -10)));

    [Fact]
    public void Arc_Dent_FromOutside_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(0, 5), Down, ArcHit(C(0, 10, 1), Dent(true), new Vec2(0, -10)));

    [Fact]
    public void Arc_Bump_FromAbove_BothSolid_Hits() =>
        AssertHit(0.4, new Vec2(0, -5), Up, ArcHit(C(0, -10, 1), Bump(true), new Vec2(0, 10)));

    [Fact]
    public void Arc_Bump_NearEnd_GrownArcBeforeCap()
    {
        // Moving left along y = -0.5, just above the bump's right end (5, 0): the grown arc (radius 6)
        // is reached before the cap. Earliest wins.
        var h = ArcHit(C(10, -0.5, 1), Bump(), new Vec2(-10, 0));
        double cx = Math.Sqrt(36 - 0.25);
        var n = new Vec2(cx, -0.5) / 6;
        AssertHit((10 - cx) / 10, 5 * n, n, h);
    }

    [Fact]
    public void Arc_Bump_EndCap_FromBelow_Hits() =>
        // Moving up at x = 5.5, below the bump's right end (5, 0). Only the cap can touch; its normal
        // (0.5, 0.87) agrees with the arc's normal at that end (1, 0), so it counts.
        AssertHit((10 - Math.Sqrt(0.75)) / 10, new Vec2(5, 0), new Vec2(0.5, Math.Sqrt(0.75)),
            ArcHit(C(5.5, 10, 1), Bump(), new Vec2(0, -10)));

    [Fact]
    public void Arc_Dent_EndCap_FromOutside_OneSided_NoHit() =>
        // The bowl's right end is (5, 0), its normal there is (-1, 0) (into the bowl). Coming up from below
        // at x = 5.5, outside the bowl: the cap normal (0.5, 0.87) points away from the open side, so no hit.
        Assert.Null(ArcHit(C(5.5, 10, 1), Dent(), new Vec2(0, -10)));

    [Fact]
    public void Arc_Touching_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(0, -5), Up, ArcHit(C(0, -6, 1), Bump(), new Vec2(0, 1)));

    [Fact]
    public void Arc_RoundingOverlap_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(0, -5), Up, ArcHit(C(0, -6 + 1e-7, 1), Bump(), new Vec2(0, 1)), 1e-6);

    [Fact]
    public void Arc_80PercentThrough_FallsBackOut() =>
        Assert.Null(ArcHit(C(0, -5.4, 1), Bump(), new Vec2(0, 2)));

    [Fact]
    public void Arc_DentSmallerThanCircle_TouchesBothEnds()
    {
        // Bowl of radius 1, circle of radius 2 falling into it: it lands on both ends (+-1, 0) at once,
        // with its center at (0, -sqrt(3)). Both contacts come back, each with its own normal.
        var bowl = new Arc(Vec2.Zero, 1, Down, -Math.PI / 2);
        Hit[]? hits = Collision.CircleArc(C(0, -5, 2), bowl, new Vec2(0, 10));
        Assert.NotNull(hits);
        Assert.Equal(2, hits.Length);
        double t = (5 - Math.Sqrt(3)) / 10;
        double s3 = Math.Sqrt(3) / 2;
        Hit left = hits.Single(h => h.Point.X < 0), right = hits.Single(h => h.Point.X > 0);
        AssertHit(t, new Vec2(-1, 0), new Vec2(0.5, -s3), left);   // normals lean toward the center
        AssertHit(t, new Vec2(1, 0), new Vec2(-0.5, -s3), right);
    }

    [Fact]
    public void Arc_WhereGrownArcMeetsCap_OneContact()
    {
        // Moving diagonally onto the bump's right end (5, 0): the grown arc's end and the cap are reached
        // at the same T, at the same contact point with the same normal. That's one contact, not two.
        AssertHit(0.5, new Vec2(5, 0), new Vec2(1, 0), ArcHit(C(8, -2, 1), Bump(), new Vec2(-4, 4)));
    }

    [Fact]
    public void Arc_ZeroRadius_SameAsPointArc()
    {
        var p = new Vec2(1, -9);
        var v = new Vec2(0.5, 8);
        Hit? a = ArcHit(new Circle(p, 0), Bump(), v);
        Hit? b = Collision.PointArc(p, Bump(), v);
        Assert.True(a.HasValue && b.HasValue);
        AssertHit(b.Value.T, b.Value.Point, b.Value.Normal, a);
    }

    [Fact]
    public void Arc_RandomBothSolid_TouchesAtTAndNotBefore()
    {
        // Same idea as the segment version: at T the circle just touches the arc, never overlapping before.
        var rng = new Random(11);
        int hits = 0;
        for (int i = 0; i < 5000; i++)
        {
            double ang = rng.NextDouble() * 2 * Math.PI;
            double ha = (rng.Next(2) == 0 ? 1 : -1) * (0.2 + rng.NextDouble() * 2.8);
            var arc = new Arc(new Vec2(rng.NextDouble() * 20, rng.NextDouble() * 20), 1 + rng.NextDouble() * 8,
                new Vec2(Math.Cos(ang), Math.Sin(ang)), ha, true);
            double r = 0.2 + rng.NextDouble() * 3;
            Vec2 p = new(rng.NextDouble() * 40 - 10, rng.NextDouble() * 40 - 10);
            if ((p - arc.ClosestPoint(p)).Length <= r + 1e-3) continue;
            Vec2 v = new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
            Hit[]? hs = Collision.CircleArc(new Circle(p, r), arc, v);
            double end = hs?[0].T ?? 1;
            for (int k = 0; k < 400; k++)
            {
                Vec2 at = p + (end * k / 400) * v;
                Assert.True((at - arc.ClosestPoint(at)).Length >= r - 1e-9, $"case {i}: overlaps before T");
            }
            if (hs != null)
            {
                hits++;
                foreach (Hit hit in hs)
                {
                    Vec2 center = p + hit.T * v;
                    Assert.Equal(end, hit.T, 1e-7);
                    Assert.Equal(r, (center - arc.ClosestPoint(center)).Length, 1e-7);
                    Assert.Equal(r, (center - hit.Point).Length, 1e-7);
                    Assert.True(Vec2.Dot(v, hit.Normal) < 0, $"case {i}: normal doesn't face the mover");
                }
            }
        }
        Assert.True(hits > 300, $"only {hits} hits");
    }
    // ---- CircleLoop ----
    // Loops are drawn clockwise on screen (solid inside). Mover radius 1 unless noted.

    static Segment S(double ax, double ay, double bx, double by) => new(new Vec2(ax, ay), new Vec2(bx, by));

    static IEdge[] Box(double x0, double y0, double x1, double y1) =>
        [S(x0, y0, x1, y0), S(x1, y0, x1, y1), S(x1, y1, x0, y1), S(x0, y1, x0, y0)];

    static IEdge[] Polygon(params double[] xy)
    {
        var edges = new IEdge[xy.Length / 2];
        for (int i = 0; i < edges.Length; i++)
        {
            int j = (i + 1) % edges.Length;
            edges[i] = S(xy[2 * i], xy[2 * i + 1], xy[2 * j], xy[2 * j + 1]);
        }
        return edges;
    }

    static readonly IEdge[] Box10 = Box(0, 0, 10, 10);
    static readonly double R2 = Math.Sqrt(0.5);

    [Fact]
    public void Loop_Empty_NoHit() =>
        Assert.Null(Collision.CircleLoop(C(0, 0, 1), [], new Vec2(5, 5)));

    [Fact]
    public void Loop_FallsOnTop_OneContact() =>
        AssertHit(0.4, new Vec2(5, 0), Up, One(Collision.CircleLoop(C(5, -5, 1), Box10, new Vec2(0, 10))));

    [Fact]
    public void Loop_ConvexCorner_OneContact()
    {
        // Diagonally into the top-left corner: both edges report the shared corner cap, kept once.
        double t = (5 * Math.Sqrt(2) - 1) / (10 * Math.Sqrt(2));
        AssertHit(t, Vec2.Zero, new Vec2(-R2, -R2), One(Collision.CircleLoop(C(-5, -5, 1), Box10, new Vec2(10, 10))));
    }

    [Fact]
    public void Loop_VNotch_TwoContacts()
    {
        // A V cut into the top of a box, bottom of the V at (5, 5). A circle falling into it touches
        // both slopes at once, with its center sqrt(2) above the bottom of the V.
        IEdge[] v = Polygon(0, 0, 5, 5, 10, 0, 10, 10, 0, 10);
        Hit[]? hits = Collision.CircleLoop(C(5, -5, 1), v, new Vec2(0, 20));
        Assert.NotNull(hits);
        Assert.Equal(2, hits.Length);
        double t = (10 - Math.Sqrt(2)) / 20;
        Hit left = hits.Single(h => h.Point.X < 5), right = hits.Single(h => h.Point.X > 5);
        AssertHit(t, new Vec2(5 - R2, 5 - Math.Sqrt(2) + R2), new Vec2(R2, -R2), left);
        AssertHit(t, new Vec2(5 + R2, 5 - Math.Sqrt(2) + R2), new Vec2(-R2, -R2), right);
    }

    [Fact]
    public void Loop_SplitFloor_OneContact()
    {
        // A floor made of two pieces meeting at (5, 0): landing exactly on the join is one contact.
        IEdge[] floor = [S(0, 0, 5, 0), S(5, 0, 10, 0)];
        AssertHit(0.4, new Vec2(5, 0), Up, One(Collision.CircleLoop(C(5, -5, 1), floor, new Vec2(0, 10))));
    }

    [Fact]
    public void Loop_StartsInside_CanLeave()
    {
        Assert.Null(Collision.CircleLoop(C(5, 5, 1), Box10, new Vec2(20, 0)));
        Assert.Null(Collision.CircleLoop(C(5, 5, 1), Box10, new Vec2(10, 10)));
    }

    [Fact]
    public void Loop_UShape_EarliestHitWins_WhateverTheOrder()
    {
        // A cup: a box with a notch from (3, 0) to (7, 7) open at the top. Moving right at y = 3 hits the
        // left outer wall first (T = 0.075), and would hit the right arm's inner wall later (T = 0.425).
        double[] cup = [0, 0, 3, 0, 3, 7, 7, 7, 7, 0, 10, 0, 10, 10, 0, 10];
        IEdge[] edges = Polygon(cup);
        for (int start = 0; start < edges.Length; start++)
        {
            IEdge[] rotated = edges.Skip(start).Concat(edges.Take(start)).ToArray();  // same loop, other first edge
            var h = One(Collision.CircleLoop(C(-2, 3, 0.5), rotated, new Vec2(20, 0)));
            AssertHit(0.075, new Vec2(0, 3), new Vec2(-1, 0), h);
        }
    }

    [Fact]
    public void Loop_ArcTop()
    {
        // A box whose top is a bump (center (5, 0), radius 5).
        var bump = new Arc(new Vec2(5, 0), 5, Up, Math.PI / 2);
        IEdge[] loop = [bump, Box10[1], Box10[2], Box10[3]];
        AssertHit(0.4, new Vec2(5, -5), Up, One(Collision.CircleLoop(C(5, -10, 1), loop, new Vec2(0, 10))));
    }

    [Fact]
    public void Loop_WithCurve_NotImplementedYet()
    {
        IEdge[] loop = [new Curve(new Vec2(0, 0), new Vec2(5, -3), new Vec2(10, 0)), Box10[1], Box10[2], Box10[3]];
        Assert.Throws<NotImplementedException>(() => Collision.CircleLoop(C(5, -10, 1), loop, new Vec2(0, 10)));
    }

    [Fact]
    public void Loop_RandomBoxes_TouchesAtTAndNotBefore()
    {
        // Random circles moving at random boxes from outside. If there's a hit, every contact is at T and
        // just touching; no sampled moment before T overlaps. If there's no hit, it never overlaps.
        var rng = new Random(13);
        int hits = 0;
        for (int i = 0; i < 3000; i++)
        {
            double x0 = rng.NextDouble() * 20, y0 = rng.NextDouble() * 20;
            IEdge[] box = Box(x0, y0, x0 + 1 + rng.NextDouble() * 10, y0 + 1 + rng.NextDouble() * 10);
            double r = 0.2 + rng.NextDouble() * 3;
            Vec2 p = new(rng.NextDouble() * 40 - 10, rng.NextDouble() * 40 - 10);
            double Dist(Vec2 at) => box.Min(e => (at - ((Segment)e).ClosestPoint(at)).Length);
            if (Dist(p) <= r + 1e-3 || Overlap.PointInLoop(p, box)) continue;
            Vec2 v = new(rng.NextDouble() * 40 - 20, rng.NextDouble() * 40 - 20);
            Hit[]? hs = Collision.CircleLoop(new Circle(p, r), box, v);
            double end = hs?[0].T ?? 1;
            for (int k = 0; k < 400; k++)
                Assert.True(Dist(p + (end * k / 400) * v) >= r - 1e-9, $"case {i}: overlaps before T");
            if (hs == null) continue;
            hits++;
            foreach (Hit h in hs)
            {
                Vec2 center = p + h.T * v;
                Assert.Equal(end, h.T, 1e-7);
                Assert.Equal(r, (center - h.Point).Length, 1e-7);
                Assert.True(Dist(h.Point) < 1e-7, $"case {i}: contact not on the box");
                Assert.True(Vec2.Dot(v, h.Normal) < 0, $"case {i}: normal doesn't face the mover");
            }
        }
        Assert.True(hits > 150, $"only {hits} hits");
    }
}
