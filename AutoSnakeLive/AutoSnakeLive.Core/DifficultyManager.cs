using System;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Core;

/// <summary>
/// Manages the difficulty progression within a single round.  
/// Difficulty increases over time, reducing the tick delay and increasing the number of obstacles.
/// </summary>
public class DifficultyManager
{
    private readonly GameConfig _config;
    private readonly DateTime _startTime;

    public DifficultyManager(GameConfig config)
    {
        _config = config;
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the current difficulty level based on elapsed time since instantiation.
    /// </summary>
    public int CurrentLevel
    {
        get
        {
            var elapsed = DateTime.UtcNow - _startTime;
            return _config.GetCurrentLevel(elapsed);
        }
    }

    /// <summary>
    /// Returns the current tick delay (in milliseconds) based on difficulty level.
    /// </summary>
    public int CurrentDelay => _config.LevelDelays[CurrentLevel];

    public int GetMoveDelayForScore(int score) => _config.GetMoveDelayForScore(score);

    /// <summary>
    /// Returns the number of obstacles to generate for the current level.
    /// </summary>
    public int CurrentObstacleCount => _config.LevelObstacleCounts[CurrentLevel];
}
