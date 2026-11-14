// AI-GEN AI-013: tests for Christopher's area code (AreaPiece on each edge, EdgeList.Area, ColliderSet.Area), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates (y down): clockwise loops are solid inside and have positive area.
public class AreaTests
{
    static void AssertNear(double expected, double actual)
    {
        Assert.True(Math.Abs(expected - actual) < 1e-9, $"expected {expected} but got {actual}");
    }

    static IEdge[] Square(Vec2 o) => new IEdge[]
    {
        new Segment(o + new Vec2(-1, -1), o + new Vec2(1, -1)),
        new Segment(o + new Vec2(1, -1), o + new Vec2(1, 1)),
        new Segment(o + new Vec2(1, 1), o + new Vec2(-1, 1)),
        new Segment(o + new Vec2(-1, 1), o + new Vec2(-1, -1)),
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

    static double Area(IEdge[] edges) => new EdgeList(edges, true).Area;

    static readonly Vec2 Far = new(37.5, -12);

    // ---------- Segments (shoelace) ----------

    [Fact]
    public void SquareHasAreaFour()
    {
        AssertNear(4, Area(Square(new Vec2(0, 0))));
    }

    [Fact]
    public void AreaDoesNotDependOnWhereTheOriginIs()
    {
        AssertNear(4, Area(Square(Far)));
    }

    [Fact]
    public void CounterclockwiseLoopHasNegativeArea()
    {
        AssertNear(-4, Area(Reversed(Square(Far))));
    }

    [Fact]
    public void Triangle()
    {
        // Right triangle, legs 3 and 4, clockwise on screen.
        var edges = new IEdge[]
        {
            new Segment(new Vec2(0, 0), new Vec2(3, 0)),
            new Segment(new Vec2(3, 0), new Vec2(0, 4)),
            new Segment(new Vec2(0, 4), new Vec2(0, 0)),
        };
        AssertNear(6, Area(edges));
    }

    // ---------- Arcs ----------

    // Half disc, radius r, flat side down: arc over the top (left to right), then back along the bottom.
    static IEdge[] HalfDisc(Vec2 c, double r)
    {
        var arc = new Arc(c, r, new Vec2(0, -1), Math.PI / 2);
        return new IEdge[] { arc, new Segment(arc.End, arc.Start) };
    }

    [Fact]
    public void HalfDiscArcGoesLeftToRight()
    {
        // Checks the test's own setup: Start on the left, End on the right.
        var arc = (Arc)HalfDisc(new Vec2(0, 0), 1)[0];
        Assert.True(arc.Start.X < -0.99 && arc.End.X > 0.99);
    }

    [Fact]
    public void HalfDiscArea()
    {
        AssertNear(Math.PI / 2, Area(HalfDisc(new Vec2(0, 0), 1)));
        AssertNear(Math.PI * 9 / 2, Area(HalfDisc(Far, 3)));
    }

    [Fact]
    public void ThreeQuarterDiscPlusTriangle()
    {
        // Arc over 270 degrees, closed by its chord: 3/4 of the disc plus the triangle
        // (center, End, Start) with a right angle at the center: area 1/2 for r = 1.
        var arc = new Arc(Far, 1, new Vec2(0, -1), Math.PI * 3 / 4);
        var edges = new IEdge[] { arc, new Segment(arc.End, arc.Start) };
        AssertNear(Math.PI * 3 / 4 + 0.5, Area(edges));
    }

    [Fact]
    public void SmallArcCap()
    {
        // 60 degree arc closed by its chord: sector minus triangle.
        double r = 2, theta = Math.PI / 3;
        var arc = new Arc(Far, r, new Vec2(1, 0), theta / 2);
        var edges = new IEdge[] { arc, new Segment(arc.End, arc.Start) };
        AssertNear(0.5 * r * r * (theta - Math.Sin(theta)), Area(edges));
    }

    [Fact]
    public void FullBumpArcIsTheWholeDisc()
    {
        AssertNear(Math.PI * 4, Area(new IEdge[] { new Arc(Far, 2, new Vec2(1, 0), Math.PI) }));
    }

    [Fact]
    public void FullDentArcIsANegativeDisc()
    {
        AssertNear(-Math.PI, Area(new IEdge[] { new Arc(Far, 1, new Vec2(0, 1), -Math.PI) }));
    }

    [Fact]
    public void DentInASquare()
    {
        // Square with its top edge replaced by a half circle bowl (radius 1) dipping into it.
        var o = new Vec2(0, 0);
        var dent = new Arc(new Vec2(0, -1), 1, new Vec2(0, 1), -Math.PI / 2);
        var sq = Square(o);
        // Check the test's own setup: the dent must run left to right like the edge it replaces.
        Assert.True(dent.Start.X < -0.99 && dent.End.X > 0.99);
        var edges = new IEdge[] { dent, sq[1], sq[2], sq[3] };
        AssertNear(4 - Math.PI / 2, Area(edges));
    }

    // ---------- Curves ----------

    [Fact]
    public void ParabolaCapIsTwoThirdsOfItsTriangle()
    {
        // Curve over the top from (-1, 0) to (1, 0), control (0, -2): triangle area 2, cap 4/3.
        var curve = new Curve(new Vec2(-1, 0), new Vec2(0, -2), new Vec2(1, 0));
        var edges = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        AssertNear(4.0 / 3, Area(edges));
    }

    [Fact]
    public void ParabolaCapAwayFromTheOrigin()
    {
        var curve = new Curve(Far + new Vec2(-1, 0), Far + new Vec2(0, -2), Far + new Vec2(1, 0));
        var edges = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        AssertNear(4.0 / 3, Area(edges));
    }

    [Fact]
    public void SquareWithBulgingAndDentedCurvedTops()
    {
        var sq = Square(Far);
        var up = new Curve(Far + new Vec2(-1, -1), Far + new Vec2(0, -3), Far + new Vec2(1, -1));
        var down = new Curve(Far + new Vec2(-1, -1), Far + new Vec2(0, 1), Far + new Vec2(1, -1));
        AssertNear(4 + 4.0 / 3, Area(new IEdge[] { up, sq[1], sq[2], sq[3] }));
        AssertNear(4 - 4.0 / 3, Area(new IEdge[] { down, sq[1], sq[2], sq[3] }));
    }

    [Fact]
    public void StraightCurveActsLikeASegment()
    {
        var sq = Square(Far);
        var flat = new Curve(Far + new Vec2(-1, -1), Far + new Vec2(0.3, -1), Far + new Vec2(1, -1));
        AssertNear(4, Area(new IEdge[] { flat, sq[1], sq[2], sq[3] }));
    }

    [Fact]
    public void CurveMatchesAManyPieceShoelace()
    {
        // A lopsided curve: compare against the curve cut into 2000 tiny segments.
        var curve = new Curve(Far + new Vec2(-2, 1), Far + new Vec2(3, -4), Far + new Vec2(2, 2));
        var edges = new IEdge[] { curve, new Segment(curve.End, curve.Start) };
        double approx = 0;
        int n = 2000;
        for (int i = 0; i < n; i++)
        {
            approx += Vec2.Cross(curve.PointAt((double)i / n), curve.PointAt((double)(i + 1) / n)) / 2;
        }
        approx += Vec2.Cross(curve.End, curve.Start) / 2;
        Assert.True(Math.Abs(approx - Area(edges)) < 1e-4, $"expected about {approx} but got {Area(edges)}");
    }

    // ---------- EdgeList and ColliderSet ----------

    [Fact]
    public void OpenListHasNoArea()
    {
        var platform = new IEdge[] { new Segment(new Vec2(3, 2), new Vec2(8, 2)), new Segment(new Vec2(8, 2), new Vec2(8, 7)) };
        AssertNear(0, new EdgeList(platform, false).Area);
    }

    [Fact]
    public void AreaIgnoresTheWorldTransform()
    {
        var list = new EdgeList(Square(Far), true);
        list.UpdateWorld(Transform.From(new Vec2(1, 2), 0.9, true));
        AssertNear(4, list.Area);
    }

    static readonly CollisionGroup G = new(0);

    [Fact]
    public void SetAreaAddsCircles()
    {
        var circles = new[] { new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(5, 5), 2) };
        AssertNear(5 * Math.PI, new ColliderSet(G, 1, circles, new EdgeList[0]).Area);
    }

