// AI-GEN AI-013: tests for Christopher's Arc and Overlap.CircleArc, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class ArcTests
{
    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 1e-9 && Math.Abs(expected.Y - actual.Y) < 1e-9,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    // Top half of a radius-2 circle around the origin: middle points up, 90 degrees each way.
    static readonly Arc TopHalf = new(new Vec2(0, 0), 2, new Vec2(0, 1), Math.PI / 2);

    // Quarter arc around (5, 5), radius 1, middle pointing right, 45 degrees each way.
    static readonly Arc Quarter = new(new Vec2(5, 5), 1, new Vec2(1, 0), Math.PI / 4);

    [Fact]
    public void EndPointsAtTheOrigin()
    {
        // Rotating "up" by +90 degrees (anticlockwise) gives "left"; by -90 gives "right".
        AssertNear(new Vec2(-2, 0), TopHalf.StartPoint);
        AssertNear(new Vec2(2, 0), TopHalf.EndPoint);
    }

    [Fact]
    public void EndPointsAwayFromTheOriginWithOtherRadius()
    {
        double h = Math.Sqrt(0.5);
        AssertNear(new Vec2(5 + h, 5 + h), Quarter.StartPoint);
        AssertNear(new Vec2(5 + h, 5 - h), Quarter.EndPoint);
    }

    [Fact]
    public void ClosestPointWithinTheArc()
    {
        AssertNear(new Vec2(0, 2), TopHalf.ClosestPoint(new Vec2(0, 5)));          // straight above, outside
        AssertNear(new Vec2(0, 2), TopHalf.ClosestPoint(new Vec2(0, 0.5)));        // straight above, inside the circle
        AssertNear(new Vec2(6, 5), Quarter.ClosestPoint(new Vec2(9, 5)));
        double h = Math.Sqrt(2);
        AssertNear(new Vec2(h, h), TopHalf.ClosestPoint(new Vec2(3, 3)));          // diagonal
    }

    [Fact]
    public void ClosestPointOutsideTheArcIsAnEnd()
    {
        AssertNear(new Vec2(-2, 0), TopHalf.ClosestPoint(new Vec2(-1, -3)));       // below, left side
        AssertNear(new Vec2(2, 0), TopHalf.ClosestPoint(new Vec2(1, -3)));         // below, right side
        double h = Math.Sqrt(0.5);
        AssertNear(new Vec2(5 + h, 5 + h), Quarter.ClosestPoint(new Vec2(5, 8)));  // above the quarter
        AssertNear(new Vec2(5 + h, 5 - h), Quarter.ClosestPoint(new Vec2(4, 4)));  // behind, a bit low
    }

    [Fact]
    public void ClosestPointFromTheCenterIsTheMiddle()
    {
        AssertNear(new Vec2(0, 2), TopHalf.ClosestPoint(new Vec2(0, 0)));
        AssertNear(new Vec2(6, 5), Quarter.ClosestPoint(new Vec2(5, 5)));
    }

    [Fact]
    public void WiderThanAHalfCircle()
    {
        // 270 degrees centered on "up" (135 each way): only the bottom 90-degree wedge is missing.
        var wide = new Arc(new Vec2(0, 0), 1, new Vec2(0, 1), 3 * Math.PI / 4);
        AssertNear(new Vec2(-Math.Sqrt(0.5), -Math.Sqrt(0.5)), wide.ClosestPoint(new Vec2(-3, -3)));  // inside the angle, behind the center
        AssertNear(new Vec2(0, -1), new Arc(new Vec2(0, 0), 1, new Vec2(0, 1), Math.PI).ClosestPoint(new Vec2(0, -4))); // full circle
    }

    [Fact]
    public void CircleArc()
    {
        // A circle sitting inside the top half's bowl, under it, doesn't touch the curved edge.
        Assert.False(Overlap.CircleArc(new Circle(new Vec2(0, 0.5), 1), TopHalf));
        Assert.True(Overlap.CircleArc(new Circle(new Vec2(0, 1.5), 1), TopHalf));    // crosses the top
        Assert.True(Overlap.CircleArc(new Circle(new Vec2(2.5, -0.3), 1), TopHalf)); // near the right end
        Assert.False(Overlap.CircleArc(new Circle(new Vec2(0, -1.5), 1), TopHalf));  // below: the missing half doesn't collide
        Assert.False(Overlap.CircleArc(new Circle(new Vec2(0, 3), 1), TopHalf));     // exactly touching
        Assert.True(Overlap.CircleArc(new Circle(new Vec2(5.9, 5.9), 0.5), Quarter)); // near the quarter's start point
    }
}
