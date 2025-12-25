using UnityEngine;
using System.Collections.Generic;

public enum HexLayout
{
    OddR,  // Pointy-top (linhas deslocadas) - o mais comum
    OddQ   // Flat-top (colunas deslocadas)
}

public static class HexUtils
{
    // floor(a/2) que funciona com números negativos (igual Math.floor do JS)
    private static int FloorDiv2(int a)
    {
        return (a >= 0) ? (a / 2) : -(((-a) + 1) / 2);
    }

    public static Vector3Int OffsetToCube(Vector3Int offset, HexLayout layout)
    {
        int col = offset.x;
        int row = offset.y;

        switch (layout)
        {
            // ODD-R: linhas deslocadas (pointy-top)
            case HexLayout.OddR:
            {
                int x = col - FloorDiv2(row);
                int z = row;
                int y = -x - z;
                return new Vector3Int(x, y, z);
            }

            // ODD-Q: colunas deslocadas (flat-top)
            case HexLayout.OddQ:
            {
                int x = col;
                int z = row - FloorDiv2(col);
                int y = -x - z;
                return new Vector3Int(x, y, z);
            }
        }

        return default;
    }

    public static List<Vector3Int> GetNeighborsOddR(Vector3Int node)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>(6);
        bool isOddRow = (node.y & 1) != 0;

        Vector3Int[] evenOffsets = {
            new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0),
            new Vector3Int(-1, 0, 0), new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0)
        };
        Vector3Int[] oddOffsets = {
            new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0)
        };

        Vector3Int[] directions = isOddRow ? oddOffsets : evenOffsets;
        for (int i = 0; i < directions.Length; i++)
        {
            neighbors.Add(node + directions[i]);
        }

        return neighbors;
    }

    public static Vector3Int GetSmartVerticalMoveOddR(Vector3Int current, int directionY, System.Func<Vector3Int, bool> isValidMove)
    {
        bool isOddRow = (current.y & 1) != 0;

        int offsetLeft = isOddRow ? 0 : -1;
        int offsetRight = isOddRow ? 1 : 0;

        Vector3Int optionA = new Vector3Int(current.x + offsetRight, current.y + directionY, 0);
        Vector3Int optionB = new Vector3Int(current.x + offsetLeft, current.y + directionY, 0);

        if (isValidMove == null) return optionA;
        if (isValidMove(optionA)) return optionA;
        if (isValidMove(optionB)) return optionB;

        return optionA;
    }
    public static int HexDistance(Vector3Int aOffset, Vector3Int bOffset, HexLayout layout)
    {
        Vector3Int a = OffsetToCube(aOffset, layout);
        Vector3Int b = OffsetToCube(bOffset, layout);

        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        int dz = Mathf.Abs(a.z - b.z);

        return Mathf.Max(dx, dy, dz);
    }
}
