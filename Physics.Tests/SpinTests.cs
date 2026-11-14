// AI-GEN AI-013: tests for Christopher's spin resistance code (RotationalInertia on circles and edges, EdgeList, ColliderSet), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// J = sum of |p|^2 over the area, about the local origin (the object's Position).
// Screen coordinates (y down): clockwise loops are solid inside.
public class SpinTests
{
    static void AssertNear(double expected, double actual, double tol = 1e-9)
    {
        double scale = Math.Max(1, Math.Abs(expected));
        Assert.True(Math.Abs(expected - actual) < tol * scale, $"expected {expected} but got {actual}");
    }

    static readonly Vec2 Far = new(37.5, -12);
    static readonly CollisionGroup G = new(0);

    static IEdge[] Rect(Vec2 corner, double w, double h) => new IEdge[]
    {
        new Segment(corner, corner + new Vec2(w, 0)),
        new Segment(corner + new Vec2(w, 0), corner + new Vec2(w, h)),
        new Segment(corner + new Vec2(w, h), corner + new Vec2(0, h)),
        new Segment(corner + new Vec2(0, h), corner),
    };

    static IEdge[] Reversed(IEdge[] loop)
    {
        var r = new IEdge[loop.Length];
        for (int i = 0; i < loop.Length; i++)
        {
            var s = (Segment)loop[loop.Length - 1 - i];
            r[i] = new Segment(s.End, s.Start);
        }
        return r;
    }

    static double J(IEdge[] loop) => new EdgeList(loop, true).RotationalInertia;

    static Vec2 ArcPoint(Arc a, double t)
    {
        double ang = -a.HalfAngle + 2 * a.HalfAngle * t;
        double cos = Math.Cos(ang), sin = Math.Sin(ang);
        return a.Center + new Vec2(a.Direction.X * cos - a.Direction.Y * sin,
                                   a.Direction.X * sin + a.Direction.Y * cos) * a.Radius;
    }

    // J of a loop with every edge cut into many tiny segments (the polygon formula).
    static double ApproxJ(IEdge[] loop)
    {
        double total = 0;
        int n = 20000;
        foreach (var e in loop)
        {
            Func<double, Vec2> at = e switch
            {
                Segment s => t => s.Start + (s.End - s.Start) * t,
                Curve c => t => c.PointAt(t),
                Arc a => t => ArcPoint(a, t),
                _ => throw new NotImplementedException(),
            };
            for (int i = 0; i < n; i++)
            {
                Vec2 p = at((double)i / n), q = at((double)(i + 1) / n);
                total += Vec2.Cross(p, q) * (Vec2.Dot(p, p) + Vec2.Dot(p, q) + Vec2.Dot(q, q)) / 12;
            }
        }
        return total;
    }

    // ---------- Segments ----------

    [Fact]
    public void RectangleAroundTheOrigin()
    {
        // w x h centered on the origin: A (w^2 + h^2) / 12.
        double w = 4, h = 3;
        AssertNear(w * h * (w * w + h * h) / 12, J(Rect(new Vec2(-w / 2, -h / 2), w, h)));
    }

    [Fact]
    public void MovedRectangleFollowsTheParallelAxisTheorem()
    {
        double w = 4, h = 3, a = w * h;
        var center = Far + new Vec2(w / 2, h / 2);
        AssertNear(a * (w * w + h * h) / 12 + a * center.LengthSquared, J(Rect(Far, w, h)));
    }

    [Fact]
    public void BackwardsLoopIsNegative()
    {
        AssertNear(-J(Rect(Far, 4, 3)), J(Reversed(Rect(Far, 4, 3))));
    }

    [Fact]
    public void TriangleMatchesManyPieces()
    {
        var a = Far; var b = Far + new Vec2(6, 1); var c = Far + new Vec2(-1, 3);
        var loop = new IEdge[] { new Segment(a, b), new Segment(b, c), new Segment(c, a) };
        AssertNear(ApproxJ(loop), J(loop), 1e-7);
    }

    // ---------- Circles ----------

    [Fact]
    public void CircleAtTheOrigin()
    {
        AssertNear(Math.PI * 16 / 2, new Circle(new Vec2(0, 0), 2).RotationalInertia());
    }

    [Fact]
    public void MovedCircle()
    {
        AssertNear(Math.PI * (16.0 / 2 + 4 * Far.LengthSquared), new Circle(Far, 2).RotationalInertia());
    }

    // ---------- Arcs ----------

    [Fact]
    public void FullBumpArcMatchesTheCircle()
    {
        var arc = new Arc(Far, 2, new Vec2(0.6, 0.8), Math.PI);
        AssertNear(new Circle(Far, 2).RotationalInertia(), J(new IEdge[] { arc }));
    }

    [Fact]
    public void FullDentArcIsTheNegativeCircle()
    {
        var arc = new Arc(Far, 2, new Vec2(0.6, 0.8), -Math.PI);
        AssertNear(-new Circle(Far, 2).RotationalInertia(), J(new IEdge[] { arc }));
    }

