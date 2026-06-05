using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoSnakeLive.Shared;

/// <summary>
/// Global configuration for the auto snake game.  
/// Centralising these values makes it easier to tune gameplay without touching core logic.
/// </summary>
public class GameConfig
{
    /// <summary>
    /// Width of the game grid.  Default is 30.
    /// </summary>
    public int Width { get; init; } = 30;

    /// <summary>
    /// Height of the game grid.  Default is 40 to achieve a 9:16 aspect ratio on the canvas.
    /// </summary>
    public int Height { get; init; } = 40;

    /// <summary>
    /// Duration of a single round.  Defaults to 30 minutes.
    /// </summary>
    public TimeSpan RoundDuration { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Target render frame rate for the WPF view.
    /// </summary>
    public int TargetFrameRate { get; init; } = 90;

    /// <summary>
    /// Minimum number of apples visible on the map.
    /// </summary>
    public int MinFoodCount { get; init; } = 3;

    /// <summary>
    /// Maximum number of apples visible on the map.
    /// </summary>
    public int MaxFoodCount { get; init; } = 5;

    /// <summary>
    /// Mapping from difficulty level to tick delay in milliseconds.  
    /// Lower values result in faster gameplay.  Six levels are supported.
    /// </summary>
    public Dictionary<int, int> LevelDelays { get; init; } = new()
    {
        [1] = 300,
        [2] = 250,
        [3] = 200,
        [4] = 150,
        [5] = 120,
        [6] = 100
    };

    /// <summary>
    /// Score milestones that increase the snake movement speed.
    /// After the last configured milestone, speed continues increasing every SpeedMilestoneStep points.
    /// </summary>
    public List<int> SpeedMilestones { get; init; } = new() { 10, 20, 40, 60 };

    /// <summary>
    /// Extra score interval after the configured milestones have been reached.
    /// </summary>
    public int SpeedMilestoneStep { get; init; } = 20;

    /// <summary>
    /// Initial delay between grid moves before any speed milestone is reached.
    /// </summary>
    public int InitialMoveDelayMs { get; init; } = 300;

    /// <summary>
    /// Delay removed at each speed milestone.
    /// </summary>
    public int MoveDelayDecreasePerMilestoneMs { get; init; } = 25;

    /// <summary>
    /// Fastest allowed grid move delay. This keeps smooth rendering and UI input responsive.
    /// </summary>
    public int MinimumMoveDelayMs { get; init; } = 70;

    /// <summary>
    /// Mapping from difficulty level to the number of obstacles to generate.  
    /// Values are tuned to keep the map challenging without trapping the bot.
    /// </summary>
    public Dictionary<int, int> LevelObstacleCounts { get; init; } = new()
    {
        [1] = 20,
        [2] = 30,
        [3] = 40,
        [4] = 50,
        [5] = 60,
        [6] = 70
    };

    /// <summary>
    /// Determines the current difficulty level based on elapsed play time.  
    /// Time windows evenly divide the round duration into six segments.
    /// </summary>
    /// <param name="elapsed">Elapsed time since the start of the current round.</param>
    /// <returns>An integer between 1 and 6 inclusive.</returns>
    public int GetCurrentLevel(TimeSpan elapsed)
    {
        var segment = RoundDuration.TotalMinutes / 6.0;
        var index = (int)(elapsed.TotalMinutes / segment) + 1;
        return Math.Clamp(index, 1, 6);
    }

    public int GetMoveDelayForScore(int score)
    {
        var milestoneCount = GetReachedSpeedMilestoneCount(score);
        var delay = InitialMoveDelayMs - milestoneCount * MoveDelayDecreasePerMilestoneMs;
        return Math.Max(MinimumMoveDelayMs, delay);
    }

    public int GetReachedSpeedMilestoneCount(int score)
    {
        var orderedMilestones = SpeedMilestones
            .Where(milestone => milestone > 0)
            .OrderBy(milestone => milestone)
            .ToList();

        var count = orderedMilestones.Count(milestone => score >= milestone);
        if (orderedMilestones.Count == 0 || SpeedMilestoneStep <= 0)
        {
            return count;
        }

        var lastMilestone = orderedMilestones[^1];
        if (score > lastMilestone)
        {
            count += (score - lastMilestone) / SpeedMilestoneStep;
        }

        return count;
    }
}
