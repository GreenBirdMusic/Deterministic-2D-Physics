// AI-GEN AI-013: tests for Christopher's swept boxes and every-pair broad phase in PhysicsWorld.Step, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class BroadPhaseTests
{
    static void AssertBox(double minX, double minY, double maxX, double maxY, AlignedBoundingBox b)
    {
        Assert.True(Math.Abs(b.Min.X - minX) < 1e-9 && Math.Abs(b.Min.Y - minY) < 1e-9 &&
                    Math.Abs(b.Max.X - maxX) < 1e-9 && Math.Abs(b.Max.Y - maxY) < 1e-9,
            $"expected ({minX}, {minY}) - ({maxX}, {maxY}) but got ({b.Min.X}, {b.Min.Y}) - ({b.Max.X}, {b.Max.Y})");
    }

    static readonly CollisionGroup Solid = new(0);
    static readonly CollisionGroup Sensor = new(1);
    static readonly CollisionGroup Ghost = new(2);

    // Solid/Solid solid, Sensor/Solid overlap, Ghost ignores everything (and Sensor/Sensor is Ignore).
    static PhysicsWorld World(double tps = 10, Vec2 gravity = default)
    {
        var t = new GroupTable(new[] { "Solid", "Sensor", "Ghost" });
        t.Set(Solid, Solid, CollisionResponse.Solid);
        t.Set(Sensor, Solid, CollisionResponse.Overlap);
        return new PhysicsWorld(t, tps, gravity);
    }

    static ColliderSet Disc(CollisionGroup g, double r = 1, Vec2 center = default, bool active = true) =>
        new(g, 1, new[] { new Circle(center, r) }, new EdgeList[0], null, active);

    static PhysicsObject Ball(Vec2 p, Vec2 v = default, MovementKind kind = MovementKind.Dynamic, CollisionGroup? g = null, double r = 1) =>
        new(p, new[] { Disc(g ?? Solid, r) }, kind, true, v);

    static EdgeList Wall(double x, double y0, double y1) =>
        new(new IEdge[] { new Segment(new Vec2(x, y0), new Vec2(x, y1), BothSolid: true) }, false);

    static bool Paired(PhysicsWorld w, ColliderSet a, ColliderSet b) =>
        w.PotentialCollisions.Any(p => (p.A == a && p.B == b) || (p.A == b && p.B == a));

    // ---------- Swept boxes ----------

    [Fact]
    public void SlidingBoxCoversStartAndEnd()
    {
        var w = World(10);
        var o = Ball(new Vec2(0, 0), new Vec2(30, -10)); // moves (3, -1) this step
        w.Add(o);
        w.Step();
        AssertBox(-1, -2, 4, 1, o.ColliderSets[0].SweptBounds);
    }

    [Fact]
    public void SweptBoxUsesTheVelocityAfterGravity()
    {
        var w = World(10, new Vec2(0, 20));
        var o = Ball(new Vec2(0, 0)); // at rest; gravity makes v = (0, 2), so it moves (0, 0.2)
        w.Add(o);
        w.Step();
        AssertBox(-1, -1, 1, 1.2, o.ColliderSets[0].SweptBounds);
    }

    [Fact]
    public void SweptBoxIsWhereTheStepStarts()
    {
        // Moved by hand after Add: the step's shapes and box start from the new spot.
        var w = World(10);
        var o = Ball(new Vec2(0, 0), new Vec2(10, 0));
        w.Add(o);
        o.Position = new Vec2(5, 5);
        w.Step();
        AssertBox(4, 4, 7, 6, o.ColliderSets[0].SweptBounds);
    }

    [Fact]
    public void RotatingUsesReachSquaresAroundPosition()
    {
        // Circle offset (2, 0) from the center, radius 1: reach 3. Tight box would be (4..6, -1..1).
        var w = World(10);
        var o = new PhysicsObject(new Vec2(3, 0), new[] { Disc(Solid, 1, new Vec2(2, 0)) }, MovementKind.Dynamic, true,
            velocity: new Vec2(10, 0), angularVelocity: 1);
        w.Add(o);
        w.Step();
        AssertBox(0, -3, 7, 3, o.ColliderSets[0].SweptBounds);
    }

    [Fact]
    public void RotatingAnchoredUsesReachToo()
    {
        var w = World(10);
        var o = new PhysicsObject(new Vec2(10, 10), new[] { Disc(Solid, 0.5, new Vec2(0, 2)) }, MovementKind.Anchored, true,
            angularVelocity: -2);
        w.Add(o);
        w.Step();
        AssertBox(7.5, 7.5, 12.5, 12.5, o.ColliderSets[0].SweptBounds);
    }

    [Fact]
    public void StaticSweptBoxIsTheTightBox()
    {
        var w = World(10);
        var o = Ball(new Vec2(4, 4), new Vec2(100, 0), MovementKind.Static); // velocity ignored
        w.Add(o);
        w.Step();
        AssertBox(3, 3, 5, 5, o.ColliderSets[0].SweptBounds);
    }

    [Fact]
    public void InactiveSetsAreLeftOut()
    {
        var w = World();
        var on = Disc(Solid);
        var off = Disc(Sensor, 1, default, active: false);
        var o = new PhysicsObject(new Vec2(0, 0), new[] { on, off }, MovementKind.Dynamic, true);
        w.Add(o);
        w.Add(Ball(new Vec2(0, 0), kind: MovementKind.Static));
        w.Step();
        Assert.Contains(on, w.ActiveSets);
        Assert.DoesNotContain(off, w.ActiveSets);
        Assert.Single(w.PotentialCollisions);
    }

    // ---------- Pairs ----------

    [Fact]
    public void OverlappingBoxesPair()
    {
        var w = World();
        var a = Ball(new Vec2(0, 0));
        var b = Ball(new Vec2(1.5, 0));
        w.Add(a);
        w.Add(b);
        w.Step();
        var pair = Assert.Single(w.PotentialCollisions);
        Assert.Equal(CollisionResponse.Solid, pair.Response);
        Assert.True(Paired(w, a.ColliderSets[0], b.ColliderSets[0]));
    }

    [Fact]
    public void SeparateBoxesDontPair()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0)));
        w.Add(Ball(new Vec2(2.5, 0)));
        w.Step();
        Assert.Empty(w.PotentialCollisions);
    }

    [Fact]
    public void TouchingBoxesPair()
    {
        // Boxes (-1..1) and (1..3) share an edge: touching counts.
        var w = World();
        w.Add(Ball(new Vec2(0, 0)));
        w.Add(Ball(new Vec2(2, 0)));
        w.Step();
        Assert.Single(w.PotentialCollisions);
    }

    [Fact]
    public void FastObjectStillPairsWithAThinWall()
    {
        // Starts left of the wall, ends right of it: neither end box touches the wall,
        // but the swept box does. This is what stops tunneling at any speed.
        var w = World(10);
        var bullet = Ball(new Vec2(-5, 0), new Vec2(100, 0), r: 0.1); // moves 10 this step
        var wall = new PhysicsObject(new Vec2(0, 0), new[] { new ColliderSet(Solid, 1, new Circle[0], new[] { Wall(0, -2, 2) }) },
            MovementKind.Static, false);
        w.Add(bullet);
        w.Add(wall);
        w.Step();
        Assert.Single(w.PotentialCollisions);
        Assert.True(bullet.Position.X > 0); // and no solver yet, so it went through
    }

    [Fact]
    public void EachPairOnce()
    {
        var w = World();
        for (int i = 0; i < 4; i++) { w.Add(Ball(new Vec2(i * 0.1, 0))); }
        w.Step();
        Assert.Equal(6, w.PotentialCollisions.Count); // 4 choose 2
        foreach (var p in w.PotentialCollisions) { Assert.NotSame(p.A, p.B); }
        var keys = w.PotentialCollisions.Select(p => (p.A.GetHashCode(), p.B.GetHashCode())).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void StaticVsStaticNeverPairs()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0), kind: MovementKind.Static));
        w.Add(Ball(new Vec2(0.5, 0), kind: MovementKind.Static));
        w.Step();
        Assert.Empty(w.PotentialCollisions);
    }

    [Fact]
    public void StaticPairsWithMovingObjectsWhicheverComesFirst()
    {
        var w = World();
        w.Add(Ball(new Vec2(0, 0), kind: MovementKind.Static));
        w.Add(Ball(new Vec2(0.5, 0)));
        w.Add(Ball(new Vec2(1, 0), kind: MovementKind.Anchored));
        w.Add(Ball(new Vec2(1.5, 0), kind: MovementKind.Static));
        w.Step();
        // static-dynamic, static-anchored (x2), dynamic-anchored, dynamic-static; not static-static.
        Assert.Equal(5, w.PotentialCollisions.Count);
    }

    [Fact]
    public void SetsOfTheSameObjectDontPair()
    {
        var w = World();
        var o = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Solid), Disc(Sensor) }, MovementKind.Dynamic, true);
        w.Add(o);
        w.Step();
        Assert.Empty(w.PotentialCollisions);
    }

    [Fact]
    public void IgnoredGroupsDontPairAndOthersKeepTheirResponse()
    {
        var w = World();
        var body = Ball(new Vec2(0, 0));
        var sensor = Ball(new Vec2(0.5, 0), g: Sensor);
        var ghost = Ball(new Vec2(-0.5, 0), g: Ghost);
        w.Add(body);
        w.Add(sensor);
        w.Add(ghost);
        w.Step();
        var pair = Assert.Single(w.PotentialCollisions);
        Assert.Equal(CollisionResponse.Overlap, pair.Response);
        Assert.True(Paired(w, body.ColliderSets[0], sensor.ColliderSets[0]));
    }

    [Fact]
    public void PairsAreRebuiltEveryStep()
    {
        var w = World(10);
        var a = Ball(new Vec2(0, 0), new Vec2(-50, 0)); // flies apart: 5 units per step each way
        var b = Ball(new Vec2(1.5, 0), new Vec2(50, 0));
        w.Add(a);
        w.Add(b);
        w.Step();
        Assert.Single(w.PotentialCollisions);
        w.Step();
        Assert.Empty(w.PotentialCollisions);
        Assert.Equal(2, w.ActiveSets.Count); // cleared and refilled, not growing
    }

    [Fact]
    public void RemovedObjectsAreLeftOut()
    {
        var w = World();
        var a = Ball(new Vec2(0, 0));
        var b = Ball(new Vec2(0.5, 0));
        w.Add(a);
        w.Add(b);
        w.Remove(b);
        w.Step();
        Assert.Empty(w.PotentialCollisions);
        Assert.Single(w.ActiveSets);
    }

    [Fact]
    public void PairOrderIsTheSameEveryRun()
    {
        static List<(double, double)> Run()
        {
            var w = World();
            for (int i = 0; i < 5; i++) { w.Add(Ball(new Vec2(i * 0.3, i * 0.1))); }
            w.Step();
            return w.PotentialCollisions.Select(p => (p.A.Bounds.Min.X, p.B.Bounds.Min.X)).ToList();
        }
        Assert.Equal(Run(), Run());
    }
}