    [Fact]
    public void HalfDiscAroundItsCircleCenter()
    {
        // Half of a disc, about the disc's center: pi r^4 / 4.
        double r = 2;
        var arc = new Arc(new Vec2(0, 0), r, new Vec2(0, -1), Math.PI / 2);
        var loop = new IEdge[] { arc, new Segment(arc.End, arc.Start) };
        AssertNear(Math.PI * Math.Pow(r, 4) / 4, J(loop));
    }

    [Fact]
    public void ArcsMatchManyPieces()
    {
        foreach (double h in new[] { 0.05, 0.3, 1.0, Math.PI / 2, 2.5, 3.0, -0.7, -2.2 })
        {
            var arc = new Arc(Far + new Vec2(1, 2), 1.7, new Vec2(0.6, -0.8), h);
            var loop = new IEdge[] { arc, new Segment(arc.End, arc.Start) };
            AssertNear(ApproxJ(loop), J(loop), 1e-6);
        }
    }

    [Fact]
    public void SquareWithADentMatchesManyPieces()
    {
        var dent = new Arc(Far + new Vec2(0, -1), 1, new Vec2(0, 1), -Math.PI / 2);
        var loop = new IEdge[]
        {
            dent,
            new Segment(Far + new Vec2(1, -1), Far + new Vec2(1, 1)),
            new Segment(Far + new Vec2(1, 1), Far + new Vec2(-1, 1)),
            new Segment(Far + new Vec2(-1, 1), Far + new Vec2(-1, -1)),
        };
        AssertNear(ApproxJ(loop), J(loop), 1e-6);
    }

    // ---------- Curves ----------

    [Fact]
    public void StraightCurveActsLikeASegment()
    {
        var c = new Curve(Far, Far + new Vec2(1, 0.5), Far + new Vec2(2, 1));
        AssertNear(new Segment(c.Start, c.End).RotationalInertia(), c.RotationalInertia());
    }

    [Fact]
    public void ParabolaCapAroundItsTip()
    {
        // Cap of y = x^2 under y = 1, tip at the origin. J = integral of (x^2 + y^2) over the cap:
        // over x in [-1, 1] of (x^2 (1 - x^2) + (1 - x^6) / 3) dx = 4/15 + 4/7 = 88/105.
        var curve = new Curve(new Vec2(-1, 1), new Vec2(0, -1), new Vec2(1, 1));
        var loop = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        AssertNear(88.0 / 105, J(loop));
    }

    [Fact]
    public void CurvesMatchManyPieces()
    {
        var curves = new[]
        {
            new Curve(Far + new Vec2(-2, 1), Far + new Vec2(3, -4), Far + new Vec2(2, 2)),
            new Curve(Far + new Vec2(-2, 1), Far + new Vec2(0, 5), Far + new Vec2(2, 1)),
            new Curve(new Vec2(-3, 0), new Vec2(8, -6), new Vec2(1, 2)),
        };
        foreach (var curve in curves)
        {
            var loop = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
            AssertNear(ApproxJ(loop), J(loop), 1e-6);
        }
    }

    // ---------- EdgeList and ColliderSet ----------

    [Fact]
    public void OpenListHasNoSpinResistance()
    {
        var platform = new EdgeList(new IEdge[] { new Segment(new Vec2(30, 2), new Vec2(80, 2)) }, false);
        Assert.Equal(0, platform.RotationalInertia);
    }

    [Fact]
    public void RingAroundItsCenter()
    {
        var outer = new EdgeList(new IEdge[] { new Arc(new Vec2(0, 0), 3, new Vec2(1, 0), Math.PI) }, true);
        var hole = new EdgeList(new IEdge[] { new Arc(new Vec2(0, 0), 2, new Vec2(1, 0), -Math.PI) }, true);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { outer, hole });
        AssertNear(Math.PI * (81 - 16) / 2, set.RotationalInertia);
    }

    [Fact]
    public void SetAddsCirclesAndClosedListsButNotOpenOnes()
    {
        var circles = new[] { new Circle(Far, 1), new Circle(new Vec2(-3, 4), 0.5) };
        var box = new EdgeList(Rect(new Vec2(1, 1), 2, 5), true);
        var platform = new EdgeList(new IEdge[] { new Segment(new Vec2(30, 2), new Vec2(80, 2)) }, false);
        var set = new ColliderSet(G, 1, circles, new[] { box, platform });
        AssertNear(circles[0].RotationalInertia() + circles[1].RotationalInertia() + box.RotationalInertia,
            set.RotationalInertia);
    }

    [Fact]
    public void FrameWithAHole()
    {
        var outer = new EdgeList(Rect(new Vec2(-2, -2), 4, 4), true);
        var hole = new EdgeList(Reversed(Rect(new Vec2(-1, -1), 2, 2)), true);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { outer, hole });
        AssertNear(16 * 32 / 12.0 - 4 * 8 / 12.0, set.RotationalInertia);
    }

    [Fact]
    public void SpinResistanceUsesLocalShapes()
    {
        var set = new ColliderSet(G, 1, new[] { new Circle(Far, 1) }, new[] { new EdgeList(Rect(Far, 4, 3), true) });
        double before = set.RotationalInertia;
        set.UpdateWorld(Transform.From(new Vec2(-5, 5), 1.1, true));
        Assert.Equal(before, set.RotationalInertia);
    }
}
