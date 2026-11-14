// AI-GEN AI-013: tests for Christopher's EdgeList and ColliderSet, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class ColliderSetTests
{
    static void AssertNear(double expected, double actual)
    {
        Assert.True(Math.Abs(expected - actual) < 1e-9, $"expected {expected} but got {actual}");
    }

    // A clockwise 2x2 square around the origin (screen coordinates, solid inside).
    static IEdge[] Square() => new IEdge[]
    {
        new Segment(new Vec2(-1, -1), new Vec2(1, -1)),
        new Segment(new Vec2(1, -1), new Vec2(1, 1)),
        new Segment(new Vec2(1, 1), new Vec2(-1, 1)),
        new Segment(new Vec2(-1, 1), new Vec2(-1, -1)),
    };

    static readonly Transform Odd = Transform.From(new Vec2(-2.5, 7), 0.7, true);

    // ---------- EdgeList: constructor checks ----------

    [Fact]
    public void EmptyEdgeListThrows()
    {
        Assert.Throws<ArgumentException>(() => new EdgeList(new IEdge[0], false));
        Assert.Throws<ArgumentException>(() => new EdgeList(new IEdge[0], true));
    }

    [Fact]
    public void WrongTagCountThrows()
    {
        Assert.Throws<ArgumentException>(() => new EdgeList(Square(), true, new ulong[3]));
        Assert.Throws<ArgumentException>(() => new EdgeList(Square(), true, new ulong[5]));
    }

    [Fact]
    public void NoTagsGivesZeroTagPerEdge()
    {
        var list = new EdgeList(Square(), true);
        Assert.Equal(new ulong[4], list.Tags);
    }

    [Fact]
    public void ClosedSquareIsAccepted()
    {
        var list = new EdgeList(Square(), true);
        Assert.True(list.Closed);
    }

    [Fact]
    public void ClosedWithAGapThrows()
    {
        var edges = Square();
        edges[2] = new Segment(new Vec2(1, 1), new Vec2(-0.9, 1)); // stops short of the next Start
        Assert.Throws<ArgumentException>(() => new EdgeList(edges, true));
    }

    [Fact]
    public void ClosedLastEdgeMustMeetTheFirst()
    {
        var edges = Square();
        edges[3] = new Segment(new Vec2(-1, 1), new Vec2(-1, -0.9)); // only the wrap-around gap
        Assert.Throws<ArgumentException>(() => new EdgeList(edges, true));
    }

    [Fact]
    public void GapWithinEpsilonCountsAsClosed()
    {
        var edges = Square();
        edges[2] = new Segment(new Vec2(1, 1), new Vec2(-1, 1 + Vec2.Epsilon / 2));
        Assert.True(new EdgeList(edges, true).Closed);
    }

    [Fact]
    public void OpenListMayHaveGaps()
    {
        var edges = new IEdge[]
        {
            new Segment(new Vec2(0, 0), new Vec2(1, 0)),
            new Segment(new Vec2(5, 5), new Vec2(6, 5)),
        };
        Assert.False(new EdgeList(edges, false).Closed);
    }

    [Fact]
    public void ArcsAndCurvesCanCloseALoop()
    {
        // Top: segment (-1,0) -> (1,0). Bottom half: a half circle arc back to (-1,0)... built from its ends.
        var arc = new Arc(new Vec2(0, 0), 1, new Vec2(0, 1), Math.PI / 2);
        var curve = new Curve(arc.End, new Vec2(0, -2), arc.Start);
        var list = new EdgeList(new IEdge[] { arc, curve }, true);
        Assert.True(list.Closed);
    }

    // ---------- EdgeList: copies ----------

    [Fact]
    public void ChangingTheInputArraysLaterDoesNothing()
    {
        var edges = Square();
        var tags = new ulong[] { 1, 2, 3, 4 };
        var list = new EdgeList(edges, true, tags);

        edges[0] = new Segment(new Vec2(9, 9), new Vec2(10, 9));
        tags[0] = 99;

        Assert.Equal(new Vec2(-1, -1), list.LocalEdges[0].Start);
        Assert.Equal(new Vec2(-1, -1), list.WorldEdges[0].Start);
        Assert.Equal(1UL, list.Tags[0]);
    }

    [Fact]
    public void TagsCanBeChangedOnPurpose()
    {
        var list = new EdgeList(Square(), true);
        list.Tags[2] = 0b101;
        Assert.Equal(0b101UL, list.Tags[2]);
    }

    // ---------- EdgeList: world update ----------

    [Fact]
    public void WorldEdgesStartAsTheLocalEdges()
    {
        var list = new EdgeList(Square(), true);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(list.LocalEdges[i], list.WorldEdges[i]);
        }
    }

    [Fact]
    public void UpdateWorldMatchesEachEdgesTransformed()
    {
        var arc = new Arc(new Vec2(3, 0), 1, new Vec2(1, 0), 0.5);
        var curve = new Curve(new Vec2(0, 0), new Vec2(1, 2), new Vec2(2, 0));
        var seg = new Segment(new Vec2(-1, 4), new Vec2(2, 5), true);
        var local = new IEdge[] { arc, curve, seg };
        var list = new EdgeList(local, false);

        list.UpdateWorld(Odd);

        for (int i = 0; i < local.Length; i++)
        {
            Assert.Equal(local[i].Transformed(Odd), list.WorldEdges[i]);
        }
    }

    [Fact]
    public void UpdateWorldLeavesLocalEdgesAlone()
    {
        var list = new EdgeList(Square(), true);
        list.UpdateWorld(Odd);
        var fresh = Square();
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(fresh[i], list.LocalEdges[i]);
        }
    }

    [Fact]
    public void UpdateWorldIsNotCumulative()
    {
        // Each update starts from the local edges, so doing it twice is the same as once.
        var list = new EdgeList(Square(), true);
        list.UpdateWorld(Odd);
        list.UpdateWorld(Odd);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(Square()[i].Transformed(Odd), list.WorldEdges[i]);
        }
    }

    [Fact]
    public void MirroredLoopIsStillALoopForPointInLoop()
    {
        // Mirroring swaps each edge's ends but keeps the order, so the world loop runs backwards.
        // Point in loop doesn't care about order, so a point inside stays inside.
        var list = new EdgeList(Square(), true);
        var t = Transform.From(new Vec2(10, 0), 0.3, true);
        list.UpdateWorld(t);
        Assert.True(Overlap.PointInLoop(new Vec2(10, 0), list.WorldEdges));
        Assert.False(Overlap.PointInLoop(new Vec2(13, 0), list.WorldEdges));
    }

    // ---------- EdgeList: reach ----------

    [Fact]
    public void SegmentReachIsTheFartherEnd()
    {
        var list = new EdgeList(new IEdge[] { new Segment(new Vec2(1, 0), new Vec2(3, 4)) }, false);
        AssertNear(5, list.Reach);
    }

    [Fact]
    public void ArcReachIsCenterDistancePlusRadius()
    {
        var list = new EdgeList(new IEdge[] { new Arc(new Vec2(3, 4), 2, new Vec2(1, 0), 0.5) }, false);
        AssertNear(7, list.Reach);
    }

    [Fact]
    public void CurveReachIsTheFarthestOfItsThreePoints()
    {
        // The control point is the farthest one here.
        var list = new EdgeList(new IEdge[] { new Curve(new Vec2(1, 0), new Vec2(0, -6), new Vec2(-1, 0)) }, false);
        AssertNear(6, list.Reach);
    }

    [Fact]
    public void ReachIsTheLargestOverAllEdges()
    {
        AssertNear(Math.Sqrt(2), new EdgeList(Square(), true).Reach);
    }

    [Fact]
    public void ReachCoversTheWorldEdgesAfterAnyRotation()
    {
        // Reach is measured from the object's center, so after any transform every world point
        // is within Reach of the transform's Position.
        var curve = new Curve(new Vec2(1, 0), new Vec2(2, -3), new Vec2(-1, 1));
        var arc = new Arc(new Vec2(-2, 1), 1.5, new Vec2(0, 1), 1.2);
        var list = new EdgeList(new IEdge[] { new Segment(new Vec2(0, 2), new Vec2(3, 3)), curve, arc }, false);
        var t = Transform.From(new Vec2(4, -1), 2.2, true);
        list.UpdateWorld(t);

        var wc = (Curve)list.WorldEdges[1];
        for (int i = 0; i <= 20; i++)
        {
            Assert.True((wc.PointAt(i / 20.0) - t.Position).Length <= list.Reach + 1e-9);
        }
        var wa = (Arc)list.WorldEdges[2];
        Assert.True((wa.Center - t.Position).Length + wa.Radius <= list.Reach + 1e-9);
    }

    // ---------- ColliderSet: constructor checks ----------

    static readonly CollisionGroup G = new(3);
    static Circle[] TwoCircles() => new[] { new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(4, 3), 2) };

    [Fact]
    public void NegativeDensityThrows()
    {
        Assert.Throws<ArgumentException>(() => new ColliderSet(G, -0.1, TwoCircles(), new EdgeList[0]));
    }

    [Fact]
    public void SettingNegativeDensityLaterThrowsAndKeepsTheOldValue()
    {
        var set = new ColliderSet(G, 1.5, TwoCircles(), new EdgeList[0]);
        Assert.Throws<ArgumentException>(() => set.Density = -1);
        Assert.Equal(1.5, set.Density);
        set.Density = 0;
        Assert.Equal(0, set.Density);
    }

    [Fact]
    public void ZeroDensityIsFine()
    {
        var set = new ColliderSet(G, 0, TwoCircles(), new EdgeList[0]);
        Assert.Equal(0, set.Density);
    }

    [Fact]
    public void EmptySetThrows()
    {
        Assert.Throws<ArgumentException>(() => new ColliderSet(G, 1, new Circle[0], new EdgeList[0]));
    }

    [Fact]
    public void EdgeListsAloneAreEnough()
    {
        var set = new ColliderSet(G, 1, new Circle[0], new[] { new EdgeList(Square(), true) });
        Assert.Single(set.EdgeLists);
    }

    [Fact]
    public void WrongCircleTagCountThrows()
    {
        Assert.Throws<ArgumentException>(() => new ColliderSet(G, 1, TwoCircles(), new EdgeList[0], new ulong[1]));
    }

    [Fact]
    public void NoCircleTagsGivesZeroTagPerCircle()
    {
        var set = new ColliderSet(G, 1, TwoCircles(), new EdgeList[0]);
        Assert.Equal(new ulong[2], set.CircleTags);
    }

    [Fact]
    public void StoresGroupAndDensity()
    {
        var set = new ColliderSet(G, 2.5, TwoCircles(), new EdgeList[0]);
        Assert.Equal(G, set.Group);
        Assert.Equal(2.5, set.Density);
    }

    [Fact]
    public void ChangingTheInputCircleArraysLaterDoesNothing()
    {
        var circles = TwoCircles();
        var tags = new ulong[] { 7, 8 };
        var set = new ColliderSet(G, 1, circles, new EdgeList[0], tags);

        circles[0] = new Circle(new Vec2(50, 50), 9);
        tags[0] = 0;

        Assert.Equal(new Circle(new Vec2(0, 0), 1), set.Circles[0]);
        Assert.Equal(new Circle(new Vec2(0, 0), 1), set.WorldCircles[0]);
        Assert.Equal(7UL, set.CircleTags[0]);
    }

    // ---------- ColliderSet: reach ----------

    [Fact]
    public void ReachFromCircles()
    {
        // Second circle: |(4, 3)| + 2 = 7.
        AssertNear(7, new ColliderSet(G, 1, TwoCircles(), new EdgeList[0]).Reach);
    }

    [Fact]
    public void ReachIsTheLargestOfCirclesAndEdgeLists()
    {
        var far = new EdgeList(new IEdge[] { new Segment(new Vec2(0, 0), new Vec2(6, 8)) }, false);
        var near = new EdgeList(Square(), true);
        AssertNear(10, new ColliderSet(G, 1, TwoCircles(), new[] { near, far }).Reach);
        AssertNear(7, new ColliderSet(G, 1, TwoCircles(), new[] { near }).Reach);
    }

    // ---------- ColliderSet: world update ----------

    [Fact]
    public void WorldCirclesStartAsTheLocalCircles()
    {
        var set = new ColliderSet(G, 1, TwoCircles(), new EdgeList[0]);
        Assert.Equal(set.Circles, set.WorldCircles);
    }

    [Fact]
    public void UpdateWorldMovesCirclesAndEdgeLists()
    {
        var list = new EdgeList(Square(), true);
        var set = new ColliderSet(G, 1, TwoCircles(), new[] { list });

        set.UpdateWorld(Odd);

        var local = TwoCircles();
        for (int i = 0; i < local.Length; i++)
        {
            Assert.Equal(local[i].Transformed(Odd), set.WorldCircles[i]);
            Assert.Equal(local[i], set.Circles[i]);
        }
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(Square()[i].Transformed(Odd), set.EdgeLists[0].WorldEdges[i]);
        }
    }

    [Fact]
    public void UpdateWorldTwiceIsTheSameAsOnce()
    {
        var set = new ColliderSet(G, 1, TwoCircles(), new EdgeList[0]);
        set.UpdateWorld(Odd);
        set.UpdateWorld(Odd);
        Assert.Equal(TwoCircles()[1].Transformed(Odd), set.WorldCircles[1]);
    }
}
