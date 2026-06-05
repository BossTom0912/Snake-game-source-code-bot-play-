using System;
using System.Collections.Generic;
using System.Linq;
using AutoSnakeLive.Models;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Core;

/// <summary>
/// Represents all mutable state for the current game session.  
/// Contains the map, snake, food and obstacles, and exposes helper methods for spawning food and obstacles.
/// </summary>
public class GameState
{
    public GameMap Map { get; }
    public Snake Snake { get; set; }
    public List<Food> Foods { get; } = new();
    public Food? Food => Foods.FirstOrDefault();
    public List<Obstacle> Obstacles { get; } = new();

    private readonly Random _random;

    public GameState(GameMap map, Snake snake)
    {
        Map = map;
        Snake = snake;
        _random = new Random();
    }

    /// <summary>
    /// Spawns food at a random empty location on the map.
    /// </summary>
    public void SpawnFood()
    {
        TrySpawnFood();
    }

    public void EnsureFoodCount(int minCount, int maxCount)
    {
        maxCount = Math.Max(minCount, maxCount);
        while (Foods.Count > maxCount)
        {
            Foods.RemoveAt(Foods.Count - 1);
        }

        if (Foods.Count >= minCount)
        {
            return;
        }

        var targetCount = _random.Next(minCount, maxCount + 1);
        while (Foods.Count < targetCount && TrySpawnFood())
        {
        }
    }

    public bool RemoveFoodAt(int row, int col)
    {
        var food = Foods.FirstOrDefault(item => item.Row == row && item.Col == col);
        if (food is null)
        {
            return false;
        }

        Foods.Remove(food);
        return true;
    }

    public bool IsFoodAt(int row, int col) => Foods.Any(food => food.Row == row && food.Col == col);

    private bool TrySpawnFood()
    {
        var maxAttempts = Map.Width * Map.Height * 2;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var r = _random.Next(0, Map.Height);
            var c = _random.Next(0, Map.Width);
            if (CanPlaceFood(r, c))
            {
                AddFood(r, c);
                return true;
            }
        }

        for (var r = 0; r < Map.Height; r++)
        {
            for (var c = 0; c < Map.Width; c++)
            {
                if (CanPlaceFood(r, c))
                {
                    AddFood(r, c);
                    return true;
                }
            }
        }

        return false;
    }

    private bool CanPlaceFood(int row, int col)
    {
        return Map[row, col].Type == CellType.Empty &&
               !Snake.Contains(row, col) &&
               !IsFoodAt(row, col);
    }

    private void AddFood(int row, int col)
    {
        Foods.Add(new Food(row, col));
        Map[row, col].Type = CellType.Food;
    }

    /// <summary>
    /// Places obstacles onto the map and records them in the Obstacles collection.
    /// </summary>
    public void PlaceObstacles(IEnumerable<Obstacle> obstacles)
    {
        Obstacles.Clear();
        foreach (var obstacle in obstacles)
        {
            Obstacles.Add(obstacle);
            var cell = Map[obstacle.Row, obstacle.Col];
            cell.Type = CellType.Obstacle;
        }
    }

    /// <summary>
    /// Resets the map cell types based on current snake, food and obstacles.
    /// </summary>
    public void RefreshMap()
    {
        // Clear everything
        Map.Clear();
        // Place obstacles
        foreach (var obstacle in Obstacles)
        {
            Map[obstacle.Row, obstacle.Col].Type = CellType.Obstacle;
        }
        // Place snake
        foreach (var seg in Snake.Segments)
        {
            Map[seg.Row, seg.Col].Type = CellType.Snake;
        }
        // Place food
        foreach (var food in Foods)
        {
            Map[food.Row, food.Col].Type = CellType.Food;
        }
    }
}
