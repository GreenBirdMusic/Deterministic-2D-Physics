// AI-GEN AI-013: tests for Christopher's Transform and the Transformed functions (Circle, Segment, Arc, Curve, IEdge), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down, so a positive angle turns +X toward +Y (clockwise on screen).
public class TransformTests
{
    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 1e-9 && Math.Abs(expected.Y - actual.Y) < 1e-9,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    static readonly Vec2 Pos = new(5, -3);
    static readonly Transform Move = Transform.From(Pos, 0, false);
    static readonly Transform Turn90 = Transform.From(Pos, Math.PI / 2, false);
    static readonly Transform Mirror = Transform.From(Pos, 0, true);
    static readonly Transform MirrorTurn90 = Transform.From(Pos, Math.PI / 2, true);
    static readonly Transform Odd = Transform.From(new Vec2(-2.5, 7), 0.7, true); // a messy one

    static readonly Transform[] All = { Move, Turn90, Mirror, MirrorTurn90, Odd,
                                        Transform.From(new Vec2(1, 1), -2.1, false) };

    // ---------- Transform ----------

    [Fact]
    public void FromStoresCosAndSin()
    {
        var t = Transform.From(Pos, 0.3, true);
        Assert.Equal(Math.Cos(0.3), t.Cos);
        Assert.Equal(Math.Sin(0.3), t.Sin);
        Assert.True(t.Mirrored);
        Assert.Equal(Pos, t.Position);
    }

    [Fact]
    public void AngleZeroJustMoves()
    {
        AssertNear(new Vec2(6, -1), Move.Apply(new Vec2(1, 2)));
    }

    [Fact]
    public void QuarterTurnTurnsXIntoY()
    {
        AssertNear(Pos + new Vec2(0, 1), Turn90.Apply(new Vec2(1, 0)));
        AssertNear(Pos + new Vec2(-1, 0), Turn90.Apply(new Vec2(0, 1)));
    }

    [Fact]
    public void MirrorFlipsX()
    {
        AssertNear(Pos + new Vec2(-1, 2), Mirror.Apply(new Vec2(1, 2)));
    }

    [Fact]
    public void MirrorHappensBeforeTheTurn()
    {
        // (1, 0) -> mirror -> (-1, 0) -> turn 90 -> (0, -1). The other order would give (0, 1).
        AssertNear(Pos + new Vec2(0, -1), MirrorTurn90.Apply(new Vec2(1, 0)));
    }

    [Fact]
    public void ApplyDirectionIsApplyWithoutTheMove()
    {
        var d = new Vec2(0.6, -0.8);
        foreach (var t in All)
            AssertNear(t.Apply(d) - t.Position, t.ApplyDirection(d));
    }

    [Fact]
    public void DistancesStayTheSame()
    {
        var a = new Vec2(1, 2);
        var b = new Vec2(-4, 0.5);
        foreach (var t in All)
            Assert.Equal((a - b).Length, (t.Apply(a) - t.Apply(b)).Length, 9);
    }

    // ---------- Circle ----------

    [Fact]
    public void CircleMovesItsCenterAndKeepsItsRadius()
    {
        var c = new Circle(new Vec2(1, 0), 2);
        var moved = c.Transformed(MirrorTurn90);
        AssertNear(MirrorTurn90.Apply(c.Center), moved.Center);
        Assert.Equal(2, moved.Radius);
    }

    // ---------- Segment ----------

    static readonly Segment Floor = new(new Vec2(0, 0), new Vec2(10, 0));

    [Fact]
    public void SegmentEndsMoveWithoutMirror()
    {
        var s = Floor.Transformed(Turn90);
        AssertNear(Turn90.Apply(Floor.Start), s.Start);
        AssertNear(Turn90.Apply(Floor.End), s.End);
    }

    [Fact]
    public void MirroredSegmentSwapsEnds()
    {
        var s = Floor.Transformed(Mirror);
        AssertNear(Mirror.Apply(Floor.End), s.Start);
        AssertNear(Mirror.Apply(Floor.Start), s.End);
    }

    [Fact]
    public void SegmentKeepsBothSolid()
    {
        Assert.True((Floor with { BothSolid = true }).Transformed(Mirror).BothSolid);
        Assert.False(Floor.Transformed(Mirror).BothSolid);
    }

    // The point of the swap: the solid side ends up on the same physical side.
    [Fact]
    public void SegmentSolidSideFollowsTheTransform()
    {
        var below = new Vec2(4, 3);   // solid side of the floor
        var above = new Vec2(4, -3);  // open side
        foreach (var t in All)
        {
            var s = Floor.Transformed(t);
            Assert.True(s.IsOnSolidSide(t.Apply(below)));
            Assert.False(s.IsOnSolidSide(t.Apply(above)));
            AssertNear(t.ApplyDirection(Floor.Normal), s.Normal);
        }
    }

    // ---------- Arc ----------

