using System;
using System.Linq;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Core;

public static class SnakeMovementRules
{
    public static bool IsCollisionFree(GameState state, Direction direction)
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
        return !snake.Contains(next.Row, next.Col) || (!willEat && next == snake.Tail);
    }

    public static bool PreservesTailGap(GameState state, Direction direction, int gapCells)
    {
        if (!IsCollisionFree(state, direction))
        {
            return false;
        }

        var snake = state.Snake;
        var (dRow, dCol) = direction.ToOffset();
        var nextHead = (Row: snake.Head.Row + dRow, Col: snake.Head.Col + dCol);
        var willEat = state.IsFoodAt(nextHead.Row, nextHead.Col);
        var segments = snake.Segments.ToList();
        var predictedTail = willEat || segments.Count < 2
            ? snake.Tail
            : segments[^2];

        var distance = Math.Abs(nextHead.Row - predictedTail.Row) +
                       Math.Abs(nextHead.Col - predictedTail.Col);
        return distance > Math.Max(0, gapCells);
    }
}
