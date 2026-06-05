using AutoSnakeLive.Models;
using AutoSnakeLive.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoSnakeLive.Core;

/// <summary>
/// Responsible for procedurally generating valid maps based on a supplied seed and configuration.  
/// Maps are validated using a flood‑fill algorithm to ensure there are no isolated pockets of empty space.
/// </summary>
public class MapGenerator
{
    private readonly GameConfig _config;

    public MapGenerator(GameConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Generates a new map with the specified number of obstacles.  
    /// Uses Guid.NewGuid().GetHashCode() as the seed to ensure uniqueness across runs.  
    /// The method regenerates the map until a valid configuration passes flood‑fill validation.
    /// </summary>
    public GameMap GenerateMap(int obstacleCount)
    {
        var seed = Guid.NewGuid().GetHashCode();
        var random = new Random(seed);
        while (true)
        {
            var map = new GameMap(_config.Width, _config.Height);
            // Optionally reserve an area around the center for the snake spawn.
            var spawnRow = _config.Height / 2;
            var spawnCol = _config.Width / 2;
            // Place obstacles at random positions; avoid spawn area.
            int placed = 0;
            var maxAttempts = obstacleCount * 5;
            int attempts = 0;
            while (placed < obstacleCount && attempts < maxAttempts)
            {
                attempts++;
                var r = random.Next(0, _config.Height);
                var c = random.Next(0, _config.Width);
                // Do not place obstacles on spawn and ensure at least one row/col margin around spawn for safety.
                if (Math.Abs(r - spawnRow) < 2 && Math.Abs(c - spawnCol) < 2)
                    continue;
                if (map[r, c].Type == CellType.Empty)
                {
                    map[r, c].Type = CellType.Obstacle;
                    placed++;
                }
            }
            // Validate connectivity; if valid return map.
            if (ValidateMap(map))
                return map;
            // otherwise try again with a new random seed
            seed = Guid.NewGuid().GetHashCode();
            random = new Random(seed);
        }
    }

    /// <summary>
    /// Validates that all empty cells in the map are part of a single connected region.  
    /// Uses a breadth‑first flood‑fill starting from the first empty cell found.
    /// </summary>
    public bool ValidateMap(GameMap map)
    {
        // Find starting empty cell
        (int Row, int Col)? start = null;
        for (int r = 0; r < map.Height && start == null; r++)
        {
            for (int c = 0; c < map.Width; c++)
            {
                if (map[r, c].Type == CellType.Empty)
                {
                    start = (r, c);
                    break;
                }
            }
        }
        if (start is null)
            return false; // no empty cells at all
        var visited = new bool[map.Height, map.Width];
        var queue = new Queue<(int Row, int Col)>();
        queue.Enqueue(start.Value);
        visited[start.Value.Row, start.Value.Col] = true;
        // BFS flood fill over empty cells
        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            foreach (var (nr, nc) in map.GetNeighbours(row, col))
            {
                if (!visited[nr, nc] && map[nr, nc].Type == CellType.Empty)
                {
                    visited[nr, nc] = true;
                    queue.Enqueue((nr, nc));
                }
            }
        }
        // Ensure every empty cell was visited
        for (int r = 0; r < map.Height; r++)
        {
            for (int c = 0; c < map.Width; c++)
            {
                if (map[r, c].Type == CellType.Empty && !visited[r, c])
                    return false;
            }
        }
        return true;
    }
}