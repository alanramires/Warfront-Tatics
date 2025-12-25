using UnityEngine;
using UnityEngine.Tilemaps;

// Utilitários para trabalhar com Grid e Tilemaps
public static class GridUtils
{
    // Obtém a posição central do centro da célula no mundo, com offsets opcionais em Y e Z
    public static Vector3 GetCellCenterWorld(Grid grid, Vector3Int cell, float yOffset = 0f, float zOffset = 0f)
    {
        if (grid == null) return default;
        Vector3 p = grid.GetCellCenterWorld(cell);
        p.y += yOffset;
        p.z += zOffset;
        return p;
    }

    // Obtém a posição do canto inferior esquerdo da célula no mundo, com offsets opcionais em Y e Z
    public static Vector3 CellToWorld(Grid grid, Vector3Int cell, float yOffset = 0f, float zOffset = 0f)
    {
        if (grid == null) return default;
        Vector3 p = grid.CellToWorld(cell);
        p.y += yOffset;
        p.z += zOffset;
        return p;
    }

    // Versão para Tilemap, com offset como Vector3
    public static Vector3 CellToWorld(Tilemap tilemap, Vector3Int cell, Vector3 offset)
    {
        if (tilemap == null) return default;
        return tilemap.CellToWorld(cell) + offset;
    }
}