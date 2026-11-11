// AI-GEN AI-013: tests for Christopher's solid sides (Segment, Arc, Curve: Normal / NormalAt, IsOnSolidSide, Curve.ClosestT and TangentAt), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. The rule: stand at Start facing End, the solid is on your right,
// and the normal points the other way (out into the open side).
public class SolidSideTests
{
    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 1e-9 && Math.Abs(expected.Y - actual.Y) < 1e-9,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    // ---------- Segment ----------

    // A floor drawn left to right: solid below (bigger Y), open above.
    static readonly Segment Floor = new(new Vec2(0, 0), new Vec2(10, 0));

    // Start below End (the wall from the chat): heading up, so the solid is to the right.
    static readonly Segment Wall = new(new Vec2(0, 10), new Vec2(0, 0));

    [Fact]
    public void SegmentNormalPointsToTheOpenSide()
    {
        AssertNear(new Vec2(0, -1), Floor.Normal);  // up
        AssertNear(new Vec2(-1, 0), Wall.Normal);   // left
        AssertNear(new Vec2(0, 1), new Segment(new Vec2(10, 0), new Vec2(0, 0)).Normal); // reversed floor: a ceiling
    }

    [Fact]
    public void SegmentNormalIsUnitLength()
    {
        var diagonal = new Segment(new Vec2(1, 1), new Vec2(4, 5));
        Assert.Equal(1, diagonal.Normal.Length, 1e-9);
    }

    [Fact]
    public void ZeroLengthSegmentHasZeroNormal()
    {
        AssertNear(Vec2.Zero, new Segment(new Vec2(3, 3), new Vec2(3, 3)).Normal);
    }

    [Fact]
    public void SegmentSolidSide()
    {
        Assert.True(Floor.IsOnSolidSide(new Vec2(5, 3)));    // below the floor
        Assert.False(Floor.IsOnSolidSide(new Vec2(5, -3)));  // above it
        Assert.False(Floor.IsOnSolidSide(new Vec2(5, 0)));   // exactly on the line: touching, not solid
        Assert.True(Floor.IsOnSolidSide(new Vec2(20, 3)));   // past the end: the whole line counts

        Assert.True(Wall.IsOnSolidSide(new Vec2(3, 5)));     // right of the wall
        Assert.False(Wall.IsOnSolidSide(new Vec2(-3, 5)));   // left of it
    }

    [Fact]
    public void SegmentBothSolid()
    {
        var both = new Segment(new Vec2(0, 0), new Vec2(10, 0), BothSolid: true);
        Assert.True(both.IsOnSolidSide(new Vec2(5, 3)));
        Assert.True(both.IsOnSolidSide(new Vec2(5, -3)));
        AssertNear(new Vec2(0, -1), both.Normal);  // still the right-hand rule's normal
    }

    // ---------- Arc ----------

    // Radius 2 around the origin, middle pointing up, 90 degrees each way: the top half.
    static readonly Arc Bump = new(new Vec2(0, 0), 2, new Vec2(0, -1), Math.PI / 2);   // solid inside
    static readonly Arc Bowl = new(new Vec2(0, 0), 2, new Vec2(0, -1), -Math.PI / 2);  // solid outside

    [Fact]
    public void ArcNormalAt()
    {
        AssertNear(new Vec2(0, -1), Bump.NormalAt(new Vec2(0, -2)));  // away from the center
        AssertNear(new Vec2(0, 1), Bowl.NormalAt(new Vec2(0, -2)));   // toward the center
        double h = Math.Sqrt(0.5);
        AssertNear(new Vec2(h, -h), Bump.NormalAt(new Vec2(2 * h, -2 * h)));  // unit length on a diagonal
    }

    [Fact]
    public void ArcSolidSide()
    {
        Assert.True(Bump.IsOnSolidSide(new Vec2(0, -1)));    // inside the circle
        Assert.False(Bump.IsOnSolidSide(new Vec2(0, -3)));   // outside
        Assert.True(Bump.IsOnSolidSide(new Vec2(0, 1)));     // inside, on the missing half: the whole circle counts
        Assert.False(Bump.IsOnSolidSide(new Vec2(0, -2)));   // exactly on the edge: touching, not solid

        Assert.False(Bowl.IsOnSolidSide(new Vec2(0, -1)));
        Assert.True(Bowl.IsOnSolidSide(new Vec2(0, -3)));
    }

    [Fact]
    public void ArcBothSolid()
    {
        var both = Bump with { BothSolid = true };
        Assert.True(both.IsOnSolidSide(new Vec2(0, -1)));
        Assert.True(both.IsOnSolidSide(new Vec2(0, -3)));
    }

