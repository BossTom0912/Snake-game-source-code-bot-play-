namespace AutoSnakeLive.Models;

/// <summary>
/// Represents a food item on the map.
/// </summary>
public class Food
{
    public int Row { get; set; }
    public int Col { get; set; }
    public Food(int row, int col)
    {
        Row = row;
        Col = col;
    }
}