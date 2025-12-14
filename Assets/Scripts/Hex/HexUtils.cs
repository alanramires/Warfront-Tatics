using UnityEngine;

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