    [Fact]
    public void SetAreaAddsCirclesAndClosedListsAndCountsOverlapsTwice()
    {
        var circles = new[] { new Circle(new Vec2(0, 0), 1) }; // sits inside the square
        var set = new ColliderSet(G, 1, circles, new[] { new EdgeList(Square(new Vec2(0, 0)), true) });
        AssertNear(4 + Math.PI, set.Area);
    }

    [Fact]
    public void RingIsOuterLoopMinusHole()
    {
        var outer = new EdgeList(new IEdge[] { new Arc(Far, 2, new Vec2(1, 0), Math.PI) }, true);
        var hole = new EdgeList(new IEdge[] { new Arc(Far, 1, new Vec2(1, 0), -Math.PI) }, true);
        AssertNear(3 * Math.PI, new ColliderSet(G, 1, new Circle[0], new[] { outer, hole }).Area);
    }

    [Fact]
    public void SquareFrameIsOuterMinusHole()
    {
        var outer = new IEdge[]
        {
            new Segment(new Vec2(-2, -2), new Vec2(2, -2)),
            new Segment(new Vec2(2, -2), new Vec2(2, 2)),
            new Segment(new Vec2(2, 2), new Vec2(-2, 2)),
            new Segment(new Vec2(-2, 2), new Vec2(-2, -2)),
        };
        var set = new ColliderSet(G, 1, new Circle[0],
            new[] { new EdgeList(outer, true), new EdgeList(Reversed(Square(new Vec2(0, 0))), true) });
        AssertNear(16 - 4, set.Area);
    }

    [Fact]
    public void OpenPlatformAddsNothingToTheSet()
    {
        var platform = new EdgeList(new IEdge[] { new Segment(new Vec2(3, 2), new Vec2(8, 2)) }, false);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { new EdgeList(Square(new Vec2(0, 0)), true), platform });
        AssertNear(4, set.Area);
    }
}
