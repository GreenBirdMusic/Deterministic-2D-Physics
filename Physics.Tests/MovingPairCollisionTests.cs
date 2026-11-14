// AI-GEN AI-013: tests for Christopher's moving-vs-moving overloads (CircleCircle, CircleLoop, LoopLoop, PointLoop with va and vb), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Both objects move during the step: the first by va, the second by vb.
// Contact points are in the world, where the two touch at time T; normals point toward the first object.
public class MovingPairCollisionTests
{
    static Segment S(double ax, double ay, double bx, double by, bool both = false) => new(new Vec2(ax, ay), new Vec2(bx, by), both);

    static IEdge[] Box(double x0, double y0, double x1, double y1) =>
        [S(x0, y0, x1, y0), S(x1, y0, x1, y1), S(x1, y1, x0, y1), S(x0, y1, x0, y0)];

    static readonly IEdge[] OneWay = [S(0, 0, 10, 0)];
    static readonly Vec2 Up = new(0, -1), Left = new(-1, 0);

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

    static void AssertSame(Hit[]? expected, Hit[]? actual)
    {
        Assert.Equal(expected == null, actual == null);
        if (expected == null || actual == null) return;
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i]);
    }

    [Fact]
    public void BoxesMovingTowardEachOther() =>
        // Gap 4, closing at 20 per step: T = 0.2, when A's right side and B's left side are both at x = 4.
        AssertContacts(0.2, Collision.LoopLoop(Box(0, -2, 2, 0), Box(6, -2, 8, 0), new Vec2(10, 0), new Vec2(-10, 0)),
            (new Vec2(4, -2), Left), (new Vec2(4, 0), Left));

    [Fact]
    public void RisingOneWayCatchesFallingBox() =>
        // Gap 4, closing at 10: T = 0.4, the platform has risen to y = -1.6.
        AssertContacts(0.4, Collision.LoopLoop(Box(4, -6, 6, -4), OneWay, new Vec2(0, 6), new Vec2(0, -4)),
            (new Vec2(4, -1.6), Up), (new Vec2(6, -1.6), Up));

    [Fact]
    public void RestingOnRisingPlatform_PlatformFaster_HitAtZero() =>
        // Resting on the platform, both rising, the platform faster: it pushes into the box at T = 0.
        AssertContacts(0, Collision.LoopLoop(Box(4, -2, 6, 0), OneWay, new Vec2(0, -1), new Vec2(0, -3)),
            (new Vec2(4, 0), Up), (new Vec2(6, 0), Up));

    [Fact]
    public void FallingPlatformOutrunsBox_NoHit() =>
        // The platform falls faster than the box above it: they get further apart.
        Assert.Null(Collision.LoopLoop(Box(4, -6, 6, -4), OneWay, new Vec2(0, 2), new Vec2(0, 5)));

    [Fact]
    public void MovingTogether_NoHit()
    {
        Vec2 v = new(3, 7);
        Assert.Null(Collision.LoopLoop(Box(4, -2, 6, 0), Box(0, 0, 10, 10), v, v));
        Assert.Null(Collision.CircleLoop(new Circle(new Vec2(5, -1), 1), Box(0, 0, 10, 10), v, v));
        Assert.Null(Collision.CircleCircle(new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(2, 0), 1), v, v));
        Assert.Null(Collision.PointLoop(new Vec2(5, 0), Box(0, 0, 10, 10), v, v));
    }

    [Fact]
    public void CirclesMovingTowardEachOther() =>
        // Centers 10 apart, radii 1 each, closing at 10: T = 0.8, touching at (5, 0).
        AssertContacts(0.8, Collision.CircleCircle(new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(10, 0), 1), new Vec2(5, 0), new Vec2(-5, 0)),
            (new Vec2(5, 0), Left));

    [Fact]
    public void BallFallsOntoRisingPlatform() =>
        // Ball bottom at y = -4, platform at 0, closing at 10: T = 0.4, platform at y = -2, ball at x = 5 + 0.4 * 2.
        AssertContacts(0.4, Collision.CircleLoop(new Circle(new Vec2(5, -5), 1), OneWay, new Vec2(2, 5), new Vec2(0, -5)),
            (new Vec2(5.8, -2), Up));

    [Fact]
    public void PointHitsMovingBox()
    {
        // Point at (-4, 5) moving right by 2, box's left side at x = 0 moving left by 2: T = 1, meeting at x = -2.
        Hit? h = Collision.PointLoop(new Vec2(-4, 5), Box(0, 0, 10, 10), new Vec2(2, 0), new Vec2(-2, 0));
        Assert.NotNull(h);
        Assert.Equal(1, h.Value.T, 1e-9);
        AssertNear(new Vec2(-2, 5), h.Value.Point);
        AssertNear(Left, h.Value.Normal);
    }

    [Fact]
    public void StillSecondObject_SameAsOneVelocity()
    {
        var rng = new Random(37);
        Vec2 Rand() => new(rng.NextDouble() * 20, rng.NextDouble() * 20);
        for (int i = 0; i < 500; i++)
        {
            IEdge[] a = Box(rng.NextDouble() * 20, rng.NextDouble() * 20, 21 + rng.NextDouble() * 3, 21 + rng.NextDouble() * 3);
            Vec2 o = Rand() - new Vec2(10, 10);
            IEdge[] b = Box(o.X, o.Y, o.X + 1 + rng.NextDouble() * 5, o.Y + 1 + rng.NextDouble() * 5);
            var c = new Circle(Rand(), 0.5 + rng.NextDouble() * 2);
            Vec2 v = new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
            AssertSame(Collision.LoopLoop(a, b, v), Collision.LoopLoop(a, b, v, Vec2.Zero));
            AssertSame(Collision.CircleLoop(c, b, v), Collision.CircleLoop(c, b, v, Vec2.Zero));
            var c2 = new Circle(Rand(), 1);
            AssertSame(Collision.CircleCircle(c, c2, v), Collision.CircleCircle(c, c2, v, Vec2.Zero));
            Vec2 p = Rand();
            Assert.Equal(Collision.PointLoop(p, b, v), Collision.PointLoop(p, b, v, Vec2.Zero));
        }
    }

    [Fact]
    public void RandomBothMovingRectangles_TouchAtTAndNotBefore()
    {
        var rng = new Random(41);
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
        Vec2 RandV() => new(rng.NextDouble() * 30 - 15, rng.NextDouble() * 30 - 15);
        for (int i = 0; i < 3000; i++)
        {
            IEdge[] a = Rect(Rand(), 0.5 + rng.NextDouble() * 3, 0.5 + rng.NextDouble() * 3, rng.NextDouble() * Math.PI);
            IEdge[] b = Rect(Rand(), 0.5 + rng.NextDouble() * 3, 0.5 + rng.NextDouble() * 3, rng.NextDouble() * Math.PI);
            if (Overlap.LoopLoop(a, b) || Dist(a, b) < 1e-3) continue;
            Vec2 va = RandV(), vb = RandV();
            Hit[]? hs = Collision.LoopLoop(a, b, va, vb);
            double end = hs?[0].T ?? 1;
            for (int k = 0; k < 400; k++)
            {
                double t = end * k / 400;
                Assert.False(Overlap.LoopLoop(Moved(a, t * va), Moved(b, t * vb)), $"case {i}: overlaps before T");
            }
            if (hs == null) continue;
            hits++;
            IEdge[] ma = Moved(a, end * va), mb = Moved(b, end * vb);
            Assert.True(Dist(ma, mb) < 1e-7, $"case {i}: not touching at T");
            foreach (Hit h in hs)
            {
                Assert.Equal(end, h.T, 1e-7);
                Assert.True(DistTo(ma, h.Point) < 1e-7 && DistTo(mb, h.Point) < 1e-7, $"case {i}: contact not on both loops at T");
                Assert.True(Vec2.Dot(va - vb, h.Normal) < 0, $"case {i}: normal doesn't face the first object");
            }
        }
        Assert.True(hits > 200, $"only {hits} hits");
    }
}
