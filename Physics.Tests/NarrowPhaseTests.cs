// AI-GEN AI-013: tests for Christopher's narrow phase, exact path (phase 4 of PhysicsWorld.Step, ExactCollisions), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class NarrowPhaseTests
{
    static readonly CollisionGroup Solid = new(0);
    static readonly CollisionGroup Sensor = new(1);

    static PhysicsWorld World(double tps = 10, Vec2 gravity = default)
    {
        var t = new GroupTable(new[] { "Solid", "Sensor" });
        t.Set(Solid, Solid, CollisionResponse.Solid);
        t.Set(Sensor, Solid, CollisionResponse.Overlap);
        return new PhysicsWorld(t, tps, gravity);
    }

    static ColliderSet Discs(CollisionGroup g, double r, params Vec2[] centers) =>
        new(g, 1, centers.Select(c => new Circle(c, r)).ToArray(), new EdgeList[0]);

    static ColliderSet Edges(CollisionGroup g, params EdgeList[] lists) => new(g, 1, new Circle[0], lists);

    static PhysicsObject Ball(Vec2 p, Vec2 v = default, MovementKind kind = MovementKind.Dynamic, double r = 0.5,
        CollisionGroup? g = null, double spin = 0) =>
        new(p, new[] { Discs(g ?? Solid, r, new Vec2(0, 0)) }, kind, true, v, angularVelocity: spin);

    // A box centered on (0, 0), clockwise on screen (y down), so solid inside.
    static EdgeList Box(double hw, double hh) => new(new IEdge[] {
        new Segment(new Vec2(-hw, -hh), new Vec2(hw, -hh)),
        new Segment(new Vec2(hw, -hh), new Vec2(hw, hh)),
        new Segment(new Vec2(hw, hh), new Vec2(-hw, hh)),
        new Segment(new Vec2(-hw, hh), new Vec2(-hw, -hh)) }, true);

    // Left to right along y = 1 (relative to the object): solid below it (y down), open above.
    static EdgeList Floor(double halfWidth = 5) =>
        new(new IEdge[] { new Segment(new Vec2(-halfWidth, 0), new Vec2(halfWidth, 0)) }, false);

    static PhysicsObject StaticFloor(double y = 1) =>
        new(new Vec2(0, y), new[] { Edges(Solid, Floor()) }, MovementKind.Static, false);

    static void Near(double expected, double actual) =>
        Assert.True(Math.Abs(expected - actual) < 1e-9, $"expected {expected} but got {actual}");

    static void NearVec(double x, double y, Vec2 v) =>
        Assert.True(Math.Abs(v.X - x) < 1e-9 && Math.Abs(v.Y - y) < 1e-9, $"expected ({x}, {y}) but got ({v.X}, {v.Y})");

    // ---------- Circles ----------

    [Fact]
    public void BallLandsOnAFloor()
    {
        // dt = 0.1: moves (0, 1); bottom at 0.5 reaches the floor at y = 1 halfway.
        var w = World();
        var ball = Ball(new Vec2(0, 0), new Vec2(0, 10));
        w.Add(ball);
        w.Add(StaticFloor());
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Assert.Same(ball.ColliderSets[0], e.A);
        Near(0.5, e.T);
        var h = Assert.Single(e.Hits);
        Near(0.5, h.T);
        NearVec(0, 1, h.Point);
        NearVec(0, -1, h.Normal); // faces A, the ball
        Assert.Equal(CollisionResponse.Solid, e.Response);
    }

    [Fact]
    public void FloorFirstFlipsTheNormalToFaceIt()
    {
        // Same landing with the floor added first: the floor is A, so the normal faces it.
        var w = World();
        var floor = StaticFloor();
        w.Add(floor);
        w.Add(Ball(new Vec2(0, 0), new Vec2(0, 10)));
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Assert.Same(floor.ColliderSets[0], e.A);
        var h = Assert.Single(e.Hits);
        Near(0.5, h.T);
        NearVec(0, 1, h.Point);
        NearVec(0, 1, h.Normal);
    }

    [Fact]
    public void UsesWorldCirclesNotLocalOnes()
    {
        // Ball far from the origin: with local circles (at 0, 0) it would never reach this floor.
        var w = World();
        w.Add(Ball(new Vec2(20, 30), new Vec2(0, 10)));
        w.Add(new PhysicsObject(new Vec2(20, 31), new[] { Edges(Solid, Floor()) }, MovementKind.Static, false));
        w.Step();
        var h = Assert.Single(Assert.Single(w.ExactCollisions).Hits);
        NearVec(20, 31, h.Point);
    }

    [Fact]
    public void TwoMovingBallsMeetHeadOn()
    {
        // Relative move (2, 0), gap 1: meet at 0.5 with A at (0.5, 0) and B at (2.5, 0).
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(10, 0), r: 1));
        w.Add(Ball(new Vec2(3, 0), new Vec2(-10, 0), r: 1));
        w.Step();
        var h = Assert.Single(Assert.Single(w.ExactCollisions).Hits);
        Near(0.5, h.T);
        NearVec(1.5, 0, h.Point);
        NearVec(-1, 0, h.Normal);
    }

    [Fact]
    public void TwoCirclesLandingTogetherGiveTwoContacts()
    {
        var w = World();
        w.Add(new PhysicsObject(new Vec2(0, 0), new[] { Discs(Solid, 0.5, new Vec2(-1, 0), new Vec2(1, 0)) },
            MovementKind.Dynamic, true, new Vec2(0, 10)));
        w.Add(StaticFloor());
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Assert.Equal(2, e.Hits.Length);
        Assert.All(e.Hits, h => { Near(0.5, h.T); NearVec(0, -1, h.Normal); });
        Assert.Contains(e.Hits, h => Math.Abs(h.Point.X + 1) < 1e-9);
        Assert.Contains(e.Hits, h => Math.Abs(h.Point.X - 1) < 1e-9);
    }

    [Fact]
    public void OnlyTheEarliestShapeCounts()
    {
        // The lower circle lands at 0.2, the upper one would at 0.6: only the first is kept.
        var w = World();
        w.Add(new PhysicsObject(new Vec2(0, 0), new[] { Discs(Solid, 0.5, new Vec2(-1, 0.3), new Vec2(1, -0.1)) },
            MovementKind.Dynamic, true, new Vec2(0, 10)));
        w.Add(StaticFloor());
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Near(0.2, e.T);
        var h = Assert.Single(e.Hits);
        NearVec(-1, 1, h.Point);
    }

    [Fact]
    public void LaterShapeFoundFirstIsReplaced()
    {
        // Same as above with the circles in the other order (the late one is checked first).
        var w = World();
        w.Add(new PhysicsObject(new Vec2(0, 0), new[] { Discs(Solid, 0.5, new Vec2(1, -0.1), new Vec2(-1, 0.3)) },
            MovementKind.Dynamic, true, new Vec2(0, 10)));
        w.Add(StaticFloor());
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Near(0.2, e.T);
        NearVec(-1, 1, Assert.Single(e.Hits).Point);
    }

    [Fact]
    public void ComingUpThroughAOneSidedFloorIsNoHit()
    {
        // Floor's solid side is below it; a ball rising from below passes through.
        var w = World();
        w.Add(Ball(new Vec2(0, 2), new Vec2(0, -10)));
        w.Add(StaticFloor());
        w.Step();
        Assert.Single(w.FilteredCollisions);
        Assert.Empty(w.ExactCollisions);
    }

    [Fact]
    public void BoxesOverlapButCirclesDontTouch()
    {
        // Diagonal neighbours at rest: the boxes overlap (mid phase keeps it), the circles are 2.69 apart.
        var w = World();
        w.Add(Ball(new Vec2(0, 0), r: 1));
        w.Add(Ball(new Vec2(1.9, 1.9), kind: MovementKind.Static, r: 1));
        w.Step();
        Assert.Single(w.FilteredCollisions);
        Assert.Empty(w.ExactCollisions);
    }

    // ---------- Edge lists ----------

    [Fact]
    public void BoxLandsOnABox()
    {
        // Box half-size 0.5 falling (0, 1) onto a wide static box whose top is at y = 1.
        var w = World();
        w.Add(new PhysicsObject(new Vec2(0, 0), new[] { Edges(Solid, Box(0.5, 0.5)) }, MovementKind.Dynamic, false,
            new Vec2(0, 10)));
        w.Add(new PhysicsObject(new Vec2(0, 1.5), new[] { Edges(Solid, Box(2, 0.5)) }, MovementKind.Static, false));
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Near(0.5, e.T);
        Assert.All(e.Hits, h => { Near(0.5, h.T); NearVec(0, -1, h.Normal); });
        Assert.Contains(e.Hits, h => Math.Abs(h.Point.X + 0.5) < 1e-9 && Math.Abs(h.Point.Y - 1) < 1e-9);
        Assert.Contains(e.Hits, h => Math.Abs(h.Point.X - 0.5) < 1e-9 && Math.Abs(h.Point.Y - 1) < 1e-9);
    }

    [Fact]
    public void BallOnABoxWithTheBoxFirst()
    {
        // A static box (A, edges) and a ball falling onto it (B, circle): the swapped CircleLoop call.
        var w = World();
        var box = new PhysicsObject(new Vec2(0, 1.5), new[] { Edges(Solid, Box(2, 0.5)) }, MovementKind.Static, false);
        w.Add(box);
        w.Add(Ball(new Vec2(0, 0), new Vec2(0, 10)));
        w.Step();
        var e = Assert.Single(w.ExactCollisions);
        Assert.Same(box.ColliderSets[0], e.A);
        var h = Assert.Single(e.Hits);
        Near(0.5, h.T);
        NearVec(0, 1, h.Point);
        NearVec(0, 1, h.Normal); // faces A, the box
    }

    [Fact]
    public void SwappedCallKeepsThePointWhereTheContactIs()
    {
        // Both moving: the edges (A) rise (0, -1) while the ball (B) falls (0, 1). Top at 1, bottom
        // at 0.5: they meet at T = 0.25 at y = 0.75. The point must not be shifted again.
        var w = World();
        w.Add(new PhysicsObject(new Vec2(0, 1.5), new[] { Edges(Solid, Box(2, 0.5)) }, MovementKind.Anchored, false,
            new Vec2(0, -10)));
        w.Add(Ball(new Vec2(0, 0), new Vec2(0, 10)));
        w.Step();
        var h = Assert.Single(Assert.Single(w.ExactCollisions).Hits);
        Near(0.25, h.T);
        NearVec(0, 0.75, h.Point);
        NearVec(0, 1, h.Normal);
    }

    // ---------- Pairs and the list ----------

    [Fact]
    public void ResponseIsCarriedOver()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(0, 10), g: Sensor));
        w.Add(StaticFloor());
        w.Step();
        Assert.Equal(CollisionResponse.Overlap, Assert.Single(w.ExactCollisions).Response);
    }

    [Fact]
    public void RotatingPairsStillGetHitsForNow()
    {
        // Temporary: rotating objects go through the exact path at their start angle.
        var w = World();
        w.Add(new PhysicsObject(new Vec2(0, 0), new[] { Edges(Solid, Box(0.5, 0.5)) }, MovementKind.Dynamic, true,
            new Vec2(0, 10), angularVelocity: 0.001));
        w.Add(StaticFloor());
        w.Step();
        Near(0.5, Assert.Single(w.ExactCollisions).T);
    }

    [Fact]
    public void ListIsClearedEachStep()
    {
        // Resting on the floor (touching), no solver yet: one pair each step, never piling up.
        var w = World();
        w.Add(Ball(new Vec2(0, 0.5)));
        w.Add(StaticFloor());
        w.Step();
        w.Step();
        Assert.True(w.ExactCollisions.Count <= 1);
    }

    [Fact]
    public void DroppedByTheMidPhaseNeverReachesIt()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(30, 0), r: 1));
        w.Add(Ball(new Vec2(2.5, 0), new Vec2(30, 0), r: 1));
        w.Step();
        Assert.Empty(w.FilteredCollisions);
        Assert.Empty(w.ExactCollisions);
    }
}
