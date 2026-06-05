namespace AutoSnakeLive.Models;

/// <summary>
/// Represents an immovable obstacle on the map.
/// </summary>
public class Obstacle
{
    public int Row { get; }
    public int Col { get; }

    public Obstacle(int row, int col)
    {
        Row = row;
        Col = col;
    }
}