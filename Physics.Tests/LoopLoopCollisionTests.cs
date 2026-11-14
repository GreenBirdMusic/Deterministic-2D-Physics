// AI-GEN AI-013: tests for Christopher's Collision.LoopLoop (and EdgesCollide through it), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Solid on the right of Start -> End; clockwise loops are solid inside.
// The first loop moves by v, the second stays still. Contact normals point from the still loop toward the mover.
public class LoopLoopCollisionTests
{
    static Segment S(double ax, double ay, double bx, double by, bool both = false) => new(new Vec2(ax, ay), new Vec2(bx, by), both);

    // Box from (x0, y0) to (x1, y1), clockwise on screen: top, right, bottom, left.
    static IEdge[] Box(double x0, double y0, double x1, double y1, bool bothTop = false) =>
    [
        S(x0, y0, x1, y0, bothTop),
        S(x1, y0, x1, y1),
        S(x1, y1, x0, y1),
        S(x0, y1, x0, y0),
    ];

    static readonly IEdge[] Ground = Box(0, 0, 10, 10);
    static readonly IEdge[] OneWay = [S(0, 0, 10, 0)];   // a one-way platform: a single edge, solid below
    static readonly Vec2 Up = new(0, -1);

    static void AssertNear(Vec2 expected, Vec2 actual, double tol = 1e-9)
    {
        Assert.Equal(expected.X, actual.X, tol);
        Assert.Equal(expected.Y, actual.Y, tol);
    }

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

