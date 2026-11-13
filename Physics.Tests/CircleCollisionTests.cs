// AI-GEN AI-013: tests for Christopher's moving circle checks (Collision.CircleCircle so far), written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

// Screen coordinates: Y points down. Hit.Point for a circle is the contact point, not the center.
public class CircleCollisionTests
{
    static void AssertNear(Vec2 expected, Vec2 actual, double tol = 1e-9)
    {
        Assert.Equal(expected.X, actual.X, tol);
        Assert.Equal(expected.Y, actual.Y, tol);
    }

    static void AssertHit(double t, Vec2 point, Vec2 normal, Hit? actual, double tol = 1e-9)
    {
        Assert.True(actual.HasValue, "expected a hit");
        Hit h = actual.Value;
        Assert.Equal(t, h.T, tol);
        AssertNear(point, h.Point, tol);
        AssertNear(normal, h.Normal, tol);
    }

    static Circle C(double x, double y, double r) => new(new Vec2(x, y), r);
    static readonly Circle Target = C(10, 0, 3);   // static circle, radius 3

    // ---- CircleCircle ----

    [Fact]
    public void HeadOn()
    {
        // Radius 2 from (0, 0) moving right 10: touches when the centers are 5 apart, center at (5, 0).
        // Contact point: on the line between centers, 2 past the mover's center.
        AssertHit(0.5, new Vec2(7, 0), new Vec2(-1, 0), Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(10, 0)));
    }

    [Fact]
    public void HeadOn_ContactIsOnBothCircles()
    {
        Hit h = Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(10, 0))!.Value;
        Assert.Equal(3, (h.Point - Target.Center).Length, 1e-9);            // on the static circle
        Assert.Equal(2, (h.Point - new Vec2(5, 0)).Length, 1e-9);           // on the mover at the hit
    }

    [Fact]
    public void Glancing()
    {
        // Moving right along y = -4: touches when the centers are 5 apart, so x offset 3, center at (7, -4).
        // Normal from the static center to the mover's center: (-3, -4) / 5.
        var n = new Vec2(-0.6, -0.8);
        AssertHit(0.7, Target.Center + 3 * n, n, Collision.CircleCircle(C(0, -4, 2), Target, new Vec2(10, 0)));
    }

    [Fact]
    public void JustMisses()
    {
        Assert.Null(Collision.CircleCircle(C(0, -5.000001, 2), Target, new Vec2(20, 0)));
    }

    [Fact]
    public void JustTouches_Grazing_NoHit()
    {
        // Exactly tangent pass (centers' closest distance = 5): a graze, not a hit (same as PointCircle).
        Assert.Null(Collision.CircleCircle(C(0, -5, 2), Target, new Vec2(20, 0)));
    }

    [Fact]
    public void StopsShort_NoHit() =>
        Assert.Null(Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(4, 0)));

    [Fact]
    public void EndsExactlyTouching_Hits() =>
        AssertHit(1, new Vec2(7, 0), new Vec2(-1, 0), Collision.CircleCircle(C(0, 0, 2), Target, new Vec2(5, 0)));

    [Fact]
    public void StartsTouching_MovingIn_HitAtZero() =>
        AssertHit(0, new Vec2(7, 0), new Vec2(-1, 0), Collision.CircleCircle(C(5, 0, 2), Target, new Vec2(1, 0)));

    [Fact]
    public void StartsOverlapping_MovingOut_NoHit() =>
        Assert.Null(Collision.CircleCircle(C(6, 0, 2), Target, new Vec2(-5, 0)));

    [Fact]
    public void StartsOverlapping_MovingIn_HitAtZero() =>
        // Filled circle: nothing goes deeper. Contact point: 2 past the mover's center, toward the target.
        AssertHit(0, new Vec2(8, 0), new Vec2(-1, 0), Collision.CircleCircle(C(6, 0, 2), Target, new Vec2(1, 0)));

    [Fact]
    public void ZeroRadius_SameAsPointCircle()
    {
        var p = new Vec2(0, -1);
        var v = new Vec2(12, 1);
        Hit? a = Collision.CircleCircle(new Circle(p, 0), Target, v);
        Hit? b = Collision.PointCircle(p, Target, v);
        Assert.True(a.HasValue && b.HasValue);
        AssertHit(b.Value.T, b.Value.Point, b.Value.Normal, a);
    }

    [Fact]
    public void NoMove_NoHit() =>
        Assert.Null(Collision.CircleCircle(C(0, 0, 2), Target, Vec2.Zero));
}
