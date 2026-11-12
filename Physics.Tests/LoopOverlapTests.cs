// AI-GEN AI-013: tests for Christopher's Overlap.CircleLoop and Overlap.LoopLoop (segments, arcs and curves; touching counts), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class LoopOverlapTests
{
    static Segment Seg(double ax, double ay, double bx, double by) => new(new Vec2(ax, ay), new Vec2(bx, by));

    // Axis-aligned box as a clockwise loop of segments (y points down).
    static IEdge[] Box(double left, double top, double right, double bottom) => new IEdge[]
    {
        Seg(left, top, right, top),
        Seg(right, top, right, bottom),
        Seg(right, bottom, left, bottom),
        Seg(left, bottom, left, top),
    };

    static readonly IEdge[] Square = Box(0, 0, 10, 10);

    // 10x10 square whose right side bulges out as a half circle, out to x = 15.
    static readonly IEdge[] RightBulge =
    {
        Seg(0, 0, 10, 0),
        new Arc(new Vec2(10, 5), 5, new Vec2(1, 0), Math.PI / 2),    // (10, 0) to (10, 10)
        Seg(10, 10, 0, 10),
        Seg(0, 10, 0, 0),
    };

    // 10x10 square whose right side dents in as a half circle, in to x = 5.
    static readonly IEdge[] RightDent =
    {
        Seg(0, 0, 10, 0),
        new Arc(new Vec2(10, 5), 5, new Vec2(-1, 0), -Math.PI / 2),  // (10, 0) to (10, 10)
        Seg(10, 10, 0, 10),
        Seg(0, 10, 0, 0),
    };

    // 10x10 square whose bottom side is a curve bulging down to y = 15.
    static readonly IEdge[] CurvedBottom =
    {
        Seg(0, 0, 10, 0),
        Seg(10, 0, 10, 10),
        new Curve(new Vec2(10, 10), new Vec2(5, 20), new Vec2(0, 10)),
        Seg(0, 10, 0, 0),
    };

    // ---- CircleLoop ----

    [Fact]
    public void CircleAndBox()
    {
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(5, 5), 1), Square));     // fully inside: no edge touched, found by point in loop
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(11, 5), 2), Square));    // center outside, crosses the right side
        Assert.False(Overlap.CircleLoop(new Circle(new Vec2(20, 5), 2), Square));   // far away
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(-50, 5), 100), Square)); // box fully inside the circle, center outside the box
    }

    [Fact]
    public void CircleTouchingBox()
    {
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(15, 5), 5), Square));    // exactly touching the right side
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(13, 14), 5), Square));   // exactly touching the corner (10, 10): a 3-4-5 triangle
        Assert.False(Overlap.CircleLoop(new Circle(new Vec2(14, 14), 5), Square));  // same corner, 5.66 away
    }

    [Fact]
    public void CircleAndArcLoops()
    {
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(15.5, 5), 1), RightBulge));  // crosses the bulge
        Assert.False(Overlap.CircleLoop(new Circle(new Vec2(16.5, 5), 1), RightBulge)); // just past it
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(16, 5), 1), RightBulge));    // exactly touching its tip
        Assert.False(Overlap.CircleLoop(new Circle(new Vec2(9, 5), 1), RightDent));     // sitting in the dent, clear of the edge
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(5.5, 5), 1), RightDent));    // reaches the dent's edge
    }

    [Fact]
    public void CircleAndCurveLoop()
    {
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(5, 12), 1), CurvedBottom));   // inside the curved part
        Assert.True(Overlap.CircleLoop(new Circle(new Vec2(5, 15.5), 1), CurvedBottom)); // crosses the curve's lowest point
        Assert.False(Overlap.CircleLoop(new Circle(new Vec2(5, 17), 1), CurvedBottom));  // below it
    }

    [Fact]
    public void CircleAndEmptyLoop()
    {
        Assert.False(Overlap.CircleLoop(new Circle(new Vec2(0, 0), 5), Array.Empty<IEdge>()));
    }

    // ---- LoopLoop ----

    [Fact]
    public void BoxesApartAndOverlapping()
    {
        Assert.False(Overlap.LoopLoop(Square, Box(20, 0, 30, 10)));
        Assert.True(Overlap.LoopLoop(Square, Box(5, 5, 15, 15)));
        Assert.True(Overlap.LoopLoop(Box(5, 5, 15, 15), Square));    // order doesn't matter
        Assert.True(Overlap.LoopLoop(Square, Box(-5, 3, 15, 7)));    // a bar through the middle: no corner of either is inside the other
    }

    [Fact]
    public void HalfShiftedBoxes()
    {
        // The example from design/physics.md: these clearly overlap, but no edges strictly cross
        // and no corner is strictly inside. Only the touching rule catches it.
        Assert.True(Overlap.LoopLoop(Square, Box(5, 0, 15, 10)));
        Assert.True(Overlap.LoopLoop(Box(5, 0, 15, 10), Square));
    }

    [Fact]
    public void BoxesTouching()
    {
        Assert.True(Overlap.LoopLoop(Square, Box(10, 0, 20, 10)));    // sharing a whole side
        Assert.True(Overlap.LoopLoop(Square, Box(10, 3, 20, 7)));     // sharing part of a side
        Assert.True(Overlap.LoopLoop(Square, Box(10, 10, 20, 20)));   // sharing only a corner
        Assert.False(Overlap.LoopLoop(Square, Box(10.5, 10.5, 20, 20)));
    }

    [Fact]
    public void BoxInsideBox()
    {
        // No edges meet at all: only the point-in-loop check at the start catches these.
        Assert.True(Overlap.LoopLoop(Square, Box(4, 4, 6, 6)));
        Assert.True(Overlap.LoopLoop(Box(4, 4, 6, 6), Square));
    }

    [Fact]
    public void EmptyLoops()
    {
        Assert.False(Overlap.LoopLoop(Square, Array.Empty<IEdge>()));
        Assert.False(Overlap.LoopLoop(Array.Empty<IEdge>(), Square));
        Assert.False(Overlap.LoopLoop(Array.Empty<IEdge>(), Array.Empty<IEdge>()));
    }

    [Fact]
    public void BoxAndArcLoops()
    {
        // A box from x = 14: it misses the plain square, but the bulge reaches x = 15.
        var box = Box(14, 4, 20, 6);
        Assert.False(Overlap.LoopLoop(Square, box));
        Assert.True(Overlap.LoopLoop(RightBulge, box));
        Assert.True(Overlap.LoopLoop(box, RightBulge));
        Assert.True(Overlap.LoopLoop(RightBulge, Box(15, 4, 20, 6)));     // exactly touching the bulge's tip
        Assert.False(Overlap.LoopLoop(RightBulge, Box(15.5, 4, 20, 6)));

        // A small box sitting in the dent: inside the square's outline, but not inside the shape.
        Assert.False(Overlap.LoopLoop(RightDent, Box(8, 4, 9, 6)));
        Assert.True(Overlap.LoopLoop(Square, Box(8, 4, 9, 6)));
    }

    [Fact]
    public void ArcLoopAgainstArcLoop()
    {
        // A mirror of RightBulge: square from x = 20 whose left side bulges out to x = 15.
        IEdge[] LeftBulge(double left) => new IEdge[]
        {
            Seg(left, 0, left + 10, 0),
            Seg(left + 10, 0, left + 10, 10),
            Seg(left + 10, 10, left, 10),
            new Arc(new Vec2(left, 5), 5, new Vec2(-1, 0), Math.PI / 2),   // (left, 10) to (left, 0)
        };
        Assert.True(Overlap.LoopLoop(RightBulge, LeftBulge(19)));    // the bulges cross (arc/arc)
        Assert.True(Overlap.LoopLoop(RightBulge, LeftBulge(20)));    // their tips touch exactly at (15, 5)
        Assert.False(Overlap.LoopLoop(RightBulge, LeftBulge(21)));
    }

    [Fact]
    public void BoxAndCurveLoop()
    {
        Assert.True(Overlap.LoopLoop(CurvedBottom, Box(4, 14, 6, 20)));    // reaches up into the curve
        Assert.True(Overlap.LoopLoop(Box(4, 14, 6, 20), CurvedBottom));
        Assert.False(Overlap.LoopLoop(CurvedBottom, Box(4, 16, 6, 20)));   // below it
        Assert.False(Overlap.LoopLoop(Square, Box(4, 14, 6, 20)));         // the plain square stops at y = 10
    }

    [Fact]
    public void CurveAgainstCurveIsNotDoneYet()
    {
        // CurveCurve is a placeholder until Ferrari's quartic: two curved loops that don't
        // overlap get as far as comparing the curves, and throw. Change this test when it's written.
        IEdge[] farCurve = { Seg(30, 0, 40, 0), Seg(40, 0, 40, 10), new Curve(new Vec2(40, 10), new Vec2(35, 20), new Vec2(30, 10)), Seg(30, 10, 30, 0) };
        Assert.Throws<NotImplementedException>(() => Overlap.LoopLoop(CurvedBottom, farCurve));
    }
}
