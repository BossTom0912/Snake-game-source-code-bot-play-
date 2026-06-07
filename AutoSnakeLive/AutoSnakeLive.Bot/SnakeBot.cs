using System;
using System.Collections.Generic;
using System.Linq;
using AutoSnakeLive.Models;
using AutoSnakeLive.Core;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Bot;

/// <summary>
/// High‑level AI engine that decides which direction the snake should move next.  
/// It combines pathfinding with safety checks to ensure the snake does not trap itself.
/// </summary>
public class SnakeBot
{
    private readonly PathFinder _pathFinder;
    private readonly SafetyChecker _safetyChecker;

    public SnakeBot(PathFinder pathFinder, SafetyChecker safetyChecker)
    {
        _pathFinder = pathFinder;
        _safetyChecker = safetyChecker;
    }

    /// <summary>
    /// Determines the next direction for the snake based on the current game state.  
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>The direction to move.</returns>
    public Direction GetNextDirection(GameState state)
    {
        var head = state.Snake.Head;
        foreach (var food in state.Foods.OrderBy(food => Math.Abs(food.Row - head.Row) + Math.Abs(food.Col - head.Col)))
        {
            var path = _pathFinder.FindPath(state, head, (food.Row, food.Col));
            if (path != null && path.Count > 0 && _safetyChecker.KeepsTailGap(state, path[0]))
            {
                // Ensure the path is safe
                var safe = _safetyChecker.IsPathSafe(state, path, _pathFinder);
                if (safe)
                {
                    return path[0];
                }
            }
        }
        // Either no path to food or unsafe path; fallback to survival path following tail
        var survivalPath = _safetyChecker.GetSurvivalPath(state, _pathFinder);
        if (survivalPath != null &&
            survivalPath.Count > 0 &&
            _safetyChecker.KeepsTailGap(state, survivalPath[0]))
        {
            return survivalPath[0];
        }
        // Prefer a cautious free move before relaxing the tail gap in an emergency.
        foreach (var dir in Enum.GetValues<Direction>())
        {
            if (_safetyChecker.KeepsTailGap(state, dir))
            {
                return dir;
            }
        }

        foreach (var dir in Enum.GetValues<Direction>())
        {
            if (SnakeMovementRules.IsCollisionFree(state, dir))
            {
                return dir;
            }
        }
        // No moves available; just keep current direction to trigger game over
        return state.Snake.CurrentDirection;
    }
}
