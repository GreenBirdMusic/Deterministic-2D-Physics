// AI-GEN AI-013: tests for Christopher's Collision.PointLoop (and PointEdge through it), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Solid on the right of Start -> End; normals point to the open side.
public class LoopCollisionTests
{
    static Segment Seg(double ax, double ay, double bx, double by, bool bothSolid = false) =>
        new(new Vec2(ax, ay), new Vec2(bx, by), bothSolid);

    // Box from (x0, y0) to (x1, y1), drawn clockwise on screen: solid inside.
    static IEdge[] Box(double x0, double y0, double x1, double y1) =>
    [
        Seg(x0, y0, x1, y0),  // top, normal up
        Seg(x1, y0, x1, y1),  // right, normal right
        Seg(x1, y1, x0, y1),  // bottom, normal down
        Seg(x0, y1, x0, y0),  // left, normal left
    ];

    static readonly IEdge[] Box10 = Box(0, 0, 10, 10);
    static readonly double R = Math.Sqrt(0.5);

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

    [Fact]
    public void EmptyLoop_NoHit() =>
        Assert.Null(Collision.PointLoop(new Vec2(0, 0), [], new Vec2(5, 5)));

    [Fact]
    public void Miss_NoHit() =>
        Assert.Null(Collision.PointLoop(new Vec2(-5, -5), Box10, new Vec2(20, 0)));

    [Fact]
    public void StopsShort_NoHit() =>
        Assert.Null(Collision.PointLoop(new Vec2(5, -5), Box10, new Vec2(0, 4)));

    [Fact]
    public void HitsTop() =>
        AssertHit(0.5, new Vec2(5, 0), new Vec2(0, -1), Collision.PointLoop(new Vec2(5, -5), Box10, new Vec2(0, 10)));

    [Fact]
    public void HitsLeft() =>
        AssertHit(0.25, new Vec2(0, 3), new Vec2(-1, 0), Collision.PointLoop(new Vec2(-2, 3), Box10, new Vec2(8, 0)));

    [Fact]
    public void ThroughWholeBox_EarliestEdgeWins()
    {
        // Passing all the way through: only the entry edge counts (the exit edge is crossed from inside).
        AssertHit(0.1, new Vec2(0, 5), new Vec2(-1, 0), Collision.PointLoop(new Vec2(-2, 5), Box10, new Vec2(20, 0)));
        // Same, from the other side, so the list order isn't what picks it.
        AssertHit(0.1, new Vec2(10, 5), new Vec2(1, 0), Collision.PointLoop(new Vec2(12, 5), Box10, new Vec2(-20, 0)));
    }

    [Fact]
    public void Corner_NormalsAveraged()
    {
        AssertHit(0.5, new Vec2(0, 0), new Vec2(-R, -R), Collision.PointLoop(new Vec2(-5, -5), Box10, new Vec2(10, 10)));
        AssertHit(0.5, new Vec2(10, 10), new Vec2(R, R), Collision.PointLoop(new Vec2(15, 15), Box10, new Vec2(-10, -10)));
    }

    [Fact]
    public void Corner_OffDiagonal_StillAveraged()
    {
        // Hitting the corner at a shallow angle: still a corner hit, so still the diagonal normal.
        AssertHit(0.5, new Vec2(10, 0), new Vec2(R, -R), Collision.PointLoop(new Vec2(14, -1), Box10, new Vec2(-8, 2)));
    }

    [Fact]
    public void Corner_EdgeOrderDoesNotMatter()
    {
        IEdge[] reversedStart = [Box10[2], Box10[3], Box10[0], Box10[1]];
        AssertHit(0.5, new Vec2(0, 0), new Vec2(-R, -R), Collision.PointLoop(new Vec2(-5, -5), reversedStart, new Vec2(10, 10)));
    }

    [Fact]
    public void Corner_AwkwardNumbers_StillAveraged()
    {
        // Corner at (0.1, 0.3): neither number is exact in binary, so the two edges can round the
        // same T slightly differently. The hit should still count as a corner.
        var box = Box(0.1, 0.3, 0.7, 1.9);
        var start = new Vec2(0.1 - 0.37, 0.3 - 0.29);
        var v = new Vec2(0.37 * 2, 0.29 * 2);
        AssertHit(0.5, new Vec2(0.1, 0.3), new Vec2(-R, -R), Collision.PointLoop(start, box, v), 1e-7);
    }

    [Fact]
    public void JustPastCorner_SingleEdge()
    {
        // Misses the corner by a little and hits only the top edge.
        AssertHit(0.5, new Vec2(0.5, 0), new Vec2(0, -1), Collision.PointLoop(new Vec2(-4.5, -5), Box10, new Vec2(10, 10)));
    }

    [Fact]
    public void StartingInside_CanLeave()
    {
        Assert.Null(Collision.PointLoop(new Vec2(5, 5), Box10, new Vec2(20, 0)));
        Assert.Null(Collision.PointLoop(new Vec2(5, 5), Box10, new Vec2(10, 10))); // out through a corner
    }

    [Fact]
    public void StartingOnEdge_MovingIn_HitsAtZero() =>
        AssertHit(0, new Vec2(5, 0), new Vec2(0, -1), Collision.PointLoop(new Vec2(5, 0), Box10, new Vec2(0, 3)));

