// AI-GEN AI-013: tests for Christopher's distance functions (Distance.cs, no curves), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class DistanceTests
{
    static readonly Vec2 Right = new(1, 0);
    static readonly Vec2 Left = new(-1, 0);
    static readonly Vec2 Down = new(0, 1);

    static void Near(double expected, double actual, double tol = 1e-9) =>
        Assert.True(Math.Abs(expected - actual) < tol, $"expected {expected} but got {actual}");

    static Segment Seg(double x1, double y1, double x2, double y2) => new(new Vec2(x1, y1), new Vec2(x2, y2));

    // A box centered on c, clockwise on screen (y down), so solid inside.
    static IEdge[] Box(Vec2 c, double hw, double hh) => new IEdge[] {
        new Segment(c + new Vec2(-hw, -hh), c + new Vec2(hw, -hh)),
        new Segment(c + new Vec2(hw, -hh), c + new Vec2(hw, hh)),
        new Segment(c + new Vec2(hw, hh), c + new Vec2(-hw, hh)),
        new Segment(c + new Vec2(-hw, hh), c + new Vec2(-hw, -hh)) };

    // ---------- Circles ----------

    [Fact]
    public void CirclesApart() => Near(3, Distance.CircleCircle(new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(5, 0), 1)));

    [Fact]
    public void CirclesTouchingOrOverlappingAreZero()
    {
        Near(0, Distance.CircleCircle(new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(2, 0), 1)));
        Near(0, Distance.CircleCircle(new Circle(new Vec2(0, 0), 1), new Circle(new Vec2(1, 0), 1)));
        Near(0, Distance.CircleCircle(new Circle(new Vec2(0, 0), 3), new Circle(new Vec2(0.5, 0), 1))); // inside: filled
    }

    [Fact]
    public void CircleToSegmentMiddleAndEnd()
    {
        Near(1, Distance.CircleEdge(new Circle(new Vec2(0, -2), 1), Seg(-5, 0, 5, 0)));
        Near(4, Distance.CircleEdge(new Circle(new Vec2(10, 0), 1), Seg(-5, 0, 5, 0))); // past the end
        Near(0, Distance.CircleEdge(new Circle(new Vec2(0, 0.5), 1), Seg(-5, 0, 5, 0)));
    }

    [Fact]
    public void CircleToArcIsOnlyTheEdge()
    {
        // Ring arc radius 5 around (0, 0); a small circle near the center is 3 from the edge.
        var arc = new Arc(new Vec2(0, 0), 5, Right, Math.PI);
        Near(3, Distance.CircleEdge(new Circle(new Vec2(1, 0), 1), arc));
        // Outside, facing the arc's middle.
        Near(2, Distance.CircleEdge(new Circle(new Vec2(8, 0), 1), new Arc(new Vec2(0, 0), 5, Right, 0.5)));
    }

    // ---------- Segment vs segment ----------

    [Fact]
    public void SegmentsInATShapeUseTheEndToTheMiddle() =>
        Near(1, Distance.SegmentSegment(Seg(0, 0, 10, 0), Seg(5, 1, 5, 5)));

    [Fact]
    public void SegmentsTShapeEitherOrder() =>
        Near(1, Distance.SegmentSegment(Seg(5, 1, 5, 5), Seg(0, 0, 10, 0)));

    [Fact]
    public void ParallelSegments() => Near(2, Distance.SegmentSegment(Seg(0, 0, 4, 0), Seg(1, 2, 3, 2)));

    [Fact]
    public void CollinearSegmentsEndToEnd() => Near(3, Distance.SegmentSegment(Seg(0, 0, 1, 0), Seg(4, 0, 6, 0)));

    [Fact]
    public void CrossingOrTouchingSegmentsAreZero()
    {
        Near(0, Distance.SegmentSegment(Seg(-1, -1, 1, 1), Seg(-1, 1, 1, -1)));
        Near(0, Distance.SegmentSegment(Seg(0, 0, 2, 0), Seg(1, 0, 1, 3))); // end on the other
    }

    // ---------- Segment vs arc ----------

    [Fact]
    public void SegmentFacingAnArcsMiddle() =>
        Near(3, Distance.SegmentArc(Seg(5, -1, 5, 1), new Arc(new Vec2(10, 0), 2, Left, 0.5)));

    [Fact]
    public void SegmentInsideTheArcsCircleIsNotNegative()
    {
        // Arc radius 5 facing right, +-45 degrees; segment x = 1, y in [-1, 1]. Closest: an end to the arc.
        var arc = new Arc(new Vec2(0, 0), 5, Right, Math.PI / 4);
        double d = Distance.SegmentArc(Seg(1, -1, 1, 1), arc);
        Near(5 - Math.Sqrt(2), d);
    }

    [Fact]
    public void SegmentThroughTheArcsCenter()
    {
        // The middle candidate would be the center itself (skipped); the ends give the answer.
        var arc = new Arc(new Vec2(0, 0), 5, Right, Math.PI / 4);
        // Vertical segment through the center: its top end (0, 2) to the arc's end (5/sqrt2, 5/sqrt2).
        double e = 5 / Math.Sqrt(2);
        Near(Math.Sqrt(e * e + (e - 2) * (e - 2)), Distance.SegmentArc(Seg(0, -2, 0, 2), arc));
        // Diagonal through the center: its end (2, 2) lies on the arc's edge direction.
        Near(5 - Math.Sqrt(8), Distance.SegmentArc(Seg(-2, -2, 2, 2), arc));
    }

    [Fact]
    public void SegmentBesideTheArcUsesTheArcsEnd()
    {
        // Arc radius 1 around (0, 0), facing down, +-30 degrees; ends at angles 60 and 120 degrees.
        // A segment far to the right: closest is the arc's right end.
        var arc = new Arc(new Vec2(0, 0), 1, Down, Math.PI / 6);
        Vec2 end = new(Math.Cos(Math.PI / 3), Math.Sin(Math.PI / 3));
        Near(5 - end.X, Distance.SegmentArc(Seg(5, -10, 5, 10), arc));
    }

    [Fact]
    public void SegmentCrossingAnArcIsZero() =>
        Near(0, Distance.SegmentArc(Seg(3, 0, 7, 0), new Arc(new Vec2(0, 0), 5, Right, 0.5)));

    [Fact]
    public void SegmentArcEitherOrderInTheSwitch()
    {
        var s = Seg(5, -1, 5, 1);
        var a = new Arc(new Vec2(10, 0), 2, Left, 0.5);
        Near(3, Distance.EdgeEdge(s, a));
        Near(3, Distance.EdgeEdge(a, s));
    }

    // ---------- Arc vs arc ----------

    [Fact]
    public void ArcsFacing() =>
        Near(8, Distance.ArcArc(new Arc(new Vec2(0, 0), 1, Right, 0.5), new Arc(new Vec2(10, 0), 1, Left, 0.5)));

    [Fact]
    public void SmallArcInsideABowlBothOrders()
    {
        var bowl = new Arc(new Vec2(0, 0), 10, Right, -0.5);
        var small = new Arc(new Vec2(5, 0), 1, Right, 0.5);
        Near(4, Distance.ArcArc(bowl, small));
        Near(4, Distance.ArcArc(small, bowl));
    }

    [Fact]
    public void FacingArcsOnOverlappingCirclesArePositive()
    {
        // Circles overlap, the arcs don't: closest are the matching ends (x = 3cos0.3 and 4 - 3cos0.3).
        var a = new Arc(new Vec2(0, 0), 3, Right, 0.3);
        var r = new Arc(new Vec2(4, 0), 3, Left, 0.3);
        double expected = 6 * Math.Cos(0.3) - 4;
        Near(expected, Distance.ArcArc(a, r));
        Near(expected, Distance.ArcArc(r, a));
    }

    [Fact]
    public void ConcentricArcs()
    {
        // Same center, radii 2 and 5, both facing right: 3 apart. Facing away from each other: ends.
        var inner = new Arc(new Vec2(0, 0), 2, Right, 0.5);
        Near(3, Distance.ArcArc(inner, new Arc(new Vec2(0, 0), 5, Right, 0.3)));
        Near(3, Distance.ArcArc(new Arc(new Vec2(0, 0), 5, Right, 0.3), inner));
    }

    [Fact]
    public void CrossingArcsAreZero() =>
        Near(0, Distance.ArcArc(new Arc(new Vec2(0, 0), 2, Right, 1), new Arc(new Vec2(3, 0), 2, Left, 1)));

    // ---------- Random pairs against brute force ----------

    static Vec2 RandomPoint(Random rng) => new(rng.NextDouble() * 10 - 5, rng.NextDouble() * 10 - 5);

    static IEdge RandomEdge(Random rng)
    {
        if (rng.Next(2) == 0) return new Segment(RandomPoint(rng), RandomPoint(rng));
        double angle = rng.NextDouble() * 2 * Math.PI;
        double half = 0.1 + rng.NextDouble() * (Math.PI - 0.1);
        return new Arc(RandomPoint(rng), 0.5 + rng.NextDouble() * 3.5, new Vec2(Math.Cos(angle), Math.Sin(angle)),
            rng.Next(2) == 0 ? half : -half);
    }

    static Vec2 PointOn(IEdge e, double f) => e switch
    {
        Segment s => s.Start + (s.End - s.Start) * f,
        Arc a => a.Center + Rotate(a.Direction, (2 * f - 1) * Math.Abs(a.HalfAngle)) * a.Radius,
        _ => throw new NotImplementedException()
    };

    static Vec2 Rotate(Vec2 d, double t) => new(d.X * Math.Cos(t) - d.Y * Math.Sin(t), d.X * Math.Sin(t) + d.Y * Math.Cos(t));

    static double EdgeLength(IEdge e) => e switch
    {
        Segment s => (s.End - s.Start).Length,
        Arc a => 2 * Math.Abs(a.HalfAngle) * a.Radius,
        _ => throw new NotImplementedException()
    };

    // Samples of each edge measured exactly to the other edge (ClosestPoint). The distance to an edge
    // changes by at most the move along the other, so the sampled minimum is at most half a sample
    // spacing above the true one, and never below it.
    static (double min, double slack) BruteForce(IEdge a, IEdge b, int n = 20000)
    {
        double min = double.PositiveInfinity;
        for (int i = 0; i <= n; i++)
        {
            Vec2 p = PointOn(a, (double)i / n);
            min = Math.Min(min, (p - b.ClosestPoint(p)).Length);
            Vec2 q = PointOn(b, (double)i / n);
            min = Math.Min(min, (q - a.ClosestPoint(q)).Length);
        }
        return (min, Math.Min(EdgeLength(a), EdgeLength(b)) / n);
    }

    [Fact]
    public void RandomEdgePairsMatchBruteForce()
    {
        var rng = new Random(20261115);
        for (int i = 0; i < 400; i++)
        {
            IEdge a = RandomEdge(rng);
            IEdge b = RandomEdge(rng);
            double d = Distance.EdgeEdge(a, b);
            (double min, double slack) = BruteForce(a, b);
            Assert.True(d <= min + 1e-9, $"pair {i} ({a}, {b}): got {d}, but two points are only {min} apart");
            Assert.True(d >= min - slack - 1e-9, $"pair {i} ({a}, {b}): got {d}, brute force says {min}");
            Near(d, Distance.EdgeEdge(b, a), 1e-9);
        }
    }

    // ---------- Loops ----------

    [Fact]
    public void CircleOutsideABox() =>
        Near(2, Distance.CircleLoop(new Circle(new Vec2(5, 0), 1), Box(new Vec2(0, 0), 2, 2), true));

    [Fact]
    public void CircleInsideAClosedBoxIsZero() =>
        Near(0, Distance.CircleLoop(new Circle(new Vec2(0, 0), 0.5), Box(new Vec2(0, 0), 2, 2), true));

    [Fact]
    public void CircleInsideAnOpenListMeasuresToTheEdges() =>
        Near(1.5, Distance.CircleLoop(new Circle(new Vec2(0, 0), 0.5), Box(new Vec2(0, 0), 2, 2), false));

    [Fact]
    public void BoxesApart() =>
        Near(3, Distance.LoopLoop(Box(new Vec2(0, 0), 1, 1), Box(new Vec2(5, 0), 1, 1), true, true));

    [Fact]
    public void BoxesApartDiagonally() =>
        Near(Math.Sqrt(2), Distance.LoopLoop(Box(new Vec2(0, 0), 1, 1), Box(new Vec2(3, 3), 1, 1), true, true));

    [Fact]
    public void CrossingBoxesAreZero() =>
        Near(0, Distance.LoopLoop(Box(new Vec2(0, 0), 1, 1), Box(new Vec2(1, 1), 1, 1), true, true));

    [Fact]
    public void SmallBoxInsideABigClosedBoxIsZero() =>
        Near(0, Distance.LoopLoop(Box(new Vec2(0, 0), 0.5, 0.5), Box(new Vec2(0, 0), 3, 3), true, true));

    [Fact]
    public void BigClosedBoxAroundASmallBoxIsZero() =>
        Near(0, Distance.LoopLoop(Box(new Vec2(0, 0), 3, 3), Box(new Vec2(0, 0), 0.5, 0.5), true, true));

    // Open vs closed: "inside" only counts for a closed list doing the surrounding.
    [Fact]
    public void SmallBoxInsideAnOpenBigListMeasuresToTheWalls() =>
        Near(2.5, Distance.LoopLoop(Box(new Vec2(0, 0), 0.5, 0.5), Box(new Vec2(0, 0), 3, 3), true, false));

    [Fact]
    public void OpenBigListAroundASmallBoxMeasuresToTheWalls() =>
        Near(2.5, Distance.LoopLoop(Box(new Vec2(0, 0), 3, 3), Box(new Vec2(0, 0), 0.5, 0.5), false, true));

    [Fact]
    public void ClosedBigBoxAroundAnOpenSmallListIsZero()
    {
        Near(0, Distance.LoopLoop(Box(new Vec2(0, 0), 3, 3), Box(new Vec2(0, 0), 0.5, 0.5), true, false));
        Near(0, Distance.LoopLoop(Box(new Vec2(0, 0), 0.5, 0.5), Box(new Vec2(0, 0), 3, 3), false, true));
    }

    [Fact]
    public void EmptyLoopIsInfinitelyFar()
    {
        Assert.True(double.IsPositiveInfinity(Distance.LoopLoop(new IEdge[0], Box(new Vec2(0, 0), 1, 1), true, true)));
        Assert.True(double.IsPositiveInfinity(Distance.LoopLoop(Box(new Vec2(0, 0), 1, 1), new IEdge[0], true, true)));
    }
}
