// AI-GEN AI-013: tests for Christopher's AlignedBoundingBox and the tight boxes (CompareBounds, ColliderSet.Bounds), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class BoundsTests
{
    static void AssertBox(double minX, double minY, double maxX, double maxY, AlignedBoundingBox b)
    {
        Assert.True(Math.Abs(b.Min.X - minX) < 1e-9 && Math.Abs(b.Min.Y - minY) < 1e-9 &&
                    Math.Abs(b.Max.X - maxX) < 1e-9 && Math.Abs(b.Max.Y - maxY) < 1e-9,
            $"expected ({minX}, {minY}) - ({maxX}, {maxY}) but got ({b.Min.X}, {b.Min.Y}) - ({b.Max.X}, {b.Max.Y})");
    }

    static readonly CollisionGroup G = new(0);

    // ---------- AlignedBoundingBox ----------

    [Fact]
    public void OverlapCountsTouching()
    {
        var a = new AlignedBoundingBox(new Vec2(0, 0), new Vec2(1, 1));
        Assert.True(AlignedBoundingBox.BoxesOverlap(a, new AlignedBoundingBox(new Vec2(1, 0), new Vec2(2, 1)))); // shared side
        Assert.True(AlignedBoundingBox.BoxesOverlap(a, new AlignedBoundingBox(new Vec2(1, 1), new Vec2(2, 2)))); // shared corner
        Assert.True(AlignedBoundingBox.BoxesOverlap(a, new AlignedBoundingBox(new Vec2(0.2, 0.2), new Vec2(0.5, 0.5)))); // inside
        Assert.False(AlignedBoundingBox.BoxesOverlap(a, new AlignedBoundingBox(new Vec2(1.01, 0), new Vec2(2, 1))));
        Assert.False(AlignedBoundingBox.BoxesOverlap(a, new AlignedBoundingBox(new Vec2(0, 1.01), new Vec2(1, 2)))); // x overlaps, y doesn't
    }

    [Fact]
    public void ExpandedGrowsEverySide()
    {
        AssertBox(-0.5, 0.5, 3.5, 4.5, new AlignedBoundingBox(new Vec2(0, 1), new Vec2(3, 4)).Expanded(0.5));
    }

    [Fact]
    public void CheckPointGrowsOnlyWhereNeeded()
    {
        var b = new AlignedBoundingBox(new Vec2(0, 0), new Vec2(1, 1));
        AssertBox(0, 0, 1, 1, b.CheckPoint(new Vec2(0.5, 0.5)));
        AssertBox(0, -2, 3, 1, b.CheckPoint(new Vec2(3, -2)));
    }

    // ---------- Shapes ----------

    [Fact]
    public void SegmentBox()
    {
        AssertBox(-1, 2, 4, 5, new Segment(new Vec2(4, 2), new Vec2(-1, 5)).CompareBounds(null));
    }

    [Fact]
    public void ArcBumpReachesItsTop()
    {
        // Top half of a circle around (0, 0): the top (0, -2) is higher than either end.
        var a = new Arc(new Vec2(0, 0), 2, new Vec2(0, -1), Math.PI / 2);
        AssertBox(-2, -2, 2, 0, a.CompareBounds(null));
    }

    [Fact]
    public void ArcAwayFromTheOrigin()
    {
        // Same arc around (10, 5).
        var a = new Arc(new Vec2(10, 5), 2, new Vec2(0, -1), Math.PI / 2);
        AssertBox(8, 3, 12, 5, a.CompareBounds(null));
    }

    [Fact]
    public void SmallArcHasNoExtremePoints()
    {
        // A 60 degree arc around the right side's middle... centered at (10, 5), pointing up-right.
        var dir = new Vec2(1, -1).Normalized();
        var a = new Arc(new Vec2(10, 5), 2, dir, Math.PI / 12);
        var b = a.CompareBounds(null);
        double minX = Math.Min(a.Start.X, a.End.X), maxX = Math.Max(a.Start.X, a.End.X);
        double minY = Math.Min(a.Start.Y, a.End.Y), maxY = Math.Max(a.Start.Y, a.End.Y);
        AssertBox(minX, minY, maxX, maxY, b);
    }

    [Fact]
    public void FullArcIsTheCircleBox()
    {
        var a = new Arc(new Vec2(3, -4), 1.5, new Vec2(1, 0), Math.PI);
        AssertBox(1.5, -5.5, 4.5, -2.5, a.CompareBounds(null));
    }

    [Fact]
    public void CurveHillTop()
    {
        // The hill from the worked example: top at t = 0.5, (2, -1.5), not the control point's -3.
        var c = new Curve(new Vec2(0, 0), new Vec2(2, -3), new Vec2(4, 0));
        AssertBox(0, -1.5, 4, 0, c.CompareBounds(null));
    }

    [Fact]
    public void CurveTurningOnX()
    {
        var c = new Curve(new Vec2(0, 0), new Vec2(3, 1), new Vec2(0, 2));
        AssertBox(0, 0, 1.5, 2, c.CompareBounds(null));
    }

    [Fact]
    public void CurveWithNoTurnUsesTheEnds()
    {
        var c = new Curve(new Vec2(0, 0), new Vec2(1, 0), new Vec2(3, 2));
        AssertBox(0, 0, 3, 2, c.CompareBounds(null));
    }

    [Fact]
    public void StraightCurveIsFine()
    {
        // Control exactly in the middle: Bend = (0, 0), both t's are 0 / 0 = NaN, skipped.
        var c = new Curve(new Vec2(0, 0), new Vec2(1, 1), new Vec2(2, 2));
        AssertBox(0, 0, 2, 2, c.CompareBounds(null));
    }

    [Fact]
    public void CompareBoundsGrowsAGivenBox()
    {
        var start = new AlignedBoundingBox(new Vec2(-5, -5), new Vec2(-4, -4));
        AssertBox(-5, -5, 4, 0, new Curve(new Vec2(0, 0), new Vec2(2, -3), new Vec2(4, 0)).CompareBounds(start));
    }

    // ---------- Sets (world coordinates) ----------

    [Fact]
    public void CircleSetBoxMovesWithTheObject()
    {
        var set = new ColliderSet(G, 1, new[] { new Circle(new Vec2(1, 0), 0.5) }, new EdgeList[0]);
        set.UpdateWorld(Transform.From(new Vec2(10, 20), 0, false));
        AssertBox(10.5, 19.5, 11.5, 20.5, set.Bounds);
    }

    [Fact]
    public void EdgeListSetBoxMovesWithTheObject()
    {
        var box = new EdgeList(new IEdge[] {
            new Segment(new Vec2(-1, -1), new Vec2(1, -1)), new Segment(new Vec2(1, -1), new Vec2(1, 1)),
            new Segment(new Vec2(1, 1), new Vec2(-1, 1)), new Segment(new Vec2(-1, 1), new Vec2(-1, -1)) }, true);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { box });
        set.UpdateWorld(Transform.From(new Vec2(10, 20), 0, false));
        AssertBox(9, 19, 11, 21, set.Bounds);
    }

    [Fact]
    public void RotatedSquareGetsTheDiamondsBox()
    {
        var box = new EdgeList(new IEdge[] {
            new Segment(new Vec2(-1, -1), new Vec2(1, -1)), new Segment(new Vec2(1, -1), new Vec2(1, 1)),
            new Segment(new Vec2(1, 1), new Vec2(-1, 1)), new Segment(new Vec2(-1, 1), new Vec2(-1, -1)) }, true);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { box });
        set.UpdateWorld(Transform.From(new Vec2(0, 0), Math.PI / 4, false));
        double r = Math.Sqrt(2);
        AssertBox(-r, -r, r, r, set.Bounds);
    }

    [Fact]
    public void MovedArcAndCurveSetBox()
    {
        // A dome (arc bump closed by its chord) and a curve hill, in one set, moved to (100, 50).
        var dome = new Arc(new Vec2(0, 0), 2, new Vec2(0, -1), Math.PI / 2);
        var hill = new Curve(new Vec2(3, 0), new Vec2(5, -3), new Vec2(7, 0));
        var set = new ColliderSet(G, 1, new Circle[0], new[] {
            new EdgeList(new IEdge[] { dome, new Segment(dome.End, dome.Start) }, true),
            new EdgeList(new IEdge[] { hill }, false) });
        set.UpdateWorld(Transform.From(new Vec2(100, 50), 0, false));
        AssertBox(98, 48, 107, 50, set.Bounds);
    }

    [Fact]
    public void MirroredArcSetBox()
    {
        // An arc bump on the right side of the object; mirrored, it's on the left.
        var a = new Arc(new Vec2(3, 0), 1, new Vec2(1, 0), Math.PI / 2);
        var set = new ColliderSet(G, 1, new Circle[0], new[] { new EdgeList(new IEdge[] { a }, false) });
        set.UpdateWorld(Transform.From(new Vec2(0, 0), 0, true));
        AssertBox(-4, -1, -3, 1, set.Bounds);
    }

    [Fact]
    public void MixedCirclesAndEdges()
    {
        var set = new ColliderSet(G, 1, new[] { new Circle(new Vec2(-3, 0), 1) },
            new[] { new EdgeList(new IEdge[] { new Segment(new Vec2(0, 0), new Vec2(2, 3)) }, false) });
        set.UpdateWorld(Transform.From(new Vec2(1, 1), 0, false));
        AssertBox(-3, 0, 3, 4, set.Bounds);
    }

    [Fact]
    public void AddingToAWorldFillsTheBox()
    {
        var table = new GroupTable(new[] { "G" });
        var w = new PhysicsWorld(table, 60, new Vec2(0, 0));
        var o = new PhysicsObject(new Vec2(5, 5), new[] { new ColliderSet(G, 1, new[] { new Circle(new Vec2(0, 0), 1) }, new EdgeList[0]) },
            MovementKind.Static, false);
        w.Add(o);
        AssertBox(4, 4, 6, 6, o.ColliderSets[0].Bounds);
    }
}
