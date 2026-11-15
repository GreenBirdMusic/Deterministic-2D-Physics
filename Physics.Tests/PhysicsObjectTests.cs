// AI-GEN AI-013: tests for Christopher's PhysicsObject (and the ColliderSet Owner / Active parts), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class PhysicsObjectTests
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

    static readonly CollisionGroup Body = new(0);
    static readonly CollisionGroup Hitbox = new(1);

    // A set holding one circle: area pi r^2, J = pi r^4 / 2 + pi r^2 |c|^2, moment = area * c.
    static ColliderSet Disc(CollisionGroup g, double density, Vec2 c, double r, bool active = true) =>
        new(g, density, new[] { new Circle(c, r) }, new EdgeList[0], null, active);

    static double DiscJ(Vec2 c, double r) => Math.PI * (Math.Pow(r, 4) / 2 + r * r * c.LengthSquared);

    static PhysicsObject Make(params ColliderSet[] sets) =>
        new(new Vec2(0, 0), sets, MovementKind.Dynamic, true);

    // ---------- Constructor: owners and groups ----------

    [Fact]
    public void SetCanOnlyBelongToOneObject()
    {
        var s = Disc(Body, 1, new Vec2(0, 0), 1);
        Make(s);
        Assert.Throws<ArgumentException>(() => Make(s));
    }

    [Fact]
    public void FailedConstructorDoesNotClaimTheOtherSets()
    {
        var owned = Disc(Hitbox, 1, new Vec2(0, 0), 1);
        Make(owned);
        var free = Disc(Body, 1, new Vec2(0, 0), 1);
        Assert.Throws<ArgumentException>(() => Make(free, owned));
        Make(free); // still usable
    }

    [Fact]
    public void TwoActiveSetsInOneGroupThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Make(Disc(Body, 1, new Vec2(0, 0), 1), Disc(Body, 1, new Vec2(3, 0), 1)));
    }

    [Fact]
    public void ActivePlusInactiveInOneGroupIsFine()
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1), Disc(Body, 1, new Vec2(3, 0), 1, false));
        Assert.Equal(2, obj.ColliderSets.Count);
    }

    [Fact]
    public void ConstructorCopiesTheSetArray()
    {
        var sets = new[] { Disc(Body, 1, new Vec2(0, 0), 1) };
        var obj = Make(sets);
        sets[0] = Disc(Hitbox, 1, new Vec2(0, 0), 1);
        Assert.Equal(Body, obj.ColliderSets[0].Group);
    }

    // ---------- Mass, center, spin resistance ----------

    [Fact]
    public void MassCenterAndSpinFromDensities()
    {
        var a = new Vec2(-2, 1); var b = new Vec2(3, 0.5);
        var obj = Make(Disc(Body, 2, a, 1), Disc(Hitbox, 0.5, b, 2));
        double ma = 2 * Math.PI, mb = 0.5 * 4 * Math.PI;
        AssertNear(ma + mb, obj.Mass);
        AssertNear((a * ma + b * mb) / (ma + mb), obj.Center);
        AssertNear(2 * DiscJ(a, 1) + 0.5 * DiscJ(b, 2), obj.RotationalInertia);
        AssertNear(1 / (ma + mb), obj.InverseMass);
        AssertNear(1 / obj.RotationalInertia, obj.InverseJ);
    }

    [Fact]
    public void ZeroDensitySetsAddNothing()
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1), Disc(Hitbox, 0, new Vec2(50, 0), 5));
        AssertNear(Math.PI, obj.Mass);
        AssertNear(new Vec2(0, 0), obj.Center);
    }

    [Fact]
    public void InactiveSetsDoNotCount()
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1), Disc(Hitbox, 1, new Vec2(5, 0), 2, false));
        AssertNear(Math.PI, obj.Mass);
    }

    [Fact]
    public void NoMassMeansCenterZeroAndNoInverses()
    {
        var obj = Make(Disc(Hitbox, 0, new Vec2(4, 4), 1));
        Assert.Equal(0, obj.Mass);
        AssertNear(new Vec2(0, 0), obj.Center);
        Assert.Equal(0, obj.InverseMass);
        Assert.Equal(0, obj.InverseJ);
    }

    // ---------- Movement kind and rotatable ----------

    [Fact]
    public void StaticAndAnchoredCannotBePushedButKeepTheirMass()
    {
        foreach (var kind in new[] { MovementKind.Static, MovementKind.Anchored })
        {
            var obj = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 1, new Vec2(1, 0), 1) }, kind, true);
            AssertNear(Math.PI, obj.Mass);
            Assert.Equal(0, obj.InverseMass);
            Assert.Equal(0, obj.InverseJ);
        }
    }

    [Fact]
    public void NonRotatableCannotBeSpun()
    {
        var obj = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 1, new Vec2(1, 0), 1) }, MovementKind.Dynamic, false);
        AssertNear(1 / Math.PI, obj.InverseMass);
        Assert.Equal(0, obj.InverseJ);
        Assert.True(obj.RotationalInertia > 0);
    }

    [Fact]
    public void ChangingMovementKindAndRotatableRecalculates()
    {
        var obj = Make(Disc(Body, 1, new Vec2(1, 0), 1));
        obj.MovementType = MovementKind.Static;
        Assert.Equal(0, obj.InverseMass);
        obj.MovementType = MovementKind.Dynamic;
        AssertNear(1 / Math.PI, obj.InverseMass);
        obj.Rotatable = false;
        Assert.Equal(0, obj.InverseJ);
        obj.Rotatable = true;
        Assert.True(obj.InverseJ > 0);
    }

    // ---------- Mass override ----------

    [Fact]
    public void OverrideSetsMassAndScalesSpin()
    {
        var c = new Vec2(2, 0);
        var obj = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 3, c, 1) }, MovementKind.Dynamic, true, massOverride: 10);
        AssertNear(10, obj.Mass);
        AssertNear(DiscJ(c, 1) * 3 * 10 / (3 * Math.PI), obj.RotationalInertia);
        AssertNear(c, obj.Center);
        AssertNear(0.1, obj.InverseMass);
    }

    [Fact]
    public void OverrideKeepsTheDensityRatios()
    {
        // Doubling one density shifts the center and spin even with the total mass overridden.
        var a = new Vec2(-1, 0); var b = new Vec2(1, 0);
        var obj = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 1, a, 1), Disc(Hitbox, 3, b, 1) },
            MovementKind.Dynamic, true, massOverride: 8);
        AssertNear(8, obj.Mass);
        AssertNear(new Vec2(0.5, 0), obj.Center); // (1 * -1 + 3 * 1) / 4
    }

    [Fact]
    public void OverrideWithNoDensityUsesTheShape()
    {
        var a = new Vec2(-1, 0); var b = new Vec2(3, 0);
        var obj = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 0, a, 1), Disc(Hitbox, 0, b, 1) },
            MovementKind.Dynamic, true, massOverride: 5);
        AssertNear(5, obj.Mass);
        AssertNear(new Vec2(1, 0), obj.Center);
        AssertNear((DiscJ(a, 1) + DiscJ(b, 1)) * 5 / (2 * Math.PI), obj.RotationalInertia);
        AssertNear(0.2, obj.InverseMass);
    }

    [Fact]
    public void BadOverrideThrows()
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1));
        Assert.Throws<ArgumentException>(() => obj.MassOverride = 0);
        Assert.Throws<ArgumentException>(() => obj.MassOverride = -2);
        Assert.Throws<ArgumentException>(() =>
            new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 1, new Vec2(0, 0), 1) }, MovementKind.Dynamic, true, massOverride: -1));
    }

    [Fact]
    public void SettingAndClearingOverrideRecalculates()
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1));
        obj.MassOverride = 7;
        AssertNear(7, obj.Mass);
        obj.MassOverride = null;
        AssertNear(Math.PI, obj.Mass);
    }

    // ---------- Mass sets ----------

    [Fact]
    public void MassSetsCountEvenWhenInactive()
    {
        var stand = Disc(Body, 1, new Vec2(0, -1), 2);
        var crouch = Disc(Body, 1, new Vec2(0, 0), 1, false);
        var obj = Make(stand, crouch);
        obj.MassSets = new[] { stand };
        obj.Swap(stand, crouch, false);
        AssertNear(4 * Math.PI, obj.Mass);
        AssertNear(new Vec2(0, -1), obj.Center);
    }

    [Fact]
    public void ClearingMassSetsGoesBackToActiveSets()
    {
        var stand = Disc(Body, 1, new Vec2(0, 0), 2, false);
        var crouch = Disc(Body, 1, new Vec2(0, 0), 1);
        var obj = Make(stand, crouch);
        obj.MassSets = new[] { stand };
        AssertNear(4 * Math.PI, obj.Mass);
        obj.MassSets = null;
        AssertNear(Math.PI, obj.Mass);
    }

    [Fact]
    public void MassSetsFromAnotherObjectThrow()
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1));
        var other = Disc(Body, 1, new Vec2(0, 0), 1);
        Assert.Throws<ArgumentException>(() => obj.MassSets = new[] { other });
        Make(other);
        Assert.Throws<ArgumentException>(() => obj.MassSets = new[] { other });
    }

    [Fact]
    public void MassSetsInConstructor()
    {
        var stand = Disc(Body, 1, new Vec2(0, 0), 2, false);
        var crouch = Disc(Body, 1, new Vec2(0, 0), 1);
        var obj = new PhysicsObject(new Vec2(0, 0), new[] { stand, crouch }, MovementKind.Dynamic, true, massSets: new[] { stand });
        AssertNear(4 * Math.PI, obj.Mass);
    }

    // ---------- Changes through the sets ----------

    [Fact]
    public void ChangingADensityRecalculates()
    {
        var s = Disc(Body, 1, new Vec2(0, 0), 1);
        var obj = Make(s);
        s.Density = 3;
        AssertNear(3 * Math.PI, obj.Mass);
        AssertNear(1 / (3 * Math.PI), obj.InverseMass);
    }

    [Fact]
    public void TurningASetOffAndOnRecalculates()
    {
        var hit = Disc(Hitbox, 2, new Vec2(4, 0), 1);
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1), hit);
        AssertNear(3 * Math.PI, obj.Mass);
        hit.Active = false;
        AssertNear(Math.PI, obj.Mass);
        AssertNear(new Vec2(0, 0), obj.Center);
        hit.Active = true;
        AssertNear(3 * Math.PI, obj.Mass);
    }

    [Fact]
    public void TurningOnASecondSetInAGroupThrowsAndChangesNothing()
    {
        var second = Disc(Body, 1, new Vec2(3, 0), 1, false);
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1), second);
        Assert.Throws<ArgumentException>(() => second.Active = true);
        Assert.False(second.Active);
        AssertNear(Math.PI, obj.Mass);
    }

    [Fact]
    public void TurningASetOnRefreshesItsWorldShapes()
    {
        var hit = Disc(Hitbox, 0, new Vec2(1, 0), 0.5, false);
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1), hit);
        obj.Position = new Vec2(10, 5);
        obj.Angle = Math.PI / 2;
        obj.UpdateWorld();
        // Inactive: left alone by UpdateWorld.
        AssertNear(new Vec2(1, 0), hit.WorldCircles[0].Center);
        hit.Active = true;
        AssertNear(new Vec2(10, 6), hit.WorldCircles[0].Center);
    }

    [Fact]
    public void SetsWithoutAnOwnerCanStillBeToggled()
    {
        var s = Disc(Body, 1, new Vec2(0, 0), 1);
        s.Active = false;
        s.Active = true;
        Assert.True(s.Active);
    }

    // ---------- World update ----------

    [Fact]
    public void UpdateWorldUsesPositionAngleAndMirror()
    {
        var s = Disc(Body, 1, new Vec2(2, 1), 1);
        var obj = Make(s);
        obj.Position = new Vec2(5, -3);
        obj.Angle = 0.7;
        obj.Mirrored = true;
        obj.UpdateWorld();
        var t = Transform.From(new Vec2(5, -3), 0.7, true);
        AssertNear(t.Apply(new Vec2(2, 1)), s.WorldCircles[0].Center);
    }

    // ---------- Swap ----------

    static (PhysicsObject obj, ColliderSet stand, ColliderSet crouch) Crouchable(double standDensity = 1)
    {
        var stand = Disc(Body, standDensity, new Vec2(0, -1), 2);
        var crouch = Disc(Body, 1, new Vec2(0, 0), 1, false);
        return (Make(stand, crouch), stand, crouch);
    }

    [Fact]
    public void SwapTurnsOneOffAndTheOtherOn()
    {
        var (obj, stand, crouch) = Crouchable();
        Assert.True(obj.Swap(stand, crouch, false));
        Assert.False(stand.Active);
        Assert.True(crouch.Active);
        AssertNear(Math.PI, obj.Mass);
        AssertNear(new Vec2(0, 0), obj.Center);
    }

    [Fact]
    public void SwapAcceptsEitherOrderByDefault()
    {
        var (obj, stand, crouch) = Crouchable();
        obj.Swap(crouch, stand, false); // reversed: stand is the active one
        Assert.False(stand.Active);
        Assert.True(crouch.Active);
    }

    [Fact]
    public void SwapInStrictOrderThrowsWhenReversed()
    {
        var (obj, stand, crouch) = Crouchable();
        Assert.Throws<ArgumentException>(() => obj.Swap(crouch, stand, false, false));
        Assert.True(stand.Active);
    }

    [Fact]
    public void SwapNeedsOneActiveAndOneInactive()
    {
        var obj2 = Make(Disc(Body, 1, new Vec2(0, 0), 1, false), Disc(Body, 1, new Vec2(1, 0), 1, false));
        Assert.Throws<ArgumentException>(() => obj2.Swap(obj2.ColliderSets[0], obj2.ColliderSets[1], false));
    }

    [Fact]
    public void SwapNeedsTheSameGroup()
    {
        var body = Disc(Body, 1, new Vec2(0, 0), 1);
        var hit = Disc(Hitbox, 0, new Vec2(0, 0), 1, false);
        var obj = Make(body, hit);
        Assert.Throws<ArgumentException>(() => obj.Swap(body, hit, false));
        Assert.True(body.Active);
        Assert.False(hit.Active);
    }

    [Fact]
    public void SwapNeedsBothSetsToBeThisObjects()
    {
        var (obj, stand, _) = Crouchable();
        var (_, _, otherCrouch) = Crouchable();
        Assert.Throws<ArgumentException>(() => obj.Swap(stand, otherCrouch, false));
        var loose = Disc(Body, 1, new Vec2(0, 0), 1, false);
        Assert.Throws<ArgumentException>(() => obj.Swap(stand, loose, false));
        Assert.True(stand.Active);
    }

    [Fact]
    public void SwapRefreshesTheNewSetsWorldShapes()
    {
        var (obj, stand, crouch) = Crouchable();
        obj.Position = new Vec2(7, 7);
        obj.UpdateWorld();
        obj.Swap(stand, crouch, false);
        AssertNear(new Vec2(7, 7), crouch.WorldCircles[0].Center);
    }

    [Fact]
    public void SwapKeepingVelocityLeavesSpeedsAlone()
    {
        var (obj, stand, crouch) = Crouchable();
        obj.Velocity = new Vec2(3, -2);
        obj.AngularVelocity = 1.5;
        obj.Swap(stand, crouch, false);
        AssertNear(new Vec2(3, -2), obj.Velocity);
        AssertNear(1.5, obj.AngularVelocity);
    }

    [Fact]
    public void SwapKeepingMomentumScalesSpeeds()
    {
        var (obj, stand, crouch) = Crouchable();
        double oldMass = obj.Mass, oldJ = obj.RotationalInertia;
        obj.Velocity = new Vec2(3, -2);
        obj.AngularVelocity = 1.5;
        obj.Swap(stand, crouch, true);
        AssertNear(new Vec2(3, -2) * (oldMass / obj.Mass), obj.Velocity);
        AssertNear(1.5 * oldJ / obj.RotationalInertia, obj.AngularVelocity);
        AssertNear(oldMass * 3, obj.Mass * obj.Velocity.X); // momentum kept
    }

    [Fact]
    public void SwapKeepingMomentumIntoAMasslessSetLeavesSpeeds()
    {
        var stand = Disc(Body, 1, new Vec2(0, 0), 1);
        var ghost = Disc(Body, 0, new Vec2(0, 0), 1, false);
        var obj = Make(stand, ghost);
        obj.Velocity = new Vec2(2, 0);
        obj.AngularVelocity = 1;
        obj.Swap(stand, ghost, true);
        AssertNear(new Vec2(2, 0), obj.Velocity);
        AssertNear(1, obj.AngularVelocity);
        Assert.False(double.IsNaN(obj.Velocity.X) || double.IsInfinity(obj.Velocity.X));
    }

    [Fact]
    public void SwapWithLockedMassKeepsSpeedsEvenWithMomentum()
    {
        var (obj, stand, crouch) = Crouchable();
        obj.MassSets = new[] { stand };
        obj.Velocity = new Vec2(3, 0);
        obj.Swap(stand, crouch, true);
        AssertNear(new Vec2(3, 0), obj.Velocity);
    }

    // ---------- Angle ----------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-1, -1)]
    [InlineData(4, 4 - 2 * Math.PI)]
    [InlineData(-4, -4 + 2 * Math.PI)]
    [InlineData(7, 7 - 2 * Math.PI)]
    [InlineData(100, 100 - 32 * Math.PI)]
    public void AngleIsWrapped(double set, double expected)
    {
        var obj = Make(Disc(Body, 1, new Vec2(0, 0), 1));
        obj.Angle = set;
        AssertNear(expected, obj.Angle);
        Assert.True(obj.Angle >= -Math.PI && obj.Angle <= Math.PI);
    }

    [Fact]
    public void ConstructorWrapsTheAngle()
    {
        var obj = new PhysicsObject(new Vec2(0, 0), new[] { Disc(Body, 1, new Vec2(0, 0), 1) }, MovementKind.Dynamic, true, angle: 5);
        AssertNear(5 - 2 * Math.PI, obj.Angle);
    }
}
