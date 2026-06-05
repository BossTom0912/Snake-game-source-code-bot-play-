namespace AutoSnakeLive.Shared;

/// <summary>
/// Defines the possible movement directions for the snake.  
/// The order of values can be used to compute vectors.
/// </summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public static class DirectionExtensions
{
    /// <summary>
    /// Returns a relative offset for a given direction in row,column form.
    /// Up is (-1, 0), Down is (1, 0), Left is (0, -1), Right is (0, 1).
    /// </summary>
    public static (int dRow, int dCol) ToOffset(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => (-1, 0),
            Direction.Down => (1, 0),
            Direction.Left => (0, -1),
            Direction.Right => (0, 1),
            _ => (0, 0)
        };
    }
    /// <summary>
    /// Returns the opposite direction.
    /// </summary>
    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => direction
    };
}