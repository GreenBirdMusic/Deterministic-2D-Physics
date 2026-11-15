// AI-GEN AI-013: tests for Christopher's mid phase (AlignedBoundingBox.FindOverlapTime and phase 3 of PhysicsWorld.Step), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class MidPhaseTests
{
    static AlignedBoundingBox Box(double minX, double minY, double maxX, double maxY) =>
        new(new Vec2(minX, minY), new Vec2(maxX, maxY));

    static void AssertWindow(double tEnter, double tExit, (double, double)? got)
    {
        Assert.True(got.HasValue, $"expected ({tEnter}, {tExit}) but got no hit");
        var (e, x) = got.Value;
        Assert.True(Math.Abs(e - tEnter) < 1e-9 && Math.Abs(x - tExit) < 1e-9,
            $"expected ({tEnter}, {tExit}) but got ({e}, {x})");
    }

    // ---------- FindOverlapTime on its own ----------

    [Fact]
    public void HeadOnAlongX()
    {
        // A's right side reaches B's left side halfway; A is still inside B's x range at the end.
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(3, 0, 4, 1), new Vec2(4, 0)));
    }

    [Fact]
    public void MovingTheNegativeWayGivesTheSameWindow()
    {
        // Mirror of HeadOnAlongX: the swap puts the times in order.
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(3, 0, 4, 1), Box(0, 0, 1, 1), new Vec2(-4, 0)));
    }

    [Fact]
    public void TouchingExactlyAtTheEndCounts()
    {
        AssertWindow(1, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(3, 0, 4, 1), new Vec2(2, 0)));
    }

    [Fact]
    public void FallingShortIsNoHit()
    {
        // Would meet at t = 2, next step.
        Assert.Null(AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(3, 0, 4, 1), new Vec2(1, 0)));
    }

    [Fact]
    public void MovingApartIsNoHit()
    {
        // The window is [-4, -2]: they "met" in the past.
        Assert.Null(AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(3, 0, 4, 1), new Vec2(-1, 0)));
    }

    [Fact]
    public void OverlappingAndMovingApartStartsAtZero()
    {
        // Window [-3, 1] clamps to [0, 1]; at t = 1 they exactly touch.
        AssertWindow(0, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 2, 1), Box(1, 0, 3, 1), new Vec2(-1, 0)));
    }

    [Fact]
    public void OverlappingAndStillIsTheWholeStep()
    {
        AssertWindow(0, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 2, 2), Box(1, 1, 3, 3), new Vec2(0, 0)));
    }

    [Fact]
    public void SeparateAndStillIsNoHit()
    {
        Assert.Null(AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(2, 0, 3, 1), new Vec2(0, 0)));
    }

    [Fact]
    public void ExitIsClampedToTheStep()
    {
        // Long B: A would stay inside until t = 10.
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(1.5, 0, 10, 1), new Vec2(1, 0)));
    }

    [Fact]
    public void NoXMotionAndSeparateOnXIsNoHit()
    {
        // Moving straight down past B, beside it: y alone would say hit.
        Assert.Null(AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(2, 3, 3, 4), new Vec2(0, 10)));
    }

    [Fact]
    public void NoXMotionAndOverlappingOnXLetsYDecide()
    {
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(0.5, 3, 1.5, 4), new Vec2(0, 4)));
    }

    [Fact]
    public void NoXMotionAndTouchingOnXCounts()
    {
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(1, 3, 2, 4), new Vec2(0, 4)));
    }

    [Fact]
    public void NoYMotionAndSeparateOnYIsNoHit()
    {
        Assert.Null(AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(3, 2, 4, 3), new Vec2(10, 0)));
    }

    [Fact]
    public void DiagonalHit()
    {
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(2, 2, 3, 3), new Vec2(2, 2)));
    }

    [Fact]
    public void LaterAxisDecidesTheEnter()
    {
        // A starts above-left of B: x lines up at 0.25, but y only at 0.5.
        AssertWindow(0.5, 1, AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(2, 3, 4, 4), new Vec2(4, 4)));
    }

    [Fact]
    public void PassingOverTheCornerIsNoHit()
    {
        // The hexagon's cut corner: x window [0.1, 0.3] is over before y's [0.67, 1.33] starts,
        // even though the start + end box (0..11, 0..4) covers B.
        Assert.Null(AlignedBoundingBox.FindOverlapTime(Box(0, 0, 1, 1), Box(2, 3, 3, 4), new Vec2(10, 3)));
    }

    // ---------- Phase 3 in Step() ----------

    static readonly CollisionGroup Solid = new(0);
    static readonly CollisionGroup Sensor = new(1);

    static PhysicsWorld World(double tps = 10, Vec2 gravity = default)
    {
        var t = new GroupTable(new[] { "Solid", "Sensor" });
        t.Set(Solid, Solid, CollisionResponse.Solid);
        t.Set(Sensor, Solid, CollisionResponse.Overlap);
        return new PhysicsWorld(t, tps, gravity);
    }

    static ColliderSet Disc(CollisionGroup g, double r = 1, Vec2 center = default) =>
        new(g, 1, new[] { new Circle(center, r) }, new EdgeList[0]);

    static PhysicsObject Ball(Vec2 p, Vec2 v = default, MovementKind kind = MovementKind.Dynamic, CollisionGroup? g = null, double r = 1) =>
        new(p, new[] { Disc(g ?? Solid, r) }, kind, true, v);

    static void AssertPair(double tEnter, double tExit, FCollisionPair p) =>
        AssertWindow(tEnter, tExit, (p.TEnter, p.TExit));

    [Fact]
    public void ApproachingPairIsKeptWithItsWindow()
    {
        // dt = 0.1: relative move (2, 0); boxes -1..1 and 2..4 meet at 0.5.
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(10, 0)));
        w.Add(Ball(new Vec2(3, 0), new Vec2(-10, 0)));
        w.Step();
        Assert.Single(w.PotentialCollisions);
        var p = Assert.Single(w.FilteredCollisions);
        AssertPair(0.5, 1, p);
        Assert.Equal(CollisionResponse.Solid, p.Response);
    }

    [Fact]
    public void PairOrderMatchesTheBroadPhase()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(10, 0)));
        w.Add(Ball(new Vec2(3, 0), new Vec2(-10, 0)));
        w.Step();
        var broad = Assert.Single(w.PotentialCollisions);
        var mid = Assert.Single(w.FilteredCollisions);
        Assert.Same(broad.A, mid.A);
        Assert.Same(broad.B, mid.B);
    }

    [Fact]
    public void MovingTogetherSideBySideIsDropped()
    {
        // Swept boxes overlap (both slide right by 3), but they never get closer.
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(30, 0)));
        w.Add(Ball(new Vec2(2.5, 0), new Vec2(30, 0)));
        w.Step();
        Assert.Single(w.PotentialCollisions);
        Assert.Empty(w.FilteredCollisions);
    }

    [Fact]
    public void PassingOverTheCornerIsDropped()
    {
        // Moves (10, 3) past a static ball at (2.5, 3.5): the broad phase pairs them, the mid phase doesn't.
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(100, 30), r: 0.5));
        w.Add(Ball(new Vec2(2.5, 3.5), kind: MovementKind.Static, r: 0.5));
        w.Step();
        Assert.Single(w.PotentialCollisions);
        Assert.Empty(w.FilteredCollisions);
    }

    [Fact]
    public void StaticVelocityIsIgnored()
    {
        // Static ball at 2.5 (box 1.5..3.5) given a velocity it never moves by. Only the dynamic
        // ball's move of (1, 0) counts: meets at 0.5. Counting the static velocity gives about 0.045.
        var w = World();
        w.Add(Ball(new Vec2(0, 0), new Vec2(10, 0)));
        w.Add(Ball(new Vec2(2.5, 0), new Vec2(-100, 0), MovementKind.Static));
        w.Step();
        AssertPair(0.5, 1, Assert.Single(w.FilteredCollisions));
    }

    [Fact]
    public void UsesTheVelocityAfterGravity()
    {
        // At rest, gravity (0, 100) at dt = 0.1 makes it move (0, 1); floor box 1.5..2.5 is met at 0.5.
        var w = World(10, new Vec2(0, 100));
        w.Add(Ball(new Vec2(0, 0)));
        w.Add(Ball(new Vec2(0, 2), kind: MovementKind.Static, r: 0.5));
        w.Step();
        AssertPair(0.5, 1, Assert.Single(w.FilteredCollisions));
    }

    [Fact]
    public void RotatingUsesTheReachSquare()
    {
        // Spinning in place: circle offset (2, 0), radius 1, reach 3. Its tight box (4..6) misses the
        // static ball at (1, 0), but the circle can swing round to it: the reach square (0..6) covers it.
        var w = World();
        w.Add(new PhysicsObject(new Vec2(3, 0), new[] { Disc(Solid, 1, new Vec2(2, 0)) }, MovementKind.Dynamic, true,
            angularVelocity: 1));
        w.Add(Ball(new Vec2(1, 0), kind: MovementKind.Static, r: 0.5));
        w.Step();
        AssertPair(0, 1, Assert.Single(w.FilteredCollisions));
    }

    [Fact]
    public void ResponseIsCarriedOver()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0), g: Sensor));
        w.Add(Ball(new Vec2(1, 0), kind: MovementKind.Static));
        w.Step();
        Assert.Equal(CollisionResponse.Overlap, Assert.Single(w.FilteredCollisions).Response);
    }

    [Fact]
    public void ListIsClearedEachStep()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0)));
        w.Add(Ball(new Vec2(1, 0), kind: MovementKind.Static));
        w.Step();
        w.Step();
        Assert.Single(w.FilteredCollisions);
    }

    [Fact]
    public void DroppedByBroadPhaseNeverReachesMidPhase()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0)));
        w.Add(Ball(new Vec2(10, 0), kind: MovementKind.Static));
        w.Step();
        Assert.Empty(w.PotentialCollisions);
        Assert.Empty(w.FilteredCollisions);
    }
}
