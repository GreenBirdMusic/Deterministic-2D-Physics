// AI-GEN AI-013: tests for Christopher's PhysicsWorld (and GroupTable.IsSolid), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class PhysicsWorldTests
{
    static void AssertNear(double expected, double actual, double tol = 1e-9)
    {
        double scale = Math.Max(1, Math.Abs(expected));
        Assert.True(Math.Abs(expected - actual) < tol * scale, $"expected {expected} but got {actual}");
    }

    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True((expected - actual).Length < 1e-9, $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    // Groups: Body is solid with Body, Sensor only overlaps Body, Ghost ignores everything.
    static GroupTable Table()
    {
        var t = new GroupTable(new[] { "Body", "Sensor", "Ghost" });
        t.Set(Body, Body, CollisionResponse.Solid);
        t.Set(Body, Sensor, CollisionResponse.Overlap);
        return t;
    }

    static readonly CollisionGroup Body = new(0);
    static readonly CollisionGroup Sensor = new(1);
    static readonly CollisionGroup Ghost = new(2);

    static readonly Vec2 Down = new(0, 10);

    static PhysicsWorld World(double tps = 60) => new(Table(), tps, Down);

    static ColliderSet Disc(CollisionGroup g, double density = 1, bool active = true) =>
        new(g, density, new[] { new Circle(new Vec2(0, 0), 1) }, new EdgeList[0], null, active);

    static PhysicsObject Obj(MovementKind kind = MovementKind.Dynamic, double density = 1, CollisionGroup? group = null, Vec2 velocity = default) =>
        new(new Vec2(0, 0), new[] { Disc(group ?? Body, density) }, kind, true, velocity);

    // ---------- GroupTable.IsSolid ----------

    [Fact]
    public void IsSolidLooksAtTheWholeRow()
    {
        var t = Table();
        Assert.True(t.IsSolid(Body));
        Assert.False(t.IsSolid(Sensor)); // overlap only
        Assert.False(t.IsSolid(Ghost));
        t.Set(Ghost, Sensor, CollisionResponse.Soft);
        Assert.True(t.IsSolid(Ghost)); // soft counts as solid
        Assert.True(t.IsSolid(Sensor));
    }

    // ---------- Add / Remove (outside a step) ----------

    [Fact]
    public void AddPutsTheObjectInTheWorld()
    {
        var w = World();
        var o = Obj();
        w.Add(o);
        Assert.Same(w, o.World);
        Assert.False(o.IsRemoved);
        Assert.Contains(o, w.Objects);
    }

    [Fact]
    public void AddingTwiceThrows()
    {
        var w = World();
        var o = Obj();
        w.Add(o);
        Assert.Throws<ArgumentException>(() => w.Add(o));
        Assert.Single(w.Objects);
    }

    [Fact]
    public void AddingToASecondWorldThrows()
    {
        var a = World();
        var b = World();
        var o = Obj();
        a.Add(o);
        Assert.Throws<ArgumentException>(() => b.Add(o));
        Assert.Same(a, o.World);
        Assert.Empty(b.Objects);
    }

    [Fact]
    public void RemoveTakesItOutAndClearsTheWorld()
    {
        var w = World();
        var o = Obj();
        w.Add(o);
        w.Remove(o);
        Assert.Null(o.World);
        Assert.True(o.IsRemoved);
        Assert.DoesNotContain(o, w.Objects);
    }

    [Fact]
    public void RemovingFromTheWrongWorldThrows()
    {
        var a = World();
        var b = World();
        var o = Obj();
        Assert.Throws<ArgumentException>(() => a.Remove(o)); // in no world
        b.Add(o);
        Assert.Throws<ArgumentException>(() => a.Remove(o));
        Assert.Same(b, o.World);
    }

    [Fact]
    public void RemovedObjectsCanBeReAdded()
    {
        var w = World();
        var o = Obj();
        w.Add(o);
        w.Remove(o);
        w.Add(o);
        Assert.Same(w, o.World);
        Assert.False(o.IsRemoved);
        Assert.Single(w.Objects);
    }

    [Fact]
    public void RemovedObjectsCanMoveToAnotherWorld()
    {
        var a = World();
        var b = World();
        var o = Obj();
        a.Add(o);
        a.Remove(o);
        b.Add(o);
        Assert.Same(b, o.World);
        Assert.Empty(a.Objects);
        Assert.Single(b.Objects);
    }

    // ---------- Solid needs mass ----------

    [Fact]
    public void MasslessSolidDynamicThrowsOnAdd()
    {
        var w = World();
        var o = Obj(density: 0);
        Assert.Throws<ArgumentException>(() => w.Add(o));
        Assert.Null(o.World);
        Assert.Empty(w.Objects);
    }

    [Fact]
    public void MasslessIsFineWhenNotSolidOrNotDynamic()
    {
        var w = World();
        w.Add(Obj(density: 0, group: Sensor)); // overlap only
        w.Add(Obj(density: 0, group: Ghost)); // ignores everything
        w.Add(Obj(MovementKind.Static, density: 0));
        w.Add(Obj(MovementKind.Anchored, density: 0));
        Assert.Equal(4, w.Objects.Count);
    }

    [Fact]
    public void MassOverrideSatisfiesTheCheck()
    {
        var w = World();
        var o = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 0) }, MovementKind.Dynamic, true, massOverride: 2);
        w.Add(o);
        Assert.Same(w, o.World);
    }

    [Fact]
    public void InactiveSolidSetsCount()
    {
        // The active set is a massless sensor; the inactive one is solid. Swapping later
        // would make a massless solid object, so it's caught when added.
        var w = World();
        var o = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Sensor, 0), Disc(Body, 1, active: false) }, MovementKind.Dynamic, true);
        Assert.Throws<ArgumentException>(() => w.Add(o));
    }

    [Fact]
    public void DensityDroppingToZeroInAWorldThrows()
    {
        var w = World();
        var o = Obj();
        w.Add(o);
        Assert.Throws<ArgumentException>(() => o.ColliderSets[0].Density = 0);
    }

    [Fact]
    public void BecomingDynamicWithoutMassThrows()
    {
        var w = World();
        var o = Obj(MovementKind.Static, density: 0);
        w.Add(o);
        Assert.Throws<ArgumentException>(() => o.MovementType = MovementKind.Dynamic);
    }

    [Fact]
    public void MassChangesOutsideAWorldDontThrow()
    {
        var w = World();
        var o = Obj();
        o.ColliderSets[0].Density = 0; // not in a world yet: fine
        w.Add(Obj());
        o.ColliderSets[0].Density = 1;
        w.Add(o);
        w.Remove(o);
        o.ColliderSets[0].Density = 0; // removed: fine again
    }

    // ---------- Step: movement ----------

    [Fact]
    public void TickCountCounts()
    {
        var w = World();
        Assert.Equal(0, w.TickCount);
        w.Step();
        w.Step();
        w.Step();
        Assert.Equal(3, w.TickCount);
    }

    [Fact]
    public void OneStepIsSemiImplicitEuler()
    {
        // Velocity first, then position with the new velocity.
        var w = World(60);
        var o = Obj();
        w.Add(o);
        w.Step();
        double dt = 1.0 / 60;
        AssertNear(Down * dt, o.Velocity);
        AssertNear(Down * dt * dt, o.Position);
    }

    [Fact]
    public void FallingMatchesTheClosedForm()
    {
        // After n steps: v = g n dt, p = g dt^2 (1 + 2 + ... + n) = g dt^2 n (n + 1) / 2.
        var w = World(50);
        var o = Obj(velocity: new Vec2(3, -2));
        w.Add(o);
        int n = 120;
        for (int i = 0; i < n; i++) { w.Step(); }
        double dt = 1.0 / 50;
        AssertNear(new Vec2(3, -2) + Down * (n * dt), o.Velocity);
        AssertNear(new Vec2(3, -2) * (n * dt) + Down * (dt * dt * n * (n + 1) / 2), o.Position);
    }

    [Fact]
    public void WorldGravityCanChange()
    {
        var w = World(10);
        var o = Obj();
        w.Add(o);
        w.Gravity = new Vec2(-5, 0);
        w.Step();
        AssertNear(new Vec2(-0.5, 0), o.Velocity);
    }

    [Fact]
    public void GravityScaleAndOverride()
    {
        var w = World(10);
        var scaled = Obj();
        scaled.GravityScale = 2;
        var flipped = Obj();
        flipped.GravityOverride = new Vec2(0, -10);
        var both = Obj();
        both.GravityOverride = new Vec2(4, 0);
        both.GravityScale = 0.5;
        var floaty = Obj();
        floaty.GravityScale = 0;
        foreach (var o in new[] { scaled, flipped, both, floaty }) { w.Add(o); }
        w.Step();
        AssertNear(new Vec2(0, 2), scaled.Velocity);
        AssertNear(new Vec2(0, -1), flipped.Velocity);
        AssertNear(new Vec2(0.2, 0), both.Velocity); // scale applies on top of the override
        AssertNear(new Vec2(0, 0), floaty.Velocity);
    }

    [Fact]
    public void AnchoredMovesByItsVelocityWithoutGravity()
    {
        var w = World(10);
        var o = Obj(MovementKind.Anchored, velocity: new Vec2(5, 0));
        o.AngularVelocity = 1;
        w.Add(o);
        w.Step();
        AssertNear(new Vec2(5, 0), o.Velocity);
        AssertNear(new Vec2(0.5, 0), o.Position);
        AssertNear(0.1, o.Angle);
    }

    [Fact]
    public void StaticNeverMoves()
    {
        var w = World(10);
        var o = Obj(MovementKind.Static, velocity: new Vec2(5, 0));
        o.AngularVelocity = 1;
        w.Add(o);
        w.Step();
        AssertNear(new Vec2(0, 0), o.Position);
        AssertNear(new Vec2(5, 0), o.Velocity); // left alone, just not used
        AssertNear(0, o.Angle);
    }

    [Fact]
    public void AngleAdvancesAndWraps()
    {
        var w = World(10);
        var o = Obj();
        o.AngularVelocity = 3 * Math.PI; // 0.3 pi per step
        w.Add(o);
        for (int i = 0; i < 4; i++) { w.Step(); } // 1.2 pi, wrapped to -0.8 pi
        AssertNear(-0.8 * Math.PI, o.Angle);
    }

    [Fact]
    public void RemovedObjectsStopMoving()
    {
        var w = World(10);
        var o = Obj();
        w.Add(o);
        w.Step();
        Vec2 p = o.Position;
        w.Remove(o);
        w.Step();
        AssertNear(p, o.Position);
    }

    [Fact]
    public void AddUpdatesWorldShapes()
    {
        // Static objects never update in Step, so Add has to place their world shapes.
        var w = World();
        var o = new PhysicsObject(new Vec2(5, 3), new[] { Disc(Body) }, MovementKind.Static, false);
        w.Add(o);
        AssertNear(new Vec2(5, 3), o.ColliderSets[0].WorldCircles[0].Center);
    }

    [Fact]
    public void StepUpdatesWorldShapesBeforeMoving()
    {
        // World shapes are where the object starts the step (what the moving checks use).
        var w = World(10);
        var o = Obj(velocity: new Vec2(10, 0));
        w.Add(o);
        o.Position = new Vec2(2, 0); // moved by hand after Add
        w.Step();
        AssertNear(new Vec2(2, 0), o.ColliderSets[0].WorldCircles[0].Center);
        AssertNear(new Vec2(3, 0.1), o.Position);
    }

    [Fact]
    public void EmptyWorldSteps()
    {
        var w = World();
        w.Step();
        Assert.Equal(1, w.TickCount);
    }
}
