// AI-GEN AI-013: tests for Christopher's center code (Moment on each edge, EdgeList.Moment, ColliderSet.Moment / Center), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates (y down): clockwise loops are solid inside.
public class CenterTests
{
    static void AssertNear(Vec2 expected, Vec2 actual, double tol = 1e-9)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < tol && Math.Abs(expected.Y - actual.Y) < tol,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    static readonly Vec2 Far = new(37.5, -12);
    static readonly CollisionGroup G = new(0);

    static IEdge[] Rect(Vec2 o, double w, double h) => new IEdge[]
    {
        new Segment(o, o + new Vec2(w, 0)),
        new Segment(o + new Vec2(w, 0), o + new Vec2(w, h)),
        new Segment(o + new Vec2(w, h), o + new Vec2(0, h)),
        new Segment(o + new Vec2(0, h), o),
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

    static Vec2 CenterOf(params EdgeList[] lists) => new ColliderSet(G, 1, new Circle[0], lists).Center;
    static Vec2 CenterOf(IEdge[] loop) => CenterOf(new EdgeList(loop, true));

    // Moment of an edge list, checked against cutting every edge into many tiny segments.
    static Vec2 ApproxMoment(IEdge[] loop)
    {
        Vec2 m = new(0, 0);
        int n = 4000;
        foreach (var e in loop)
        {
            Func<double, Vec2> at = e switch
            {
                Segment s => t => s.Start + (s.End - s.Start) * t,
                Curve c => t => c.PointAt(t),
                Arc a => t =>
                {
                    // From Start (Direction turned by -HalfAngle) to End (+HalfAngle).
                    double ang = -a.HalfAngle + 2 * a.HalfAngle * t;
                    double cos = Math.Cos(ang), sin = Math.Sin(ang);
                    return a.Center + new Vec2(a.Direction.X * cos - a.Direction.Y * sin,
                                               a.Direction.X * sin + a.Direction.Y * cos) * a.Radius;
                },
                _ => throw new NotImplementedException(),
            };
            for (int i = 0; i < n; i++)
            {
                Vec2 p = at((double)i / n), q = at((double)(i + 1) / n);
                m += (p + q) * (Vec2.Cross(p, q) / 6);
            }
        }
        return m;
    }

    // ---------- Edge moments ----------

    [Fact]
    public void SegmentMomentIsTriangleAreaTimesTriangleCenter()
    {
        var s = new Segment(new Vec2(3, 0), new Vec2(0, 6));
        // Triangle (0,0), (3,0), (0,6): area 9 (clockwise on screen), center (1, 2).
        AssertNear(new Vec2(9, 18), s.Moment());
    }

    [Fact]
    public void ArcMomentMatchesManyPieces()
    {
        foreach (double h in new[] { 0.3, 1.0, Math.PI / 2, 2.5, -0.7, -2.2 })
        {
            var arc = new Arc(Far + new Vec2(1, 2), 1.7, new Vec2(0.6, -0.8), h);
            var loop = new IEdge[] { arc, new Segment(arc.End, arc.Start) };
            AssertNear(ApproxMoment(loop), arc.Moment() + loop[1].Moment(), 1e-3);
        }
    }

    [Fact]
    public void CurveMomentMatchesManyPieces()
    {
        var curve = new Curve(Far + new Vec2(-2, 1), Far + new Vec2(3, -4), Far + new Vec2(2, 2));
        var loop = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        AssertNear(ApproxMoment(loop), curve.Moment() + loop[1].Moment(), 1e-3);
    }

    // ---------- Centers of known shapes ----------

    [Fact]
    public void RectangleCenterIsItsMiddle()
    {
        AssertNear(Far + new Vec2(2, 1.5), CenterOf(Rect(Far, 4, 3)));
    }

    [Fact]
    public void BackwardsRectangleHasTheSameCenter()
    {
        // Negative area and negative moment: the center comes out the same.
        AssertNear(Far + new Vec2(2, 1.5), CenterOf(Reversed(Rect(Far, 4, 3))));
    }

    [Fact]
    public void TriangleCenterIsTheAverageOfItsCorners()
    {
        var a = Far; var b = Far + new Vec2(6, 0); var c = Far + new Vec2(0, 3);
        var loop = new IEdge[] { new Segment(a, b), new Segment(b, c), new Segment(c, a) };
        AssertNear((a + b + c) / 3, CenterOf(loop));
    }

    [Fact]
    public void HalfDiscCenterIsFourROverThreePiFromTheFlatSide()
    {
        double r = 2;
        var arc = new Arc(Far, r, new Vec2(0, -1), Math.PI / 2); // bulges up (negative y)
        var loop = new IEdge[] { arc, new Segment(arc.End, arc.Start) };
        AssertNear(Far + new Vec2(0, -4 * r / (3 * Math.PI)), CenterOf(loop));
    }

    [Fact]
    public void FullArcCenterIsTheCircleCenter()
    {
        AssertNear(Far, CenterOf(new IEdge[] { new Arc(Far, 2, new Vec2(0.6, 0.8), Math.PI) }));
    }

    [Fact]
    public void ParabolaCapCenterIsTwoFifthsTowardTheCurve()
    {
        // Cap of y = x^2 under y = 1, moved to Far: center 2/5 of the way from the chord (y = 1) to the tip (y = 0).
        var curve = new Curve(Far + new Vec2(-1, 1), Far + new Vec2(0, -1), Far + new Vec2(1, 1));
        var loop = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        // Make sure it's wound so the area is positive (just a check on the test itself).
        Assert.True(new EdgeList(loop, true).Area > 0);
        AssertNear(Far + new Vec2(0, 0.6), CenterOf(loop));
    }

    [Fact]
    public void LopsidedCurveCapMatchesManyPieces()
    {
        var curve = new Curve(Far + new Vec2(-2, 1), Far + new Vec2(3, -4), Far + new Vec2(2, 2));
        var loop = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        var list = new EdgeList(loop, true);
        AssertNear(ApproxMoment(loop) / list.Area, CenterOf(list), 1e-3);
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
        var list = new EdgeList(loop, true);
        AssertNear(ApproxMoment(loop) / list.Area, CenterOf(list), 1e-3);
    }

    // ---------- Sets ----------

    [Fact]
    public void SingleCircleCenter()
    {
        var set = new ColliderSet(G, 1, new[] { new Circle(Far, 2) }, new EdgeList[0]);
        AssertNear(Far, set.Center);
        AssertNear(Far * (4 * Math.PI), set.Moment);
    }

    [Fact]
    public void TwoCirclesWeightedByArea()
    {
        // Area 1 pi at (0,0) and 4 pi at (10,0): center at 8 along x.
        var set = new ColliderSet(G, 1, new[] { new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(10, 0), 2) }, new EdgeList[0]);
        AssertNear(new Vec2(8, 0), set.Center);
    }

