// AI-GEN AI-013: tests for Christopher's moving edge-vs-edge checks (Collision.SegmentSegment, SegmentArc), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Solid on the right of Start -> End; normals point to the open side.
// The first segment moves by v, the second stays still. Contact normals point from the still one toward the mover.
public class EdgeCollisionTests
{
    static Segment S(double ax, double ay, double bx, double by, bool both = false) => new(new Vec2(ax, ay), new Vec2(bx, by), both);

    static readonly Segment Floor = S(0, 0, 10, 0);            // solid below, normal up
    static readonly Segment FloorBoth = S(0, 0, 10, 0, true);
    static readonly Vec2 Up = new(0, -1), Down = new(0, 1);
    static readonly double R2 = Math.Sqrt(0.5);

    static void AssertNear(Vec2 expected, Vec2 actual, double tol = 1e-9)
    {
        Assert.Equal(expected.X, actual.X, tol);
        Assert.Equal(expected.Y, actual.Y, tol);
    }

    // Checks the contacts (any order): each expected (point, normal) appears once, all at time t.
    static void AssertContacts(double t, Hit[]? hits, params (Vec2 point, Vec2 normal)[] expected)
    {
        Assert.NotNull(hits);
        Assert.Equal(expected.Length, hits.Length);
        foreach (var (point, normal) in expected)
        {
            var match = hits.Where(h => (h.Point - point).Length < 1e-9).ToArray();
            Assert.True(match.Length == 1, $"expected one contact at ({point.X}, {point.Y}), got {match.Length}");
            Assert.Equal(t, match[0].T, 1e-9);
            AssertNear(normal, match[0].Normal);
        }
    }

