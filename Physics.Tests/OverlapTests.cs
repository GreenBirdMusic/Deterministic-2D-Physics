// AI-GEN AI-013: tests for Christopher's Segment, Circle and Overlap, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class SegmentTests
{
    static readonly Segment Flat = new(new Vec2(0, 0), new Vec2(10, 0));

    static void AssertNear(Vec2 expected, Vec2 actual)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 1e-12 && Math.Abs(expected.Y - actual.Y) < 1e-12,
            $"expected ({expected.X}, {expected.Y}) but got ({actual.X}, {actual.Y})");
    }

    [Fact]
    public void ClosestPointInTheMiddle()
    {
        AssertNear(new Vec2(3, 0), Flat.ClosestPoint(new Vec2(3, 4)));     // the lesson's example
        AssertNear(new Vec2(7, 0), Flat.ClosestPoint(new Vec2(7, -2)));    // other side
        AssertNear(new Vec2(5, 0), Flat.ClosestPoint(new Vec2(5, 0)));     // on the segment
    }

    [Fact]
    public void ClosestPointBeyondTheEndsIsTheEnd()
    {
        AssertNear(new Vec2(0, 0), Flat.ClosestPoint(new Vec2(-5, 2)));
        AssertNear(new Vec2(10, 0), Flat.ClosestPoint(new Vec2(25, -3)));
    }

    [Fact]
    public void ClosestPointOnASlantedSegment()
    {
        var s = new Segment(new Vec2(0, 0), new Vec2(4, 4));
        AssertNear(new Vec2(2, 2), s.ClosestPoint(new Vec2(0, 4)));
    }

    [Fact]
    public void DirectionOfTheSegmentDoesNotMatter()
    {
        var reversed = new Segment(new Vec2(10, 0), new Vec2(0, 0));
        AssertNear(new Vec2(3, 0), reversed.ClosestPoint(new Vec2(3, 4)));
        AssertNear(new Vec2(0, 0), reversed.ClosestPoint(new Vec2(-5, 2)));
    }

    [Fact]
    public void ZeroLengthSegmentIsJustAPoint()
    {
        var dot = new Segment(new Vec2(2, 3), new Vec2(2, 3));
        var result = dot.ClosestPoint(new Vec2(100, -50));
        Assert.False(double.IsNaN(result.X) || double.IsNaN(result.Y), "got NaN");
        AssertNear(new Vec2(2, 3), result);
    }
}

public class OverlapTests
{
    static readonly Circle Unit = new(new Vec2(0, 0), 1);

    [Fact]
    public void PointInCircle()
    {
        Assert.True(Overlap.PointInCircle(new Vec2(0, 0), Unit));
        Assert.True(Overlap.PointInCircle(new Vec2(0.6, 0.7), Unit));
        Assert.False(Overlap.PointInCircle(new Vec2(0.8, 0.7), Unit));
        Assert.False(Overlap.PointInCircle(new Vec2(1, 0), Unit));       // exactly on the edge: touching, not inside
    }

    [Fact]
    public void CircleCircle()
    {
        var near = new Circle(new Vec2(1.5, 0), 1);
        var touching = new Circle(new Vec2(2, 0), 1);
        var far = new Circle(new Vec2(3, 4), 1);
        var inside = new Circle(new Vec2(0.1, 0), 0.2);                 // fully inside the other
        Assert.True(Overlap.CircleCircle(Unit, near));
        Assert.True(Overlap.CircleCircle(near, Unit));                    // order doesn't matter
        Assert.False(Overlap.CircleCircle(Unit, touching));
        Assert.False(Overlap.CircleCircle(Unit, far));
        Assert.True(Overlap.CircleCircle(Unit, inside));
    }

    [Fact]
    public void CircleSegment()
    {
        var floor = new Segment(new Vec2(-10, 2), new Vec2(10, 2));
        Assert.False(Overlap.CircleSegment(Unit, floor));                                   // 2 away, radius 1
        Assert.True(Overlap.CircleSegment(new Circle(new Vec2(3, 1.5), 1), floor));         // 0.5 away
        Assert.False(Overlap.CircleSegment(new Circle(new Vec2(3, 1), 1), floor));          // exactly touching

        // Near an end: the closest point is the end itself, not the line beyond it.
        var shortSeg = new Segment(new Vec2(0, 0), new Vec2(5, 0));
        Assert.True(Overlap.CircleSegment(new Circle(new Vec2(5.5, 0.5), 1), shortSeg));    // ~0.71 from the end
        Assert.False(Overlap.CircleSegment(new Circle(new Vec2(6, 1), 1), shortSeg));       // ~1.41 from the end

        // A segment entirely inside the circle still overlaps.
        Assert.True(Overlap.CircleSegment(new Circle(new Vec2(0, 0), 10), new Segment(new Vec2(-1, 0), new Vec2(1, 0))));
    }
}
