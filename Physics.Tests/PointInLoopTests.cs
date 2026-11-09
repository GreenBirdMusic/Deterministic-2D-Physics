// AI-GEN AI-013: tests for Christopher's Overlap.PointInLoop, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class PointInLoopTests
{
    // Builds a closed loop of segments from corner points (the last corner joins back to the first).
    static Segment[] Loop(params (double x, double y)[] corners)
    {
        var segs = new Segment[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Length];
            segs[i] = new Segment(new Vec2(a.x, a.y), new Vec2(b.x, b.y));
        }
        return segs;
    }

    static readonly Segment[] Square = Loop((0, 0), (10, 0), (10, 10), (0, 10));

    [Fact]
    public void InsideAndOutsideASquare()
    {
        Assert.True(Overlap.PointInLoop(new Vec2(5, 5), Square));
        Assert.True(Overlap.PointInLoop(new Vec2(0.1, 9.9), Square));
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 5), Square));     // left: ray crosses both sides
        Assert.False(Overlap.PointInLoop(new Vec2(11, 5), Square));     // right: ray crosses nothing
        Assert.False(Overlap.PointInLoop(new Vec2(5, 11), Square));     // above
        Assert.False(Overlap.PointInLoop(new Vec2(5, -1), Square));     // below
    }

    [Fact]
    public void DirectionOfTheLoopDoesNotMatter()
    {
        var reversed = Loop((0, 0), (0, 10), (10, 10), (10, 0));
        Assert.True(Overlap.PointInLoop(new Vec2(5, 5), reversed));
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 5), reversed));
    }

    [Fact]
    public void RayRunningAlongAHorizontalEdge()
    {
        // p.Y = 0 lines up with the square's bottom edge; the ray runs along it.
        Assert.False(Overlap.PointInLoop(new Vec2(-5, 0), Square));
        Assert.False(Overlap.PointInLoop(new Vec2(-5, 10), Square));
    }

    [Fact]
    public void RayThroughACornerWhereTheLoopPassesThrough()
    {
        // Diamond: the ray from the center goes exactly through the right corner (10, 5).
        var diamond = Loop((5, 0), (10, 5), (5, 10), (0, 5));
        Assert.True(Overlap.PointInLoop(new Vec2(5, 5), diamond));
        Assert.False(Overlap.PointInLoop(new Vec2(-3, 5), diamond));    // through both side corners
    }

    [Fact]
    public void RayTouchingACornerThatTurnsBack()
    {
        // Triangle pointing up; its top corner (5, 10) is touched by a ray at y = 10 and turns back down.
        var triangle = Loop((0, 0), (10, 0), (5, 10));
        Assert.False(Overlap.PointInLoop(new Vec2(0, 10), triangle));
        // Triangle pointing down; its bottom corner (5, 0) touched the same way.
        var down = Loop((0, 10), (10, 10), (5, 0));
        Assert.False(Overlap.PointInLoop(new Vec2(0, 0), down));
    }

    [Fact]
    public void ConcaveShape()
    {
        // A "U": the gap between the arms is outside, the arms and base are inside.
        var u = Loop((0, 0), (10, 0), (10, 10), (7, 10), (7, 3), (3, 3), (3, 10), (0, 10));
        Assert.True(Overlap.PointInLoop(new Vec2(1, 8), u));            // left arm: ray crosses 3 edges
        Assert.True(Overlap.PointInLoop(new Vec2(8, 8), u));            // right arm
        Assert.True(Overlap.PointInLoop(new Vec2(5, 1), u));            // base
        Assert.False(Overlap.PointInLoop(new Vec2(5, 8), u));           // in the gap: ray crosses 2 edges
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 8), u));          // left of everything: 4 edges
    }

    [Fact]
    public void RayThroughTheInnerCornersOfAConcaveShape()
    {
        // Same "U"; y = 3 runs exactly along the inside of the base, through two inner corners.
        var u = Loop((0, 0), (10, 0), (10, 10), (7, 10), (7, 3), (3, 3), (3, 10), (0, 10));
        Assert.True(Overlap.PointInLoop(new Vec2(1, 3), u));
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 3), u));
        Assert.False(Overlap.PointInLoop(new Vec2(11, 3), u));
    }

    [Fact]
    public void EmptyLoopContainsNothing()
    {
        Assert.False(Overlap.PointInLoop(new Vec2(0, 0), new Segment[0]));
    }
}
