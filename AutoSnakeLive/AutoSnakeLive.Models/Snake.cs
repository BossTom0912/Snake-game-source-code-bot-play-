using System.Collections.Generic;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Models;

/// <summary>
/// Represents the snake entity in the game.  
/// The snake consists of a list of body segments stored as (row, col) tuples.  
/// </summary>
public class Snake
{
    private readonly LinkedList<(int Row, int Col)> _segments = new();

    /// <summary>
    /// Current direction of movement.  Updated by the bot at each tick.
    /// </summary>
    public Direction CurrentDirection { get; set; } = Direction.Right;

    /// <summary>
    /// Gets the coordinate of the snake's head.
    /// </summary>
    public (int Row, int Col) Head => _segments.First!.Value;

    /// <summary>
    /// Gets the coordinate of the snake's tail.
    /// </summary>
    public (int Row, int Col) Tail => _segments.Last!.Value;

    /// <summary>
    /// Readonly snapshot of the snake segments.
    /// </summary>
    public IReadOnlyCollection<(int Row, int Col)> Segments => _segments;

    /// <summary>
    /// Initializes the snake with a starting position and initial length.
    /// </summary>
    public Snake(int startRow, int startCol, int initialLength = 3)
    {
        // Build snake horizontally to the left of the starting point.
        for (var i = 0; i < initialLength; i++)
        {
            _segments.AddLast((startRow, startCol - i));
        }
    }

    /// <summary>
    /// Moves the snake in the specified direction.  
    /// If grow is true, the tail is not removed (snake length increases).
    /// </summary>
    public void Move(Direction direction, bool grow = false)
    {
        var (dRow, dCol) = direction.ToOffset();
        var newHead = (Row: Head.Row + dRow, Col: Head.Col + dCol);
        _segments.AddFirst(newHead);
        if (!grow)
        {
            _segments.RemoveLast();
        }
        CurrentDirection = direction;
    }

    /// <summary>
    /// Checks whether the snake occupies the given coordinate.
    /// </summary>
    public bool Contains(int row, int col) => _segments.Contains((row, col));

    /// <summary>
    /// Creates a deep copy of this snake for simulation purposes.
    /// </summary>
    public Snake Clone()
    {
        var clone = new Snake(0, 0, 0);
        foreach (var segment in _segments)
        {
            clone._segments.AddLast(segment);
        }
        clone.CurrentDirection = CurrentDirection;
        return clone;
    }
}