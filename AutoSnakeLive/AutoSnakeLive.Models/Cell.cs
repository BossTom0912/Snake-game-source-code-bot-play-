namespace AutoSnakeLive.Models;

using AutoSnakeLive.Shared;

/// <summary>
/// Represents a single cell in the game grid.  
/// Each cell has a position and a type (empty, snake, food or obstacle).
/// </summary>
public class Cell
{
    public int Row { get; }
    public int Col { get; }
    public CellType Type { get; set; }

    public Cell(int row, int col, CellType type = CellType.Empty)
    {
        Row = row;
        Col = col;
        Type = type;
    }
}