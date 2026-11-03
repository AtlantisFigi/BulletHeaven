using System;
using System.Collections.Generic;
using BulletHeaven.ECS.Core;

namespace BulletHeaven.Game.Spatial;

public readonly struct CellRange
{
    public readonly int MinCol;
    public readonly int MaxCol;
    public readonly int MinRow;
    public readonly int MaxRow;

    public CellRange(int minCol, int maxCol, int minRow, int maxRow)
    {
        MinCol = minCol;
        MaxCol = maxCol;
        MinRow = minRow;
        MaxRow = maxRow;
    }
}

public sealed class SpatialGrid
{
    public readonly float OriginX;
    public readonly float OriginY;
    public readonly float Width;
    public readonly float Height;
    public readonly float CellSize;
    public readonly float InvCellSize;
    public readonly int Cols;
    public readonly int Rows;

    private readonly List<Entity>[] _cells;

    public SpatialGrid(float originX, float originY, float width, float height, float cellSize = 128f)
    {
        OriginX = originX;
        OriginY = originY;
        Width = width;
        Height = height;
        CellSize = cellSize;
        InvCellSize = 1f / cellSize;

        Cols = Math.Max(1, (int)MathF.Ceiling(width * InvCellSize));
        Rows = Math.Max(1, (int)MathF.Ceiling(height * InvCellSize));

        int totalCells = Cols * Rows;
        _cells = new List<Entity>[totalCells];
        for (int i = 0; i < totalCells; i++)
        {
            _cells[i] = new List<Entity>(32);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Clear();
        }
    }

    public void Insert(Entity entity, float x, float y)
    {
        int col = Math.Clamp((int)((x - OriginX) * InvCellSize), 0, Cols - 1);
        int row = Math.Clamp((int)((y - OriginY) * InvCellSize), 0, Rows - 1);
        _cells[row * Cols + col].Add(entity);
    }

    public CellRange GetRange(float x, float y, float radius)
    {
        int minCol = Math.Clamp((int)((x - radius - OriginX) * InvCellSize), 0, Cols - 1);
        int maxCol = Math.Clamp((int)((x + radius - OriginX) * InvCellSize), 0, Cols - 1);
        int minRow = Math.Clamp((int)((y - radius - OriginY) * InvCellSize), 0, Rows - 1);
        int maxRow = Math.Clamp((int)((y + radius - OriginY) * InvCellSize), 0, Rows - 1);
        return new CellRange(minCol, maxCol, minRow, maxRow);
    }

    public List<Entity> GetCell(int col, int row)
    {
        return _cells[row * Cols + col];
    }
}