    [Fact]
    public void BoxLandsFlat_TwoContacts_CornersNotDoubled() =>
        // Each bottom corner is reached by the bottom edge and a side edge: same point and normal, kept once.
        AssertContacts(0.4, Collision.LoopLoop(Box(4, -6, 6, -4), Ground, new Vec2(0, 10)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void DiamondCornerLands_OneContact()
    {
        IEdge[] diamond = [S(5, -7, 7, -5), S(7, -5, 5, -3), S(5, -3, 3, -5), S(3, -5, 5, -7)];
        AssertContacts(0.3, Collision.LoopLoop(diamond, Ground, new Vec2(0, 10)), (new Vec2(5, 0), Up));
    }

    [Fact]
    public void BoxOverGroundCorner_CornerAndMiddleContacts() =>
        // The mover's corner lands at (8, 0); the ground's corner (10, 0) meets the mover's bottom middle.
        AssertContacts(0.2, Collision.LoopLoop(Box(8, -4, 14, -2), Ground, new Vec2(0, 10)),
            (new Vec2(8, 0), Up), (new Vec2(10, 0), Up));

    [Fact]
    public void EarliestHitWins_SideWallBeforeFloor()
    {
        // Moving down-right: the right side reaches the wall (x = 10) at T = 0.25, before the floor (T = 0.8).
        IEdge[] wall = [S(10, 10, 10, -10)];   // solid to the right, normal left
        AssertContacts(0.25, Collision.LoopLoop(Box(4, -6, 8, -4), [.. Ground, .. wall], new Vec2(8, 5)),
            (new Vec2(10, -4.75), new Vec2(-1, 0)), (new Vec2(10, -2.75), new Vec2(-1, 0)));
    }

    [Fact]
    public void MovingAway_NoHit() =>
        Assert.Null(Collision.LoopLoop(Box(4, -6, 6, -4), Ground, new Vec2(0, -10)));

    [Fact]
    public void StopsShort_NoHit() =>
        Assert.Null(Collision.LoopLoop(Box(4, -6, 6, -4), Ground, new Vec2(0, 3.9)));

    [Fact]
    public void Resting_MovingDown_HitAtZero() =>
        AssertContacts(0, Collision.LoopLoop(Box(4, -2, 6, 0), Ground, new Vec2(0, 1)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void Resting_Sliding_NoHit() =>
        Assert.Null(Collision.LoopLoop(Box(4, -2, 6, 0), Ground, new Vec2(3, 0)));

    [Fact]
    public void LandsOnOneWay() =>
        AssertContacts(0.4, Collision.LoopLoop(Box(4, -6, 6, -4), OneWay, new Vec2(0, 10)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void RisesThroughOneWay_NoHit() =>
        Assert.Null(Collision.LoopLoop(Box(4, 2, 6, 4), OneWay, new Vec2(0, -10)));

    // "80% through, falling": the box's top is 0.4 above the platform, the rest is below it.

    [Fact]
    public void EightyPercentThrough_OneSidedTop_FallsOut() =>
        Assert.Null(Collision.LoopLoop(Box(4, -0.4, 6, 1.6), OneWay, new Vec2(0, 1)));

    [Fact]
    public void EightyPercentThrough_BothSolidTop_CaughtByTop() =>
        // The zipline idea: a BothSolid top edge catches on the platform from above.
        AssertContacts(0.4, Collision.LoopLoop(Box(4, -0.4, 6, 1.6, bothTop: true), OneWay, new Vec2(0, 1)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void EightyPercentThrough_SolidGround_NoSnap() =>
        // Deep inside solid ground (one-sided edges): nothing pushes it back out, it's free to leave.
        Assert.Null(Collision.LoopLoop(Box(4, -0.4, 6, 1.6), Ground, new Vec2(0, 1)));

    // Reversed pairs (the mover's edge is an arc or a curve, the ground's is a segment): checks ReverseHits.

    [Fact]
    public void ArcBottomLands_NormalFacesMover()
    {
        // Bottom half-circle, center (5, -6), radius 2: lowest point (5, -4).
        IEdge[] drop = [S(3, -10, 7, -10), S(7, -10, 7, -6), new Arc(new Vec2(5, -6), 2, new Vec2(0, 1), Math.PI / 2), S(3, -6, 3, -10)];
        AssertContacts(0.4, Collision.LoopLoop(drop, Ground, new Vec2(0, 10)), (new Vec2(5, 0), Up));
    }

    [Fact]
    public void CurveBottomLands_NormalFacesMover()
    {
        // Curve (7, -6) -> (5, -2) -> (3, -6): lowest point (5, -4).
        IEdge[] drop = [S(3, -10, 7, -10), S(7, -10, 7, -6), new Curve(new Vec2(7, -6), new Vec2(5, -2), new Vec2(3, -6)), S(3, -6, 3, -10)];
        AssertContacts(0.4, Collision.LoopLoop(drop, Ground, new Vec2(0, 10)), (new Vec2(5, 0), Up));
    }

    [Fact]
    public void BoxLandsOnCurvedHill()
    {
        // Still hill: curve (-5, 0) -> (0, -10) -> (5, 0) (top at (0, -5)), closed by a flat bottom.
        IEdge[] hill = [new Curve(new Vec2(-5, 0), new Vec2(0, -10), new Vec2(5, 0)), S(5, 0, -5, 0)];
        AssertContacts(0.1, Collision.LoopLoop(Box(-1, -8, 1, -6), hill, new Vec2(0, 10)), (new Vec2(0, -5), Up));
    }

    [Fact]
    public void ArcVsCurve_NotImplementedYet() =>
        Assert.Throws<NotImplementedException>(() => Collision.LoopLoop(
            [new Arc(new Vec2(0, -10), 2, new Vec2(0, 1), Math.PI / 2)],
            [new Curve(new Vec2(-5, 0), new Vec2(0, -10), new Vec2(5, 0))], new Vec2(0, 10)));

    [Fact]
    public void RandomBothSolidRectangles_TouchAtTAndNotBefore()
    {
        var rng = new Random(31);
        int hits = 0;
        IEdge[] Rect(Vec2 c, double w, double h, double ang)
        {
            Vec2 ax = new(Math.Cos(ang), Math.Sin(ang)), ay = ax.Perp();
            Vec2[] p = [c - w * ax - h * ay, c + w * ax - h * ay, c + w * ax + h * ay, c - w * ax + h * ay];
            return [.. Enumerable.Range(0, 4).Select(i => (IEdge)new Segment(p[i], p[(i + 1) % 4], true))];
        }
        static IEdge[] Moved(IEdge[] loop, Vec2 o) => [.. loop.Select(e => (IEdge)new Segment(e.Start + o, e.End + o, true))];
        static double Dist(IEdge[] a, IEdge[] b) =>
            a.SelectMany(e => b.Select(f => Math.Min((e.Start - f.ClosestPoint(e.Start)).Length, (f.Start - e.ClosestPoint(f.Start)).Length))).Min();
        static double DistTo(IEdge[] loop, Vec2 p) => loop.Min(e => (p - e.ClosestPoint(p)).Length);
        Vec2 Rand() => new(rng.NextDouble() * 20, rng.NextDouble() * 20);
        for (int i = 0; i < 3000; i++)
        {
            IEdge[] a = Rect(Rand(), 0.5 + rng.NextDouble() * 3, 0.5 + rng.NextDouble() * 3, rng.NextDouble() * Math.PI);
            IEdge[] b = Rect(Rand(), 0.5 + rng.NextDouble() * 3, 0.5 + rng.NextDouble() * 3, rng.NextDouble() * Math.PI);
            if (Overlap.LoopLoop(a, b) || Dist(a, b) < 1e-3) continue;
            Vec2 v = new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
            Hit[]? hs = Collision.LoopLoop(a, b, v);
            double end = hs?[0].T ?? 1;
            for (int k = 0; k < 400; k++)
                Assert.False(Overlap.LoopLoop(Moved(a, (end * k / 400) * v), b), $"case {i}: overlaps before T");
            if (hs == null) continue;
            hits++;
            IEdge[] m = Moved(a, end * v);
            Assert.True(Dist(m, b) < 1e-7, $"case {i}: not touching at T");
            foreach (Hit h in hs)
            {
                Assert.Equal(end, h.T, 1e-7);
                Assert.True(DistTo(m, h.Point) < 1e-7 && DistTo(b, h.Point) < 1e-7, $"case {i}: contact not on both loops");
                Assert.True(Vec2.Dot(v, h.Normal) < 0, $"case {i}: normal doesn't face the mover");
            }
        }
        Assert.True(hits > 200, $"only {hits} hits");
    }
}
