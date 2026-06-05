using System;
using System.Collections.Generic;
using AutoSnakeLive.Core;
using AutoSnakeLive.Models;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Bot;

/// <summary>
/// Provides pathfinding functionality using the A* algorithm on a grid.  
/// Obstacles and snake segments (except for the tail when allowed) are treated as blocked cells.
/// </summary>
public class PathFinder
{
    /// <summary>
    /// Finds a path between two points on the map using the A* algorithm.  
    /// Snake segments are considered obstacles except for the tail to allow the snake to follow itself.
    /// </summary>
    /// <param name="state">Current game state, used to determine obstacles.</param>
    /// <param name="start">Start coordinate.</param>
    /// <param name="goal">Goal coordinate.</param>
    /// <returns>A list of directions representing the path, or null if no path found.</returns>
    public List<Direction>? FindPath(GameState state, (int Row, int Col) start, (int Row, int Col) goal)
    {
        var map = state.Map;
        var snake = state.Snake;
        // Use priority queue keyed by fScore (g + h)
        var open = new PriorityQueue<(int Row, int Col), double>();
        // Dictionaries for gScore and cameFrom
        var gScore = new Dictionary<(int, int), double>();
        var fScore = new Dictionary<(int, int), double>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        // Initialize start
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        open.Enqueue(start, fScore[start]);
        var visited = new HashSet<(int, int)>();
        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (current.Row == goal.Row && current.Col == goal.Col)
            {
                return ReconstructPath(cameFrom, current, start);
            }
            visited.Add(current);
            foreach (var (nr, nc) in map.GetNeighbours(current.Row, current.Col))
            {
                var neighbor = (nr, nc);
                // Skip if neighbour is obstacle or snake body (excluding tail)
                var cellType = map[nr, nc].Type;
                bool isSnakeBody = snake.Contains(nr, nc);
                // tail of snake is allowed since it will move away in next tick
                if ((cellType == CellType.Obstacle) || (isSnakeBody && !(neighbor == snake.Tail)))
                    continue;
                if (visited.Contains(neighbor))
                    continue;
                var tentativeG = gScore[current] + 1;
                if (!gScore.TryGetValue(neighbor, out var oldG) || tentativeG < oldG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    var f = tentativeG + Heuristic(neighbor, goal);
                    fScore[neighbor] = f;
                    open.Enqueue(neighbor, f);
                }
            }
        }
        return null;
    }

    private static double Heuristic((int Row, int Col) a, (int Row, int Col) b)
    {
        // Manhattan distance for grid movement
        return Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col);
    }

    private static List<Direction> ReconstructPath(Dictionary<(int, int), (int, int)> cameFrom, (int Row, int Col) current, (int Row, int Col) start)
    {
        var path = new List<Direction>();
        while (!current.Equals(start))
        {
            var prev = cameFrom[current];
            path.Insert(0, GetDirection(prev, current));
            current = prev;
        }
        return path;
    }

    private static Direction GetDirection((int Row, int Col) from, (int Row, int Col) to)
    {
        var dRow = to.Row - from.Row;
        var dCol = to.Col - from.Col;
        return (dRow, dCol) switch
        {
            (-1, 0) => Direction.Up,
            (1, 0) => Direction.Down,
            (0, -1) => Direction.Left,
            (0, 1) => Direction.Right,
            _ => Direction.Up
        };
    }
}
