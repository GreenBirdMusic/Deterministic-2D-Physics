// AI-GEN AI-013: tests for Christopher's Solve.SolveCubic, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class SolveTests
{
    // Every answer returned must be one of the expected ones, and every expected one must be returned
    // (a repeated root may come back once or several times).
    static void AssertRoots(double[] expected, double[] actual)
    {
        string got = "[" + string.Join(", ", actual) + "]";
        foreach (double x in actual)
            Assert.True(expected.Any(e => Math.Abs(e - x) < 1e-6), $"got an answer {x} that isn't expected; all answers: {got}");
        foreach (double e in expected)
            Assert.True(actual.Any(x => Math.Abs(e - x) < 1e-6), $"missing the answer {e}; got {got}");
    }

    [Fact]
    public void ThreeSimpleRoots()
    {
        // (t - 1)(t - 2)(t - 3) = t^3 - 6t^2 + 11t - 6
        AssertRoots(new[] { 1.0, 2, 3 }, Solve.SolveCubic(1, -6, 11, -6));
    }

    [Fact]
    public void LeadingNumberIsNotOne()
    {
        // 2(t - 1)(t - 2)(t - 3): same answers, every number doubled
        AssertRoots(new[] { 1.0, 2, 3 }, Solve.SolveCubic(2, -12, 22, -12));
        // -0.5(t + 1)(t - 0.5)(t - 4) = -0.5t^3 + 1.75t^2 + 1.25t - 1
        AssertRoots(new[] { -1.0, 0.5, 4 }, Solve.SolveCubic(-0.5, 1.75, 1.25, -1));
    }

    [Fact]
    public void OneRealRoot()
    {
        // (t - 1)(t^2 + t + 2) = t^3 + 0t^2 + t - 2: the second bracket never reaches 0
        AssertRoots(new[] { 1.0 }, Solve.SolveCubic(1, 0, 1, -2));
        // (t - 3)(t^2 + 1) = t^3 - 3t^2 + t - 3
        AssertRoots(new[] { 3.0 }, Solve.SolveCubic(1, -3, 1, -3));
    }

    [Fact]
    public void PureCube()
    {
        AssertRoots(new[] { 2.0 }, Solve.SolveCubic(1, 0, 0, -8));     // t^3 = 8
        AssertRoots(new[] { -2.0 }, Solve.SolveCubic(1, 0, 0, 8));     // t^3 = -8: Cbrt handles negatives
    }

    [Fact]
    public void TripleRoot()
    {
        // (t - 1)^3 = t^3 - 3t^2 + 3t - 1: the p = 0 case, after the shift
        AssertRoots(new[] { 1.0 }, Solve.SolveCubic(1, -3, 3, -1));
    }

    [Fact]
    public void DoubleRoot()
    {
        // (t - 1)^2 (t + 2) = t^3 - 3t + 2: Delta is exactly 0, Acos's input is exactly -1
        AssertRoots(new[] { 1.0, -2 }, Solve.SolveCubic(1, 0, -3, 2));
        // (t - 1/3)^2 (t - 5): Delta is 0 up to rounding, which can push Acos's input just past 1 or -1
        double r = 1.0 / 3;
        AssertRoots(new[] { r, 5 }, Solve.SolveCubic(1, -(2 * r + 5), r * r + 10 * r, -5 * r * r));
    }

    [Fact]
    public void NoNaNs()
    {
        foreach (var (a, b, c, d) in new[] { (1.0, -6.0, 11.0, -6.0), (1.0, 0.0, -3.0, 2.0), (1.0, -1.0, 1.0 / 3, -1.0 / 27) })
            Assert.DoesNotContain(Solve.SolveCubic(a, b, c, d), double.IsNaN);
    }
}
