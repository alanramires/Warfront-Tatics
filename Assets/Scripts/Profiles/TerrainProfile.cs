using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Terreno_", menuName = "Warfront/Ficha do Terreno")]
public class TerrainProfile  : ScriptableObject
{
    [Header("Identidade")]
    public string terrainName;

    [Header("Qual tile do Tilemap isso representa?")]
    public TileBase tile; // o “ID” do terreno é o TileBase

    [Header("Peculiaridades (Movimento/Bloqueios)")]
    public TerrainCategory category = TerrainCategory.Plain;

    [Header("DPQ (Defesa / Qualidade)")]
    public PositionProfile positionProfile; // PP_Favorable, PP_Standard etc
}
