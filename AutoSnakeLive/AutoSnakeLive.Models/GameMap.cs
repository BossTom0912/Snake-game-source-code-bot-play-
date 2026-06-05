using System.Collections.Generic;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Models;

/// <summary>
/// Represents the overall game grid containing cells.  
/// Provides helper methods to check boundaries, access neighbouring cells and update cell types.
/// </summary>
public class GameMap
{
    public int Width { get; }
    public int Height { get; }
    private readonly Cell[,] _cells;

    public GameMap(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new Cell[height, width];
        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                _cells[r, c] = new Cell(r, c);
            }
        }
    }

    /// <summary>
    /// Indexer to access a cell directly by row and column.
    /// </summary>
    public Cell this[int row, int col] => _cells[row, col];

    /// <summary>
    /// Returns true if the specified coordinate lies within the bounds of the map.
    /// </summary>
    public bool IsInside(int row, int col) => row >= 0 && row < Height && col >= 0 && col < Width;

    /// <summary>
    /// Enumerates four-way neighbours (up, down, left, right) that are inside the map.
    /// </summary>
    public IEnumerable<(int Row, int Col)> GetNeighbours(int row, int col)
    {
        foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
        {
            var nr = row + dr;
            var nc = col + dc;
            if (IsInside(nr, nc))
                yield return (nr, nc);
        }
    }

    /// <summary>
    /// Resets all cells in the map to empty.
    /// </summary>
    public void Clear()
    {
        for (int r = 0; r < Height; r++)
        {
            for (int c = 0; c < Width; c++)
            {
                _cells[r, c].Type = CellType.Empty;
            }
        }
    }
}