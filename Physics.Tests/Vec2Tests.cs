// AI-GEN AI-013: tests for Christopher's Vec2, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class Vec2Tests
{
    // Doubles can be off in the last digits, so most checks allow a tiny difference.
    const double Tolerance = 1e-12;

    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < Tolerance && Math.Abs(expected.Y - actual.Y) < Tolerance,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    [Fact]
    public void ConstructorStoresXAndY()
    {
        var v = new Vec2(3, -4);
        Assert.Equal(3, v.X);
        Assert.Equal(-4, v.Y);
    }

    [Fact]
    public void ZeroAndDefaultAreZero()
    {
        AssertNear(new Vec2(0, 0), Vec2.Zero);
        AssertNear(new Vec2(0, 0), default(Vec2));
    }

    [Fact]
    public void AddSubtractNegate()
    {
        var a = new Vec2(1, 2);
        var b = new Vec2(10, -5);
        AssertNear(new Vec2(11, -3), a + b);
        AssertNear(new Vec2(9, -7), b - a);   // "from a to b"
        AssertNear(new Vec2(-1, -2), -a);
    }

    [Fact]
    public void ScaleBothOrdersAndDivide()
    {
        var v = new Vec2(2, -3);
        AssertNear(new Vec2(4, -6), v * 2);
        AssertNear(new Vec2(4, -6), 2 * v);
        AssertNear(new Vec2(1, -1.5), v / 2);
    }

    [Fact]
    public void LengthAndLengthSquared()
    {
        var v = new Vec2(3, 4);
        Assert.Equal(25, v.LengthSquared);
        Assert.Equal(5, v.Length, Tolerance);
        Assert.Equal(5, new Vec2(-3, -4).Length, Tolerance);
    }

    [Fact]
    public void NormalizedHasLengthOneAndSameDirection()
    {
        AssertNear(new Vec2(0.6, 0.8), new Vec2(3, 4).Normalized());
        Assert.Equal(1, new Vec2(-7, 13).Normalized().Length, Tolerance);
    }

    [Fact]
    public void NormalizedWorksOnStraightLines()
    {
        // The bug from the first draft: one part being 0 must not give zero.
        AssertNear(new Vec2(0, 1), new Vec2(0, 5).Normalized());
        AssertNear(new Vec2(-1, 0), new Vec2(-0.5, 0).Normalized());
    }

    [Fact]
    public void NormalizingZeroOrTinyGivesZeroNotNaN()
    {
        AssertNear(Vec2.Zero, Vec2.Zero.Normalized());
        AssertNear(Vec2.Zero, new Vec2(1e-300, 0).Normalized());
        AssertNear(Vec2.Zero, new Vec2(1e-10, -1e-10).Normalized());
    }
}