    // A bump: center (0, 0), radius 5, middle pointing up, a quarter circle each way.
    static readonly Arc Bump = new(new Vec2(0, 0), 5, new Vec2(0, -1), Math.PI / 4);
    static readonly Arc Bowl = new(new Vec2(0, 0), 5, new Vec2(0, 1), -Math.PI / 4);

    [Fact]
    public void ArcMovesWithoutMirror()
    {
        var a = Bump.Transformed(Turn90);
        AssertNear(Turn90.Apply(Bump.Center), a.Center);
        AssertNear(Turn90.Apply(Bump.Start), a.Start);
        AssertNear(Turn90.Apply(Bump.End), a.End);
        Assert.Equal(Bump.Radius, a.Radius);
        Assert.Equal(Bump.HalfAngle, a.HalfAngle);
    }

    // Arcs don't swap anything by hand: mirroring the Direction alone swaps Start and End.
    [Fact]
    public void MirroredArcSwapsEndsByItself()
    {
        var a = Bump.Transformed(Mirror);
        AssertNear(Mirror.Apply(Bump.End), a.Start);
        AssertNear(Mirror.Apply(Bump.Start), a.End);
        var orig = new Arc(new Vec2(0, 0), 5, new Vec2(1, 0), Math.PI / 6);
        var b = orig.Transformed(MirrorTurn90);
        AssertNear(MirrorTurn90.Apply(orig.End), b.Start);
        AssertNear(MirrorTurn90.Apply(orig.Start), b.End);
    }

    [Fact]
    public void ArcSolidSideFollowsTheTransform()
    {
        var outside = new Vec2(0.5, -7);  // above the bump: open
        var inside = new Vec2(0.5, -3);   // under the bump: solid
        foreach (var t in All)
        {
            var a = Bump.Transformed(t);
            Assert.True(a.IsOnSolidSide(t.Apply(inside)));
            Assert.False(a.IsOnSolidSide(t.Apply(outside)));
            AssertNear(t.ApplyDirection(Bump.NormalAt(outside)), a.NormalAt(t.Apply(outside)));

            var w = Bowl.Transformed(t);
            Assert.True(w.IsOnSolidSide(t.Apply(new Vec2(0.5, 7))));   // under the bowl's edge
            Assert.False(w.IsOnSolidSide(t.Apply(new Vec2(0.5, 3))));  // in the bowl
        }
    }

    // ---------- Curve ----------

    static readonly Curve Hill = new(new Vec2(0, 0), new Vec2(5, -6), new Vec2(10, 0));

    [Fact]
    public void CurvePointsMoveWithoutMirror()
    {
        var c = Hill.Transformed(Odd with { Mirrored = false });
        var t = Odd with { Mirrored = false };
        AssertNear(t.Apply(Hill.Start), c.Start);
        AssertNear(t.Apply(Hill.Control), c.Control);
        AssertNear(t.Apply(Hill.End), c.End);
        AssertNear(t.Apply(Hill.PointAt(0.3)), c.PointAt(0.3));
    }

    [Fact]
    public void MirroredCurveSwapsEnds()
    {
        var c = Hill.Transformed(Mirror);
        AssertNear(Mirror.Apply(Hill.End), c.Start);
        AssertNear(Mirror.Apply(Hill.Control), c.Control);
        AssertNear(Mirror.Apply(Hill.Start), c.End);
        AssertNear(Mirror.Apply(Hill.PointAt(0.3)), c.PointAt(0.7)); // runs the other way
        Assert.True((Hill with { BothSolid = true }).Transformed(Mirror).BothSolid);
    }

    [Fact]
    public void CurveSolidSideFollowsTheTransform()
    {
        var under = new Vec2(5, -1);  // under the hill: solid
        var over = new Vec2(5, -5);   // over it: open
        foreach (var t in All)
        {
            var c = Hill.Transformed(t);
            Assert.True(c.IsOnSolidSide(t.Apply(under)));
            Assert.False(c.IsOnSolidSide(t.Apply(over)));
            AssertNear(t.ApplyDirection(Hill.NormalAt(over)), c.NormalAt(t.Apply(over)));
        }
    }
}

// Calls through IEdge (explicit interface implementations) give the same edge as the direct calls.
public class TransformThroughIEdgeTests
{
    [Fact]
    public void IEdgeTransformedGivesTheSameEdge()
    {
        var t = Transform.From(new Vec2(1, 2), 0.4, true);
        IEdge[] edges =
        {
            new Segment(new Vec2(0, 0), new Vec2(10, 0)),
            new Arc(new Vec2(0, 0), 5, new Vec2(0, -1), Math.PI / 4),
            new Curve(new Vec2(0, 0), new Vec2(5, -6), new Vec2(10, 0)),
        };
        Assert.Equal(((Segment)edges[0]).Transformed(t), edges[0].Transformed(t));
        Assert.Equal(((Arc)edges[1]).Transformed(t), edges[1].Transformed(t));
        Assert.Equal(((Curve)edges[2]).Transformed(t), edges[2].Transformed(t));
    }
}
