using System;

namespace Physics;

public class TickClock
{
    // Static global settings
    /// <summary>
    /// the number of ticks per second
    /// </summary>
    private double TicksPerSecond {get;}
    /// <summary>
    /// To prevent the game from getting stuck forever on a single frame 
    /// </summary>
    private int MaxTicksPerFrame {get;}
    /// <summary>
    /// The max time (in seconds) that the accumulator can gain per frame
    /// </summary>
    private double MaxDelta {get;}

    /// <summary>
    /// Progress to the next frame
    /// </summary>
    public double Alpha => AccumulatedTime * TicksPerSecond;

    // State
    /// <summary>
    /// The amount of time accumulated since the last tick
    /// </summary>
    private double AccumulatedTime = 0;
    /// <summary>
    /// The current rate of time in the game. Used primarily for slowdown effects
    /// </summary>
    public double TimeRate = 1;
    /// <summary>
    /// Whether or not the game is currently paused
    /// </summary>
    public bool Paused = false;
    

    /// <summary>
    /// Constructs a new TickClock object
    /// </summary>
    public TickClock(double ticksPerSecond = 60, int maxTicksPerFrame = 5, double maxDelta = 0.25)
    {
        TicksPerSecond = ticksPerSecond;
        MaxTicksPerFrame = maxTicksPerFrame;
        MaxDelta = maxDelta;
    }

    /// <summary>
    /// WIP function that advances the clock and returns the number of ticks to run
    /// </summary>
    public int AdvanceClock(double delta)
    {
        if (Paused) {return 0;} // Nothing happens while paused
        if (delta > MaxDelta) {delta = MaxDelta;}
        AccumulatedTime += delta * TimeRate;
        int ticksToRun = (int)(AccumulatedTime * TicksPerSecond);
        AccumulatedTime -= ticksToRun/TicksPerSecond;
        if (ticksToRun > MaxTicksPerFrame) {ticksToRun = MaxTicksPerFrame;}
        return ticksToRun;
    }
}