    [Fact]
    public void ArcFollowsTheSameRuleAsSegments()
    {
        // The chord from Start to End faces the same way as the arc's middle,
        // so arcs and segments chained End to Start face consistently.
        AssertNear(Bump.NormalAt(new Vec2(0, -2)), new Segment(Bump.Start, Bump.End).Normal);
        AssertNear(Bowl.NormalAt(new Vec2(0, -2)), new Segment(Bowl.Start, Bowl.End).Normal);
    }

    // ---------- Curve ----------

    // x = 10t, y = 20t(1 - t): bulges down on screen, lowest point (5, 5).
    // Walking left to right, the solid is below; the hollow above is open.
    static readonly Curve Hump = new(new Vec2(0, 0), new Vec2(5, 10), new Vec2(10, 0));
    static readonly Curve Straight = new(new Vec2(0, 0), new Vec2(5, 0), new Vec2(10, 0));

    [Fact]
    public void CurveTangentAt()
    {
        AssertNear(new Vec2(10, 20), Hump.TangentAt(0));
        AssertNear(new Vec2(10, 0), Hump.TangentAt(0.5));
        AssertNear(new Vec2(10, -20), Hump.TangentAt(1));
    }

    [Fact]
    public void CurveTangentFallsBackWhenControlSitsOnAnEnd()
    {
        var c = new Curve(new Vec2(0, 0), new Vec2(0, 0), new Vec2(10, 0));
        AssertNear(new Vec2(10, 0), c.TangentAt(0));
        AssertNear(new Vec2(0, -1), c.NormalAt(0));
    }

    [Fact]
    public void CurveNormalAt()
    {
        AssertNear(new Vec2(0, -1), Hump.NormalAt(0.5));
        AssertNear(new Vec2(2, -1) / Math.Sqrt(5), Hump.NormalAt(0));
        AssertNear(new Vec2(-2, -1) / Math.Sqrt(5), Hump.NormalAt(1));
        AssertNear(Straight.NormalAt(0.3), new Segment(Straight.Start, Straight.End).Normal);
    }

    [Fact]
    public void CurveClosestT()
    {
        Assert.Equal(0.5, Hump.ClosestT(new Vec2(5, 8)), 1e-9);    // straight below the lowest point
        Assert.Equal(0.3, Straight.ClosestT(new Vec2(3, 7)), 1e-9);
        Assert.Equal(0, Straight.ClosestT(new Vec2(-4, 2)), 1e-9);  // before Start: clamped
        Assert.Equal(1, Straight.ClosestT(new Vec2(14, 2)), 1e-9);  // past End: clamped
        AssertNear(Hump.PointAt(Hump.ClosestT(new Vec2(1, 7))), Hump.ClosestPoint(new Vec2(1, 7)));
    }

    [Fact]
    public void CurveSolidSide()
    {
        Assert.True(Hump.IsOnSolidSide(new Vec2(5, 6)));    // just below the lowest point
        Assert.False(Hump.IsOnSolidSide(new Vec2(5, 4)));   // just above it, in the hollow
        Assert.False(Hump.IsOnSolidSide(new Vec2(5, 0)));   // deep in the hollow (closest point is off to one side)
        Assert.True(Hump.IsOnSolidSide(new Vec2(2, 6)));    // below the left part
        Assert.False(Hump.IsOnSolidSide(Hump.PointAt(0.3))); // on the curve: touching, not solid

        Assert.True(Straight.IsOnSolidSide(new Vec2(5, 3)));
        Assert.False(Straight.IsOnSolidSide(new Vec2(5, -3)));
    }

    [Fact]
    public void CurveBendDoesNotChangeTheSide()
    {
        // Same Start and End (heading up, like Wall), bulging either way: solid on the right both times.
        var bulgeLeft = new Curve(new Vec2(0, 10), new Vec2(-5, 5), new Vec2(0, 0));
        var bulgeRight = new Curve(new Vec2(0, 10), new Vec2(5, 5), new Vec2(0, 0));
        Assert.True(bulgeLeft.IsOnSolidSide(new Vec2(1, 5)));
        Assert.False(bulgeLeft.IsOnSolidSide(new Vec2(-4, 5)));
        Assert.True(bulgeRight.IsOnSolidSide(new Vec2(4, 5)));
        Assert.False(bulgeRight.IsOnSolidSide(new Vec2(-1, 5)));
    }

    [Fact]
    public void CurveBothSolid()
    {
        var both = Hump with { BothSolid = true };
        Assert.True(both.IsOnSolidSide(new Vec2(5, 6)));
        Assert.True(both.IsOnSolidSide(new Vec2(5, 4)));
    }
}
