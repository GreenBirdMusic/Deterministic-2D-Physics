// AI-GEN AI-013: tests for Christopher's moving edge-vs-edge checks (Collision.SegmentSegment so far), written by Claude. Dev-only, never part of the game.
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
}
