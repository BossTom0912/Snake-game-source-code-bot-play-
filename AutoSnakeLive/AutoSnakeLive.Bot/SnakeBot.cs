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
            if (path != null && path.Count > 0)
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
        if (survivalPath != null && survivalPath.Count > 0)
        {
            return survivalPath[0];
        }
        // As a last resort, pick any available direction that does not immediately hit an obstacle
        foreach (var dir in Enum.GetValues<Direction>())
        {
            var (dRow, dCol) = dir.ToOffset();
            var next = (Row: head.Row + dRow, Col: head.Col + dCol);
            if (state.Map.IsInside(next.Row, next.Col) && state.Map[next.Row, next.Col].Type != CellType.Obstacle && !state.Snake.Contains(next.Row, next.Col))
            {
                return dir;
            }
        }
        // No moves available; just keep current direction to trigger game over
        return state.Snake.CurrentDirection;
    }
}
