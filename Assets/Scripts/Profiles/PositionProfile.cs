using UnityEngine;

[CreateAssetMenu(fileName = "PositionProfile", menuName = "Warfront/Ficha do DPQ")]
public class PositionProfile : ScriptableObject
{
    [Header("Identidade")]
    public PositionQuality quality = PositionQuality.Standard;

    [Header("DPQ (editável no asset)")]
    public int qualityPoints = 1;
    public int defenseBonus = 0;
}


public enum PositionQuality
{
    Unfavorable, // 0 pts, -1 def
    Standard,    // 1 pt,  0 def
    Improved,    // 2 pts, +2 def
    Favorable,   // 3 pts, +4 def
    Unique       // 4pts, +6 def (uso especial, ex: Base)

}