    // A box's bottom edge is drawn right to left (normal down, solid above); its top edge left to right.
    [Fact]
    public void FlatLanding_TwoContacts() =>
        AssertContacts(0.5, Collision.SegmentSegment(S(6, -5, 4, -5), Floor, new Vec2(0, 10)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void CornerOnFlat_OneContact() =>
        AssertContacts(0.5, Collision.SegmentSegment(S(6, -5, 4, -6), Floor, new Vec2(0, 10)),
            (new Vec2(6, 0), Up));

    [Fact]
    public void FloorCornerHitsMovingEdge()
    {
        // Only the floor's end (10, 0) is reached: the mover's middle comes down onto it.
        // Contact point is the floor's end itself; normal points from the floor toward the mover.
        AssertContacts(0.5, Collision.SegmentSegment(S(12, -3, 8, -7), Floor, new Vec2(0, 10)),
            (new Vec2(10, 0), new Vec2(R2, -R2)));
    }

    [Fact]
    public void LongEdgeOverFloorEnd_BothKindsOfContact() =>
        // The mover's left end lands at (8, 0) and the floor's end (10, 0) meets the mover's middle, same T.
        AssertContacts(0.5, Collision.SegmentSegment(S(14, -5, 8, -5), Floor, new Vec2(0, 10)),
            (new Vec2(8, 0), Up), (new Vec2(10, 0), Up));

    [Fact]
    public void StopsShort_NoHit() =>
        Assert.Null(Collision.SegmentSegment(S(6, -5, 4, -5), Floor, new Vec2(0, 4)));

    [Fact]
    public void MissesSideways_NoHit() =>
        Assert.Null(Collision.SegmentSegment(S(16, -5, 14, -5), Floor, new Vec2(0, 10)));

    [Fact]
    public void BoxTopUpThroughOneWay_NoHit() =>
        Assert.Null(Collision.SegmentSegment(S(4, 5, 6, 5), Floor, new Vec2(0, -10)));

    [Fact]
    public void BoxTopUpUnderOneWayEnd_NoHit() =>
        // The platform's end (10, 0) would meet the box's top from inside the platform's solid side: filtered.
        Assert.Null(Collision.SegmentSegment(S(8, 5, 12, 5), Floor, new Vec2(0, -10)));

    [Fact]
    public void BoxTopUpThroughBothSolid_Hits() =>
        AssertContacts(0.5, Collision.SegmentSegment(S(4, 5, 6, 5), FloorBoth, new Vec2(0, -10)),
            (new Vec2(4, 0), Down), (new Vec2(6, 0), Down));

    [Fact]
    public void OneWayPlatformMovingDownOntoFloor_PassesThrough() =>
        // The mover is a one-way platform (solid below): it's only solid from above, so it passes through.
        Assert.Null(Collision.SegmentSegment(S(4, -5, 6, -5), Floor, new Vec2(0, 10)));

    [Fact]
    public void BothSolidPlatformMovingDownOntoFloor_Hits() =>
        AssertContacts(0.5, Collision.SegmentSegment(S(4, -5, 6, -5, true), Floor, new Vec2(0, 10)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void AlreadyCrossing_NoHit() =>
        // A vertical edge already sticking through the floor, moving down: free to leave, no snap.
        Assert.Null(Collision.SegmentSegment(S(5, -2, 5, 2), Floor, new Vec2(0, 10)));

    [Fact]
    public void AlreadyCrossing_BothSolid_NoHit() =>
        Assert.Null(Collision.SegmentSegment(S(5, -2, 5, 2), FloorBoth, new Vec2(0, 10)));

    [Fact]
    public void RestingFlat_MovingDown_HitAtZero() =>
        AssertContacts(0, Collision.SegmentSegment(S(6, 0, 4, 0), Floor, new Vec2(0, 1)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void RestingOnCorner_MovingDown_HitAtZero() =>
        AssertContacts(0, Collision.SegmentSegment(S(6, 0, 4, -2), Floor, new Vec2(0, 1)),
            (new Vec2(6, 0), Up));

    [Fact]
    public void RestingFlat_MovingUp_NoHit() =>
        Assert.Null(Collision.SegmentSegment(S(6, 0, 4, 0), Floor, new Vec2(0, -1)));

    [Fact]
    public void RestingFlat_Sliding_NoHit() =>
        Assert.Null(Collision.SegmentSegment(S(6, 0, 4, 0), Floor, new Vec2(3, 0)));

    [Fact]
    public void RoundingSunk_MovingDown_HitAtZero() =>
        // 1e-10 below the floor's surface (rounding), moving further down: caught, no sinking through.
        AssertContacts(0, Collision.SegmentSegment(S(6, 1e-10, 4, 1e-10), Floor, new Vec2(0, 1)),
            (new Vec2(4, 1e-10), Up), (new Vec2(6, 1e-10), Up));

    [Fact]
    public void RandomBothSolid_TouchesAtTAndNotBefore()
    {
        // Random BothSolid pairs, starting apart. If there's a hit, the segments touch at T and don't
        // overlap at any sampled moment before; if there's no hit, they never overlap along the way.
        var rng = new Random(17);
        int hits = 0;
        static double Dist(Segment a, Segment b) => new[]
        {
            (a.Start - b.ClosestPoint(a.Start)).Length, (a.End - b.ClosestPoint(a.End)).Length,
            (b.Start - a.ClosestPoint(b.Start)).Length, (b.End - a.ClosestPoint(b.End)).Length,
        }.Min();
        Vec2 Rand() => new(rng.NextDouble() * 20, rng.NextDouble() * 20);
        for (int i = 0; i < 5000; i++)
        {
            Segment a = new(Rand(), Rand(), true), b = new(Rand(), Rand(), true);
            if ((a.End - a.Start).Length < 0.5 || (b.End - b.Start).Length < 0.5) continue;
            if (Overlap.SegmentSegment(a, b) != null || Dist(a, b) < 1e-3) continue;
            Vec2 v = new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
            Hit[]? hs = Collision.SegmentSegment(a, b, v);
            double end = hs?[0].T ?? 1;
            for (int k = 0; k < 400; k++)
            {
                Vec2 off = (end * k / 400) * v;
                Assert.True(Overlap.SegmentSegment(new Segment(a.Start + off, a.End + off), b) == null, $"case {i}: overlaps before T");
            }
            if (hs == null) continue;
            hits++;
            Vec2 o = end * v;
            var moved = new Segment(a.Start + o, a.End + o);
            Assert.True(Dist(moved, b) < 1e-7, $"case {i}: not touching at T");
            foreach (Hit h in hs)
            {
                Assert.Equal(end, h.T, 1e-7);
                Assert.True((h.Point - moved.ClosestPoint(h.Point)).Length < 1e-7 && (h.Point - b.ClosestPoint(h.Point)).Length < 1e-7,
                    $"case {i}: contact not on both segments");
                Assert.True(Vec2.Dot(v, h.Normal) < 0, $"case {i}: normal doesn't face the mover");
            }
        }
        Assert.True(hits > 300, $"only {hits} hits");
    }
    // ---- SegmentArc (the segment moves, the arc stays still) ----
    // Bump: top half of the circle at (0, 0), radius 5, solid inside. Bowl: bottom half, open inside.

    static Arc Bump(bool both = false) => new(Vec2.Zero, 5, Up, Math.PI / 2, both);
    static Arc Bowl() => new(Vec2.Zero, 5, Down, -Math.PI / 2);
    static readonly double S21 = Math.Sqrt(21);

    [Fact]
    public void Arc_FlatLandsOnTopOfBump_ExtremePoint() =>
        AssertContacts(0.3, Collision.SegmentArc(S(2, -8, -2, -8), Bump(), new Vec2(0, 10)),
            (new Vec2(0, -5), Up));

    [Fact]
    public void Arc_SegmentEndLandsOnBump() =>
        // The extreme point (0, -5) is outside the segment's x range, so its left end (3, -8) lands first.
        AssertContacts(0.4, Collision.SegmentArc(S(6, -8, 3, -8), Bump(), new Vec2(0, 10)),
            (new Vec2(3, -4), new Vec2(0.6, -0.8)));

    [Fact]
    public void Arc_ArcEndHitsSegment() =>
        // A box's left edge moving left into the bump's right end (5, 0).
        AssertContacts(0.3, Collision.SegmentArc(S(8, 2, 8, -2), Bump(), new Vec2(-10, 0)),
            (new Vec2(5, 0), new Vec2(1, 0)));

    [Fact]
    public void Arc_IntoBowl_EndsTouchFirst() =>
        // A box's bottom edge dropping into the bowl: its ends touch the sides before the bottom.
        AssertContacts((S21 + 1) / 10, Collision.SegmentArc(S(2, -1, -2, -1), Bowl(), new Vec2(0, 10)),
            (new Vec2(2, S21), new Vec2(-2, -S21) / 5), (new Vec2(-2, S21), new Vec2(2, -S21) / 5));

    [Fact]
    public void Arc_UpThroughOneSidedBump_NoHit() =>
        // A box's top edge rising from below, through the inside of the bump: passes through. The lower
        // extreme point (0, 5) is not on the arc, so it must not be swept.
        Assert.Null(Collision.SegmentArc(S(-2, 8, 2, 8), Bump(), new Vec2(0, -20)));

    [Fact]
    public void Arc_UpIntoBothSolidBump_EndsHitFromInside() =>
        AssertContacts((8 + S21) / 20, Collision.SegmentArc(S(-2, 8, 2, 8), Bump(true), new Vec2(0, -20)),
            (new Vec2(-2, -S21), new Vec2(2, S21) / 5), (new Vec2(2, -S21), new Vec2(-2, S21) / 5));

    [Fact]
    public void Arc_RestingOnTopOfBump_MovingDown_HitAtZero() =>
        // Resting on the bump's top: touches in the middle of both edges, not at an end.
        AssertContacts(0, Collision.SegmentArc(S(2, -5, -2, -5), Bump(), new Vec2(0, 1)),
            (new Vec2(0, -5), Up));

    [Fact]
    public void Arc_RestingOnTopOfBump_Sliding_NoHit() =>
        Assert.Null(Collision.SegmentArc(S(2, -5, -2, -5), Bump(), new Vec2(1, 0)));

    [Fact]
    public void Arc_AlreadyCrossing_NoHit() =>
        // A segment cutting through the bump at y = -3, moving down: free to leave.
        Assert.Null(Collision.SegmentArc(S(6, -3, -6, -3), Bump(), new Vec2(0, 10)));

    [Fact]
    public void Arc_StopsShort_NoHit() =>
        Assert.Null(Collision.SegmentArc(S(2, -8, -2, -8), Bump(), new Vec2(0, 2.9)));

    [Fact]
    public void Arc_RandomBothSolid_TouchesAtTAndNotBefore()
    {
        var rng = new Random(19);
        int hits = 0;
        static double Dist(Segment s, Arc a)
        {
            var d = new List<double>
            {
                (s.Start - a.ClosestPoint(s.Start)).Length, (s.End - a.ClosestPoint(s.End)).Length,
                (a.Start - s.ClosestPoint(a.Start)).Length, (a.End - s.ClosestPoint(a.End)).Length,
            };
            foreach (int sign in new[] { 1, -1 })
            {
                Vec2 e = a.Center + sign * a.Radius * s.Normal;
                if (a.IsWithinAngle(e - a.Center)) d.Add((e - s.ClosestPoint(e)).Length);
            }
            return d.Min();
        }
        Vec2 Rand() => new(rng.NextDouble() * 20, rng.NextDouble() * 20);
        for (int i = 0; i < 5000; i++)
        {
            Segment s = new(Rand(), Rand(), true);
            double ang = rng.NextDouble() * 2 * Math.PI;
            double ha = (rng.Next(2) == 0 ? 1 : -1) * (0.2 + rng.NextDouble() * 2.8);
            var a = new Arc(Rand(), 1 + rng.NextDouble() * 6, new Vec2(Math.Cos(ang), Math.Sin(ang)), ha, true);
            if ((s.End - s.Start).Length < 0.5) continue;
            if (Overlap.SegmentArc(s, a) != null || Dist(s, a) < 1e-3) continue;
            Vec2 v = new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
            Hit[]? hs = Collision.SegmentArc(s, a, v);
            double end = hs?[0].T ?? 1;
            for (int k = 0; k < 400; k++)
            {
                Vec2 off = (end * k / 400) * v;
                Assert.True(Overlap.SegmentArc(new Segment(s.Start + off, s.End + off), a) == null, $"case {i}: overlaps before T");
            }
            if (hs == null) continue;
            hits++;
            Vec2 o = end * v;
            var moved = new Segment(s.Start + o, s.End + o, true);
            Assert.True(Dist(moved, a) < 1e-7, $"case {i}: not touching at T");
            foreach (Hit h in hs)
            {
                Assert.Equal(end, h.T, 1e-7);
                Assert.True((h.Point - moved.ClosestPoint(h.Point)).Length < 1e-7 && (h.Point - a.ClosestPoint(h.Point)).Length < 1e-7,
                    $"case {i}: contact not on both edges");
                Assert.True(Vec2.Dot(v, h.Normal) < 0, $"case {i}: normal doesn't face the mover");
            }
        }
        Assert.True(hits > 200, $"only {hits} hits");
    }
}