    [Fact]
    public void CircleAndRectangleTogether()
    {
        // Rectangle area 12 centered at (2, 1.5); circle area pi at (10, 0).
        var set = new ColliderSet(G, 1, new[] { new Circle(new Vec2(10, 0), 1) },
            new[] { new EdgeList(Rect(new Vec2(0, 0), 4, 3), true) });
        var expected = (new Vec2(2, 1.5) * 12 + new Vec2(10, 0) * Math.PI) / (12 + Math.PI);
        AssertNear(expected, set.Center);
    }

    [Fact]
    public void FrameWithAnOffCenterHole()
    {
        // 4x4 square at the origin minus a 1x1 hole at (2.5..3.5, 0.5..1.5).
        var outer = new EdgeList(Rect(new Vec2(0, 0), 4, 4), true);
        var hole = new EdgeList(Reversed(Rect(new Vec2(2.5, 0.5), 1, 1)), true);
        var expected = (new Vec2(2, 2) * 16 - new Vec2(3, 1) * 1) / 15;
        AssertNear(expected, CenterOf(outer, hole));
    }

    [Fact]
    public void OpenListAddsNoMoment()
    {
        var platform = new EdgeList(new IEdge[] { new Segment(new Vec2(30, 2), new Vec2(80, 2)) }, false);
        AssertNear(new Vec2(0, 0), platform.Moment);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { new EdgeList(Rect(Far, 4, 3), true), platform });
        AssertNear(Far + new Vec2(2, 1.5), set.Center);
    }

    [Fact]
    public void NoAreaGivesCenterZero()
    {
        var platform = new EdgeList(new IEdge[] { new Segment(new Vec2(30, 2), new Vec2(80, 2)) }, false);
        AssertNear(new Vec2(0, 0), new ColliderSet(G, 0, new Circle[0], new[] { platform }).Center);
    }

    [Fact]
    public void CenterUsesLocalShapesNotWorld()
    {
        var set = new ColliderSet(G, 1, new[] { new Circle(Far, 1) }, new[] { new EdgeList(Rect(Far, 4, 3), true) });
        var before = set.Center;
        set.UpdateWorld(Transform.From(new Vec2(-5, 5), 1.1, true));
        AssertNear(before, set.Center);
    }
}
