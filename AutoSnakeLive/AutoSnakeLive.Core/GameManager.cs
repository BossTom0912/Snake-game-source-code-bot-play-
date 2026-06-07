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
    private readonly Random _random = new();
    private DifficultyManager _difficultyManager;
    private readonly Func<GameState, Direction> _getNextDirection;
    private DateTime? _foodRefillDueAt;
    private int _foodRefillTarget;

    public GameState State { get; private set; } = null!;

    public bool GameOver { get; private set; }

    public event Action? AppleEaten;

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
        var map = _mapGenerator.GenerateMap(0);
        // Spawn snake at centre
        var snake = new Snake(_config.Height / 2, _config.Width / 2);
        var state = new GameState(map, snake);
        state.RefreshMap();
        state.FillFoodToCount(GetRandomFoodTarget());
        State = state;
        _foodRefillDueAt = null;
        _foodRefillTarget = 0;
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
        // Refresh cell types and process delayed apple refill before asking the bot.
        State.RefreshMap();
        ProcessFoodRefill();
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
            AppleEaten?.Invoke();
            HandleFoodCountAfterEating();
        }
        // Refresh map after move and food spawn
        State.RefreshMap();
        return _difficultyManager.GetMoveDelayForScore(Score);
    }

    private void ProcessFoodRefill()
    {
        if (State.Foods.Count == 0)
        {
            RefillFoodImmediately();
            return;
        }

        ScheduleFoodRefillIfNeeded(resetDelay: false);
        if (_foodRefillDueAt is null || DateTime.UtcNow < _foodRefillDueAt.Value)
        {
            return;
        }

        State.FillFoodToCount(_foodRefillTarget);
        _foodRefillDueAt = null;
        _foodRefillTarget = 0;
    }

    private void HandleFoodCountAfterEating()
    {
        if (State.Foods.Count == 0)
        {
            RefillFoodImmediately();
            return;
        }

        if (State.Foods.Count <= _config.FoodRefillThreshold)
        {
            ScheduleFoodRefillIfNeeded(resetDelay: true);
        }
        else
        {
            _foodRefillDueAt = null;
            _foodRefillTarget = 0;
        }
    }

    private void ScheduleFoodRefillIfNeeded(bool resetDelay)
    {
        if (State.Foods.Count == 0 || State.Foods.Count > _config.FoodRefillThreshold)
        {
            return;
        }

        if (_foodRefillDueAt is not null && !resetDelay)
        {
            return;
        }

        var minMilliseconds = (int)_config.MinimumFoodRefillDelay.TotalMilliseconds;
        var maxMilliseconds = (int)_config.MaximumFoodRefillDelay.TotalMilliseconds;
        if (maxMilliseconds < minMilliseconds)
        {
            (minMilliseconds, maxMilliseconds) = (maxMilliseconds, minMilliseconds);
        }

        var refillDelay = _random.Next(minMilliseconds, maxMilliseconds + 1);
        _foodRefillDueAt = DateTime.UtcNow.AddMilliseconds(refillDelay);
        _foodRefillTarget = GetRandomFoodTarget();
    }

    private void RefillFoodImmediately()
    {
        State.FillFoodToCount(GetRandomFoodTarget());
        _foodRefillDueAt = null;
        _foodRefillTarget = 0;
    }

    private int GetRandomFoodTarget()
    {
        var minCount = Math.Min(_config.MinFoodCount, _config.MaxFoodCount);
        var maxCount = Math.Max(_config.MinFoodCount, _config.MaxFoodCount);
        return _random.Next(minCount, maxCount + 1);
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
