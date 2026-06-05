using System;
using System.Collections.Generic;
using AutoSnakeLive.Models;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Core;

/// <summary>
/// High level manager that coordinates round progression, difficulty and bot decision making.  
/// The Update method advances the game by one tick: it asks the bot for a direction, moves the snake, 
/// handles eating food and spawns new food when necessary.
/// </summary>
public class GameManager
{
    private readonly GameConfig _config;
    private readonly MapGenerator _mapGenerator;
    private readonly RoundManager _roundManager;
    private DifficultyManager _difficultyManager;
    private readonly Func<GameState, Direction> _getNextDirection;

    public GameState State { get; private set; } = null!;

    public bool GameOver { get; private set; }

    /// <summary>
    /// Gets the current round number from the round manager.
    /// </summary>
    public int CurrentRound => _roundManager.RoundNumber;

    public int Score => Math.Max(0, State.Snake.Segments.Count - 3);

    public GameManager(GameConfig config, Func<GameState, Direction>? getNextDirection = null)
    {
        _config = config;
        _mapGenerator = new MapGenerator(config);
        _roundManager = new RoundManager(config);
        _difficultyManager = new DifficultyManager(config);
        _getNextDirection = getNextDirection ?? GetFallbackDirection;
        // Create initial round
        InitialiseRound();
    }

    /// <summary>
    /// Regenerates the map, places the snake and food for a new round.  
    /// Difficulty manager is reset to track time from the start of this round.
    /// </summary>
    private void InitialiseRound()
    {
        var map = _mapGenerator.GenerateMap(_config.LevelObstacleCounts[1]);
        // Spawn snake at centre
        var snake = new Snake(_config.Height / 2, _config.Width / 2);
        var state = new GameState(map, snake);
        // Place obstacles for level 1 (others added at runtime)
        var obstacles = new List<Obstacle>();
        state.PlaceObstacles(obstacles);
        state.RefreshMap();
        state.EnsureFoodCount(_config.MinFoodCount, _config.MaxFoodCount);
        State = state;
        _difficultyManager = new DifficultyManager(_config);
    }

    /// <summary>
    /// Advances the game state by one tick.  
    /// Returns the recommended delay (in milliseconds) before the next tick based on current difficulty.
    /// </summary>
    public int Update()
    {
        if (GameOver)
        {
            return _difficultyManager.GetMoveDelayForScore(Score);
        }
        // Check round completion
        if (_roundManager.IsRoundComplete)
        {
            _roundManager.NextRound();
            InitialiseRound();
        }
        // Increase map difficulty by adding obstacles when level changes.
        // Determine number of obstacles for current level and ensure they are on the map.
        var level = _difficultyManager.CurrentLevel;
        var desiredObstacleCount = _config.LevelObstacleCounts[level];
        if (State.Obstacles.Count < desiredObstacleCount)
        {
            // Generate additional obstacles using MapGenerator and ensure they do not block the snake or food
            var map = State.Map;
            var random = new Random();
            while (State.Obstacles.Count < desiredObstacleCount)
            {
                var r = random.Next(0, map.Height);
                var c = random.Next(0, map.Width);
                if (map[r, c].Type == CellType.Empty && !State.Snake.Contains(r, c) && !State.IsFoodAt(r, c))
                {
                    // Tentatively place obstacle
                    var obstacle = new Obstacle(r, c);
                    map[r, c].Type = CellType.Obstacle;
                    // Validate connectivity; if fails revert
                    if (_mapGenerator.ValidateMap(map))
                    {
                        State.Obstacles.Add(obstacle);
                    }
                    else
                    {
                        map[r, c].Type = CellType.Empty;
                    }
                }
            }
        }
        // Refresh cell types for snake and food before computing the bot path
        State.EnsureFoodCount(_config.MinFoodCount, _config.MaxFoodCount);
        State.RefreshMap();
        // Ask bot for next direction
        var requestedDirection = _getNextDirection(State);
        var safeDirection = GetSafeDirection(State, requestedDirection);
        if (safeDirection is null)
        {
            return _difficultyManager.GetMoveDelayForScore(Score);
        }

        var nextDirection = safeDirection.Value;
        // Determine whether the move will result in eating food prior to moving.
        var snake = State.Snake;
        var prospectiveHead = (Row: snake.Head.Row + nextDirection.ToOffset().dRow,
                               Col: snake.Head.Col + nextDirection.ToOffset().dCol);
        bool willEat = State.IsFoodAt(prospectiveHead.Row, prospectiveHead.Col);
        // Perform move with or without growth
        snake.Move(nextDirection, grow: willEat);
        // If we have eaten food, spawn a new one
        if (willEat)
        {
            State.RemoveFoodAt(prospectiveHead.Row, prospectiveHead.Col);
            State.EnsureFoodCount(_config.MinFoodCount, _config.MaxFoodCount);
        }
        // Refresh map after move and food spawn
        State.RefreshMap();
        return _difficultyManager.GetMoveDelayForScore(Score);
    }

    private static Direction GetFallbackDirection(GameState state)
    {
        return GetSafeDirection(state, state.Snake.CurrentDirection) ?? state.Snake.CurrentDirection;
    }

    private static Direction? GetSafeDirection(GameState state, Direction preferredDirection)
    {
        if (IsSafeMove(state, preferredDirection))
        {
            return preferredDirection;
        }

        foreach (var direction in Enum.GetValues<Direction>())
        {
            if (IsSafeMove(state, direction))
            {
                return direction;
            }
        }

        return null;
    }

    private static bool IsSafeMove(GameState state, Direction direction)
    {
        var snake = state.Snake;
        var (dRow, dCol) = direction.ToOffset();
        var next = (Row: snake.Head.Row + dRow, Col: snake.Head.Col + dCol);
        if (!state.Map.IsInside(next.Row, next.Col))
        {
            return false;
        }

        if (state.Map[next.Row, next.Col].Type == CellType.Obstacle)
        {
            return false;
        }

        var willEat = state.IsFoodAt(next.Row, next.Col);
        if (snake.Contains(next.Row, next.Col) && (willEat || next != snake.Tail))
        {
            return false;
        }

        return true;
    }
}
