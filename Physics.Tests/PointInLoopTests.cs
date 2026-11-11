// AI-GEN AI-013: tests for Christopher's Overlap.PointInLoop (segments, arcs and curves, via chords and caps) and Curve.IsInCap, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class PointInLoopTests
{
    // Builds a closed loop of segments from corner points (the last corner joins back to the first).
    static IEdge[] Loop(params (double x, double y)[] corners)
    {
        var segs = new IEdge[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Length];
            segs[i] = new Segment(new Vec2(a.x, a.y), new Vec2(b.x, b.y));
        }
        return segs;
    }

    static readonly IEdge[] Square = Loop((0, 0), (10, 0), (10, 10), (0, 10));

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
        Assert.False(Overlap.PointInLoop(new Vec2(0, 0), new IEdge[0]));
    }

    // ---------- Loops with arcs (chord trick) ----------

    static Segment Seg(double ax, double ay, double bx, double by) => new(new Vec2(ax, ay), new Vec2(bx, by));

    [Fact]
    public void HalfDisc()
    {
        // Top half of a radius-2 circle (a bump, Start (-2, 0), End (2, 0)), closed by a flat bottom.
        var arc = new Arc(new Vec2(0, 0), 2, new Vec2(0, -1), Math.PI / 2);
        IEdge[] loop = { arc, Seg(2, 0, -2, 0) };
        Assert.True(Overlap.PointInLoop(new Vec2(0, -1), loop));
        Assert.True(Overlap.PointInLoop(new Vec2(1.9, -0.1), loop));   // tucked in a corner
        Assert.False(Overlap.PointInLoop(new Vec2(0, -3), loop));      // above the arc
        Assert.False(Overlap.PointInLoop(new Vec2(0, 1), loop));       // below the flat side
        Assert.False(Overlap.PointInLoop(new Vec2(-3, -1), loop));     // left: ray crosses the arc twice
    }

    [Fact]
    public void SquareWithABulge()
    {
        // 10x10 square, clockwise; its right side bulges out as a half circle (bump: adds area).
        var bulge = new Arc(new Vec2(10, 5), 5, new Vec2(1, 0), Math.PI / 2);
        AssertChains(bulge, 10, 0, 10, 10);
        IEdge[] loop = { Seg(0, 0, 10, 0), bulge, Seg(10, 10, 0, 10), Seg(0, 10, 0, 0) };
        Assert.True(Overlap.PointInLoop(new Vec2(5, 5), loop));
        Assert.True(Overlap.PointInLoop(new Vec2(13, 5), loop));       // in the bulge
        Assert.False(Overlap.PointInLoop(new Vec2(16, 5), loop));      // past it
        Assert.False(Overlap.PointInLoop(new Vec2(14, 1), loop));      // beside it, outside the circle
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 5), loop));
    }

    [Fact]
    public void SquareWithADent()
    {
        // Same square, but the right side dents in as a half circle (bowl: removes area).
        var dent = new Arc(new Vec2(10, 5), 5, new Vec2(-1, 0), -Math.PI / 2);
        AssertChains(dent, 10, 0, 10, 10);
        IEdge[] loop = { Seg(0, 0, 10, 0), dent, Seg(10, 10, 0, 10), Seg(0, 10, 0, 0) };
        Assert.True(Overlap.PointInLoop(new Vec2(3, 5), loop));
        Assert.False(Overlap.PointInLoop(new Vec2(7, 5), loop));       // in the dent
        Assert.True(Overlap.PointInLoop(new Vec2(6, 0.5), loop));      // beside the dent, still in the square
        Assert.False(Overlap.PointInLoop(new Vec2(12, 5), loop));      // right of the square
    }

    [Fact]
    public void WideArcPastNinetyDegrees()
    {
        // 135 degrees each way: most of a circle, closed by a short flat bottom at y = 2cos(45deg).
        // The two chords cancel out, so only the cap (more than half the circle) decides.
        var arc = new Arc(new Vec2(0, 0), 2, new Vec2(0, -1), 3 * Math.PI / 4);
        IEdge[] loop = { arc, new Segment(arc.End, arc.Start) };
        Assert.True(Overlap.PointInLoop(new Vec2(0, 0), loop));        // the center is inside
        Assert.True(Overlap.PointInLoop(new Vec2(0, 1.3), loop));      // just above the flat bottom
        Assert.False(Overlap.PointInLoop(new Vec2(0, 1.6), loop));     // just below it
        Assert.False(Overlap.PointInLoop(new Vec2(3, 0), loop));
    }

    [Fact]
    public void RayTouchingAnArcCornerThatTurnsBack()
    {
        // A "D" bulging right: corners at (0, -2) and (0, 2).
        var arc = new Arc(new Vec2(0, 0), 2, new Vec2(1, 0), Math.PI / 2);
        AssertChains(arc, 0, -2, 0, 2);
        IEdge[] loop = { arc, Seg(0, 2, 0, -2) };
        Assert.True(Overlap.PointInLoop(new Vec2(1, 0), loop));
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 0), loop));
        Assert.False(Overlap.PointInLoop(new Vec2(-1, -2), loop));     // ray at the top corner's height
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 2), loop));      // and the bottom one
    }

    // ---------- Curve caps ----------

    // Bulges down on screen to (5, 5); Cross(A, Bend) is negative for this one.
    static readonly Curve Hump = new(new Vec2(0, 0), new Vec2(5, 10), new Vec2(10, 0));
    // The same hump flipped to bulge up; Cross(A, Bend) is positive.
    static readonly Curve UpHump = new(new Vec2(0, 0), new Vec2(5, -10), new Vec2(10, 0));

    [Fact]
    public void CurveCapBendingEitherWay()
    {
        Assert.True(Hump.IsInCap(new Vec2(5, 3)));       // between the chord (y = 0) and the curve (y = 5)
        Assert.True(Hump.IsInCap(new Vec2(0.5, 0.2)));   // near Start, where the cap is thin
        Assert.False(Hump.IsInCap(new Vec2(5, 6)));      // past the curve
        Assert.False(Hump.IsInCap(new Vec2(5, -1)));     // the other side of the chord
        Assert.False(Hump.IsInCap(new Vec2(12, 2)));     // beyond End

        Assert.True(UpHump.IsInCap(new Vec2(5, -3)));
        Assert.False(UpHump.IsInCap(new Vec2(5, -6)));
        Assert.False(UpHump.IsInCap(new Vec2(5, 3)));
    }

    [Fact]
    public void StraightCurveHasNoCap()
    {
        var straight = new Curve(new Vec2(0, 0), new Vec2(5, 0), new Vec2(10, 0));
        var foldsBack = new Curve(new Vec2(0, 0), new Vec2(15, 0), new Vec2(10, 0));  // control on the line, past End
        Assert.False(straight.IsInCap(new Vec2(5, 0)));
        Assert.False(foldsBack.IsInCap(new Vec2(11, 0)));
    }

    [Fact]
    public void Lens()
    {
        // The hump closed by a flat top.
        IEdge[] loop = { Hump, Seg(10, 0, 0, 0) };
        Assert.True(Overlap.PointInLoop(new Vec2(5, 3), loop));
        Assert.False(Overlap.PointInLoop(new Vec2(5, 6), loop));
        Assert.False(Overlap.PointInLoop(new Vec2(5, -1), loop));
        Assert.False(Overlap.PointInLoop(new Vec2(-1, 2), loop));
    }

    [Fact]
    public void SquareWithACurvedBulgeAndDent()
    {
        // 10x10 square whose bottom side is a curve.
        // Bulging out (down) to y = 15:
        var outward = new Curve(new Vec2(10, 10), new Vec2(5, 20), new Vec2(0, 10));
        IEdge[] bulge = { Seg(0, 0, 10, 0), Seg(10, 0, 10, 10), outward, Seg(0, 10, 0, 0) };
        Assert.True(Overlap.PointInLoop(new Vec2(5, 5), bulge));
        Assert.True(Overlap.PointInLoop(new Vec2(5, 12), bulge));      // in the bulge
        Assert.False(Overlap.PointInLoop(new Vec2(5, 16), bulge));     // past it
        Assert.False(Overlap.PointInLoop(new Vec2(1, 14), bulge));     // beside it (the curve is at y = 11.8 there)

        // Denting in (up) to y = 5:
        var inward = new Curve(new Vec2(10, 10), new Vec2(5, 0), new Vec2(0, 10));
        IEdge[] dent = { Seg(0, 0, 10, 0), Seg(10, 0, 10, 10), inward, Seg(0, 10, 0, 0) };
        Assert.True(Overlap.PointInLoop(new Vec2(5, 3), dent));
        Assert.False(Overlap.PointInLoop(new Vec2(5, 7), dent));       // in the dent
        Assert.False(Overlap.PointInLoop(new Vec2(1, 9.5), dent));     // in the dent's thin end (curve at y = 8.2)
        Assert.True(Overlap.PointInLoop(new Vec2(1, 7), dent));        // above the curve there, still in the square
    }

    // Checks that an arc's ends are where the next edges expect (so the loops above really are closed).
    static void AssertChains(Arc a, double sx, double sy, double ex, double ey)
    {
        Assert.True((a.Start - new Vec2(sx, sy)).Length < 1e-9, $"Start is ({a.Start.X}, {a.Start.Y})");
        Assert.True((a.End - new Vec2(ex, ey)).Length < 1e-9, $"End is ({a.End.X}, {a.End.Y})");
    }
}
