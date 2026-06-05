namespace AutoSnakeLive.Shared;

/// <summary>
/// Represents the type of a cell within the game grid.  
/// Cells may be empty, part of the snake, a food pellet or an obstacle.
/// </summary>
public enum CellType
{
    Empty,
    Snake,
    Food,
    Obstacle
}