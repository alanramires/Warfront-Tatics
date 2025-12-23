using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public partial class UnitMovement : MonoBehaviour
{
    // Aqui vão ficar só as coisas de RANGE / ÁREA DE MOVIMENTO
    public void ShowRange()
    {
        if (rangeTilemap == null) return;
        int range = GetEffectiveRange(); 
        UnitType type = data != null ? data.unitType : UnitType.Infantry;

        navigableTiles = Pathfinding.GetReachableTiles(currentCell, range, GetMovementBlockers(), new HashSet<Vector3Int>(), type);
        validMoveTiles = Pathfinding.GetReachableTiles(currentCell, range, GetMovementBlockers(), GetStoppingBlockers(), type);
        
        Color rangeColor = originalColor;
        rangeColor.a = 0.5f; 

        foreach (Vector3Int tilePos in validMoveTiles)
        {
            rangeTilemap.SetTile(tilePos, rangeTile); 
            rangeTilemap.SetTileFlags(tilePos, TileFlags.None);
            rangeTilemap.SetColor(tilePos, rangeColor); 
        }
    }

    void ClearRange()
    {
        if (rangeTilemap != null) rangeTilemap.ClearAllTiles();
    }
    public void ClearVisuals()
    {
        ClearRange();
    }

    public bool IsValidMove(Vector3Int pos)
    {
        return validMoveTiles.Contains(pos);
    }

        public bool IsValidDestination(Vector3Int pos)
    {
        return IsValidMove(pos);
    }
}
