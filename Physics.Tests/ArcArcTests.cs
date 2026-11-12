// AI-GEN AI-013: tests for Christopher's Overlap.ArcArc and the ArcCurve / CurveCurve placeholders, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down, so (0, -1) is "up".
public class ArcArcTests
{
    static readonly Vec2 Up = new(0, -1), Down = new(0, 1), Left = new(-1, 0), Right = new(1, 0);
    static double Deg(double d) => d * Math.PI / 180;

    // Two radius-2 circles, centers 3 apart. They cross at (1.5, -1.32) (top) and (1.5, 1.32) (bottom),
    // about 48.6 degrees from straight up / down as seen from either center.
    static Arc A(Vec2 dir, double halfAngle) => new(new Vec2(0, 0), 2, dir, halfAngle);
    static Arc B(Vec2 dir, double halfAngle) => new(new Vec2(3, 0), 2, dir, halfAngle);

    [Fact]
    public void FacingHalvesCross()
    {
        Assert.True(Overlap.ArcArc(A(Right, Deg(90)), B(Left, Deg(90))));
        Assert.True(Overlap.ArcArc(B(Left, Deg(90)), A(Right, Deg(90))));  // order doesn't matter
    }

    [Fact]
    public void HalvesFacingAwayMiss()
    {
        Assert.False(Overlap.ArcArc(A(Left, Deg(90)), B(Left, Deg(90))));  // A's half is away from both points
        Assert.False(Overlap.ArcArc(A(Left, Deg(90)), B(Right, Deg(90))));
    }

    [Fact]
    public void OnlyTheTopCrossingPoint()
    {
        Assert.True(Overlap.ArcArc(A(Up, Deg(60)), B(Up, Deg(60))));    // both reach 48.6 degrees
        Assert.False(Overlap.ArcArc(A(Up, Deg(40)), B(Up, Deg(40))));   // neither does
        Assert.False(Overlap.ArcArc(A(Up, Deg(60)), B(Up, Deg(40))));   // only one does
    }

    [Fact]
    public void EachArcHoldingADifferentCrossingPoint()
    {
        // A holds only the top point, B only the bottom one: the arcs never actually meet.
        Assert.False(Overlap.ArcArc(A(Up, Deg(60)), B(Down, Deg(60))));
    }

    [Fact]
    public void SignOfHalfAngleDoesNotMatter()
    {
        Assert.True(Overlap.ArcArc(A(Up, -Deg(60)), B(Up, Deg(60))));
        Assert.True(Overlap.ArcArc(A(Right, -Deg(90)), B(Left, -Deg(90))));
        Assert.False(Overlap.ArcArc(A(Up, -Deg(40)), B(Up, -Deg(40))));
    }

    [Fact]
    public void CirclesThatDoNotCross()
    {
        var far = new Arc(new Vec2(5, 0), 2, Left, Deg(90));
        var touching = new Arc(new Vec2(4, 0), 2, Left, Deg(90));
        Assert.False(Overlap.ArcArc(A(Right, Deg(90)), far));
        Assert.True(Overlap.ArcArc(A(Right, Deg(90)), touching));   // exactly touching counts

        // A small circle entirely inside a big one.
        var big = new Arc(new Vec2(0, 0), 5, Right, Deg(180));
        var small = new Arc(new Vec2(1, 0), 1, Right, Deg(180));
        Assert.False(Overlap.ArcArc(big, small));

        // A small circle touching a big one exactly from inside, at (5, 0). Both orders,
        // since the smaller circle being arc1 or arc2 takes a different sign in the formula.
        var touchingInside = new Arc(new Vec2(4, 0), 1, Right, Deg(90));
        Assert.True(Overlap.ArcArc(big, touchingInside));
        Assert.True(Overlap.ArcArc(touchingInside, big));
        // Same circles, but the small arc faces away from the touching point.
        var facingAway = new Arc(new Vec2(4, 0), 1, Left, Deg(45));
        Assert.False(Overlap.ArcArc(big, facingAway));
        Assert.False(Overlap.ArcArc(facingAway, big));
    }

    [Fact]
    public void ArcsOnTheSameCircle()
    {
        Assert.True(Overlap.ArcArc(A(Up, Deg(45)), A(Right, Deg(90))));   // up's right end lies in the right half
        Assert.True(Overlap.ArcArc(A(Up, Deg(10)), A(Up, Deg(90))));      // one inside the other
        Assert.True(Overlap.ArcArc(A(Up, Deg(90)), A(Up, Deg(10))));
        Assert.False(Overlap.ArcArc(A(Up, Deg(30)), A(Down, Deg(30))));   // opposite sides
    }

    [Fact]
    public void SameCenterDifferentRadius()
    {
        var inner = new Arc(new Vec2(0, 0), 1, Up, Deg(90));
        Assert.False(Overlap.ArcArc(A(Up, Deg(90)), inner));
    }

    [Fact]
    public void CurvePlaceholdersThrowUntilWritten()
    {
        // When these get written, replace this test with real ones.
        var curve = new Curve(new Vec2(0, 0), new Vec2(5, 10), new Vec2(10, 0));
        Assert.Throws<NotImplementedException>(() => Overlap.ArcCurve(A(Up, Deg(90)), curve));
        Assert.Throws<NotImplementedException>(() => Overlap.CurveCurve(curve, curve));
    }
}
