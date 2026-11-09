// AI-GEN AI-013: tests for Christopher's TickClock, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class TickClockTests
{
    // Runs the clock for a number of frames of the same length; returns the total ticks.
    static int RunFrames(TickClock clock, int frames, double delta)
    {
        int total = 0;
        for (int i = 0; i < frames; i++) total += clock.AdvanceClock(delta);
        return total;
    }

    [Fact]
    public void SteadyScreenMatchingTickRateGivesOneTickEveryFrame()
    {
        var clock = new TickClock(ticksPerSecond: 60);
        for (int i = 0; i < 600; i++)
            Assert.Equal(1, clock.AdvanceClock(1.0 / 60));
    }

    [Fact]
    public void FasterTickRateGivesSeveralTicksPerFrame()
    {
        var clock = new TickClock(ticksPerSecond: 120);
        Assert.Equal(1200, RunFrames(clock, 600, 1.0 / 60));   // 10 seconds
    }

    [Fact]
    public void FasterScreenGivesTheRightTotalOverTime()
    {
        var clock = new TickClock(ticksPerSecond: 60);
        int total = RunFrames(clock, 1440, 1.0 / 144);          // 10 seconds at 144 Hz
        Assert.InRange(total, 599, 600);                         // the last tick may still be building up
    }

    [Fact]
    public void UnevenFramesStillAddUp()
    {
        var clock = new TickClock(ticksPerSecond: 60);
        double[] deltas = { 0.010, 0.033, 0.017, 0.001, 0.050, 0.016, 0.020, 0.003 };  // 0.15 s in all
        int total = 0;
        for (int round = 0; round < 20; round++)                 // 3 seconds
            foreach (var d in deltas) total += clock.AdvanceClock(d);
        Assert.InRange(total, 179, 180);
    }

    [Fact]
    public void HalfTimeRateGivesHalfTheTicks()
    {
        var clock = new TickClock(ticksPerSecond: 60) { TimeRate = 0.5 };
        Assert.InRange(RunFrames(clock, 600, 1.0 / 60), 299, 300);
    }

    [Fact]
    public void NegativeTimeRateBecomesZero()
    {
        var clock = new TickClock(ticksPerSecond: 60) { TimeRate = -2 };
        Assert.Equal(0, clock.TimeRate);
        Assert.Equal(0, RunFrames(clock, 60, 1.0 / 60));
    }

    [Fact]
    public void LaptopWakingFromSleepRunsOnlyTheCap()
    {
        var clock = new TickClock(ticksPerSecond: 60, maxTicksPerFrame: 5);
        Assert.Equal(5, clock.AdvanceClock(3600));                // an hour asleep
        Assert.Equal(1, clock.AdvanceClock(1.0 / 60));            // no catching up afterwards
    }

    [Fact]
    public void ZeroTimeRateGivesNoTicks()
    {
        var clock = new TickClock(ticksPerSecond: 60) { TimeRate = 0 };
        Assert.Equal(0, RunFrames(clock, 600, 1.0 / 60));
    }

    [Fact]
    public void PausedGivesNoTicksAndKeepsAlpha()
    {
        var clock = new TickClock(ticksPerSecond: 60);
        clock.AdvanceClock(0.5 / 60);                            // half a tick built up
        double alphaBefore = clock.Alpha;
        clock.Paused = true;
        Assert.Equal(0, RunFrames(clock, 100, 1.0 / 60));
        Assert.Equal(alphaBefore, clock.Alpha);                   // the picture holds still
        clock.Paused = false;
        Assert.Equal(1, clock.AdvanceClock(0.5 / 60));            // carries on where it left off
    }

    [Fact]
    public void HugeDeltaIsCappedAndTheRestIsDropped()
    {
        var clock = new TickClock(ticksPerSecond: 60, maxTicksPerFrame: 5);
        Assert.Equal(5, clock.AdvanceClock(10));                  // 10 s -> 600 ticks owed -> 5 run
        Assert.Equal(1, clock.AdvanceClock(1.0 / 60));            // the other 595 were dropped, not saved up
    }

    [Fact]
    public void AlphaStaysBetweenZeroAndOne()
    {
        var clock = new TickClock(ticksPerSecond: 60);
        foreach (var d in new[] { 0.004, 0.011, 0.029, 0.0167, 0.2, 0.0001 })
        {
            clock.AdvanceClock(d);
            Assert.InRange(clock.Alpha, 0, 1);
            Assert.True(clock.Alpha < 1, $"alpha was {clock.Alpha}");
        }
    }

    [Fact]
    public void LerpGoesBetweenTwoPoints()
    {
        var a = new Vec2(0, 10);
        var b = new Vec2(4, 2);
        Assert.Equal(a, Vec2.Lerp(a, b, 0));
        Assert.Equal(b, Vec2.Lerp(a, b, 1));
        var mid = Vec2.Lerp(a, b, 0.5);
        Assert.Equal(2, mid.X, 1e-12);
        Assert.Equal(6, mid.Y, 1e-12);
    }
}
