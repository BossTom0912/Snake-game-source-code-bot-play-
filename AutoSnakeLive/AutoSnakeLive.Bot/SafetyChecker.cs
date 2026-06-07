using System.Collections.Generic;
using AutoSnakeLive.Models;
using AutoSnakeLive.Core;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Bot;

/// <summary>
/// Evaluates whether prospective moves are safe by simulating snake growth and ensuring a path exists to the snake's tail.  
/// If unsafe, provides an alternative survival path that follows the snake's tail.
/// </summary>
public class SafetyChecker
{
    /// <summary>
    /// Determines whether following the proposed path to food leaves the snake with a path back to its tail.  
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="pathToFood">Path to reach the food.</param>
    /// <param name="pathFinder">PathFinder instance used to compute routes.</param>
    /// <returns>True if safe; otherwise false.</returns>
    public bool IsPathSafe(GameState state, List<Direction> pathToFood, PathFinder pathFinder)
    {
        if (pathToFood.Count == 0 || !KeepsTailGap(state, pathToFood[0]))
        {
            return false;
        }

        // Simulate snake movement along the path
        var cloneSnake = state.Snake.Clone();
        var foodIndex = pathToFood.Count - 1;
        for (int i = 0; i < pathToFood.Count; i++)
        {
            var dir = pathToFood[i];
            bool grow = i == foodIndex;
            cloneSnake.Move(dir, grow);
        }
        // Build a temporary game state with the cloned snake; obstacles and map remain unchanged
        var tempState = new GameState(state.Map, cloneSnake);
        // We need to avoid modifying the original map; treat snake's segments on tempState as obstacles
        // Determine if there is a path from the new head to the new tail using pathFinder
        var head = cloneSnake.Head;
        var tail = cloneSnake.Tail;
        var safePath = pathFinder.FindPath(tempState, head, tail);
        return safePath != null && safePath.Count > 1;
    }

    /// <summary>
    /// When the shortest path to food is unsafe, compute a fallback path that simply follows the snake's tail.  
    /// This allows the snake to survive until a safe route opens up.
    /// </summary>
    public List<Direction>? GetSurvivalPath(GameState state, PathFinder pathFinder)
    {
        var snake = state.Snake;
        var head = snake.Head;
        var tail = snake.Tail;
        return pathFinder.FindPath(state, head, tail);
    }

    public bool KeepsTailGap(GameState state, Direction direction, int gapCells = 1)
    {
        return SnakeMovementRules.PreservesTailGap(state, direction, gapCells);
    }
}