    [Fact]
    public void MixedEdges_ArcBumpOnTop()
    {
        // Box whose top is a bump (arc centered at (5, 0), radius 5, bulging up). Solid inside.
        var arc = new Arc(new Vec2(5, 0), 5, new Vec2(0, -1), Math.PI / 2);
        IEdge[] loop = [arc, Box10[1], Box10[2], Box10[3]];
        AssertHit(0.5, new Vec2(5, -5), new Vec2(0, -1), Collision.PointLoop(new Vec2(5, -10), loop, new Vec2(0, 10)));
    }
    [Fact]
    public void Corner_RandomBoxes_AlwaysBothEdgesTied()
    {
        // 100,000 random boxes, a point flying exactly into the top-left corner each time. Rounding used
        // to make one or both edges miss (both = slipping into the box). Fixed seed, so it's repeatable.
        var rng = new Random(1);
        for (int i = 0; i < 100000; i++)
        {
            double x0 = rng.NextDouble() * 100, y0 = rng.NextDouble() * 100;
            var box = Box(x0, y0, x0 + 1 + rng.NextDouble() * 50, y0 + 1 + rng.NextDouble() * 50);
            var d = new Vec2(1 + rng.NextDouble() * 9, 1 + rng.NextDouble() * 9);
            Hit? h = Collision.PointLoop(new Vec2(x0, y0) - d, box, 2 * d);
            Assert.True(h.HasValue, $"case {i}: slipped through the corner");
            Assert.Equal(0.5, h.Value.T, 1e-9);
            AssertNear(new Vec2(-R, -R), h.Value.Normal);
        }
    }
    // ---- Corners where arcs and curves meet other edges (random, fixed seed) ----

    static Vec2 RandDir(Random rng)
    {
        double a = rng.NextDouble() * 2 * Math.PI;
        return new Vec2(Math.Cos(a), Math.Sin(a));
    }

    static double RandHalfAngle(Random rng) =>
        (rng.Next(2) == 0 ? 1 : -1) * (0.2 + rng.NextDouble() * 2.3);  // bumps and dents

    // An edge of the given kind ('s', 'a', 'c') that ends at c (first) or starts at c (second).
    static IEdge EdgeAt(Random rng, char kind, Vec2 c, bool first)
    {
        double len = 2 + rng.NextDouble() * 48;
        switch (kind)
        {
            case 's':
            {
                Vec2 o = c + RandDir(rng) * len;
                return first ? Seg(o.X, o.Y, c.X, c.Y) : Seg(c.X, c.Y, o.X, o.Y);
            }
            case 'c':
            {
                Vec2 a = c + RandDir(rng) * len, b = c + RandDir(rng) * len;
                return first ? new Curve(b, a, c) : new Curve(c, a, b);
            }
            default:
            {
                // Arc through c: pick the center, then turn the direction so c is its End (first) or Start.
                double r = len, ha = RandHalfAngle(rng);
                Vec2 center = c - RandDir(rng) * r;
                Vec2 d = (c - center) / r;
                double turn = first ? -ha : ha;  // End = Direction rotated by +ha, Start by -ha
                Vec2 dir = new(d.X * Math.Cos(turn) - d.Y * Math.Sin(turn), d.X * Math.Sin(turn) + d.Y * Math.Cos(turn));
                return new Arc(center, r, dir, ha);
            }
        }
    }

    static Vec2 CornerNormal(IEdge e, Vec2 c, bool first) => e switch
    {
        Segment s => s.Normal,
        Arc r => r.NormalAt(c),
        Curve k => k.NormalAt(first ? 1 : 0),
        _ => throw new NotImplementedException()
    };

    [Theory]
    [InlineData("sa")] [InlineData("as")] [InlineData("sc")] [InlineData("cs")]
    [InlineData("ac")] [InlineData("ca")] [InlineData("aa")] [InlineData("cc")]
    public void Corner_MixedEdges_RandomCorners_AlwaysTied(string pair)
    {
        // A point flying exactly into the corner where two edges meet. Rounding at the ends of arcs
        // (IsWithinAngle) and curves (t in [0, 1]) used to make one or both edges miss.
        var rng = new Random(pair[0] * 31 + pair[1]);
        int tested = 0;
        for (int i = 0; i < 20000; i++)
        {
            // An arc fixes the corner (its own computed Start/End); the other edge is built onto it.
            Vec2 c = new(rng.NextDouble() * 100, rng.NextDouble() * 100);
            IEdge e1 = EdgeAt(rng, pair[0], c, true);
            if (e1 is Arc a1) c = a1.End;
            IEdge e2 = EdgeAt(rng, pair[1], c, false);
            if (e2 is Arc a2)
            {
                if ((a2.Start - c).Length > 1e-9) continue;  // two arcs whose computed ends don't line up
                c = a2.Start;
            }

            Vec2 sum = CornerNormal(e1, c, true) + CornerNormal(e2, c, false);
            if (sum.Length < 0.1) continue;  // nearly opposite normals: a zero-thickness spike
            Vec2 v = -sum.Normalized() * (0.1 + rng.NextDouble() * 4);
            Hit? h = Collision.PointLoop(c - v / 2, [e1, e2], v);
            Assert.True(h.HasValue, $"{pair} case {i}: slipped through the corner");
            Assert.Equal(0.5, h.Value.T, 1e-6);
            AssertNear(sum.Normalized(), h.Value.Normal, 1e-6);
            tested++;
        }
        Assert.True(tested > 18000, $"only {tested} cases tested");
    }
}
