using System;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Core;

/// <summary>
/// Manages the lifecycle of individual rounds.  
/// A round lasts for the configured duration; when complete, the round number increments and the start time resets.
/// </summary>
public class RoundManager
{
    private readonly GameConfig _config;
    private DateTime _roundStart;

    public int RoundNumber { get; private set; } = 1;

    public RoundManager(GameConfig config)
    {
        _config = config;
        _roundStart = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the elapsed time since the current round started.
    /// </summary>
    public TimeSpan Elapsed => DateTime.UtcNow - _roundStart;

    /// <summary>
    /// Returns true if the round timer has reached the configured duration.
    /// </summary>
    public bool IsRoundComplete => Elapsed >= _config.RoundDuration;

    /// <summary>
    /// Resets the round state and increments the round counter.
    /// </summary>
    public void NextRound()
    {
        RoundNumber++;
        _roundStart = DateTime.UtcNow;
    }
}