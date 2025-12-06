using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    // Singleton para facilitar o acesso global (Pathfinding chama TerrainManager.Instance)
    public static TerrainManager Instance;

    [System.Serializable]
    public struct TerrainData
    {
        public string name;     // Nome (ex: "Floresta")
        public TileBase tile;   // O arquivo do Tile
        public TerrainCategory category; // A categoria (que define o custo)
    }

    public List<TerrainData> terrainConfig; // Lista para configurar no Inspector
    public Tilemap gameBoard; // Referência ao chão

    // Dicionário para busca rápida
    private Dictionary<TileBase, TerrainCategory> categoryMap = new Dictionary<TileBase, TerrainCategory>();

    void Awake()
    {
        // Configura o Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeCostMap();
    }

    void InitializeCostMap()
    {
        categoryMap.Clear();
        foreach (var data in terrainConfig)
        {
            if (data.tile != null && !categoryMap.ContainsKey(data.tile))
            {
                categoryMap.Add(data.tile, data.category);
            }
        }
    }

    // A LÓGICA DE OURO: Recebe o tile E o tipo da unidade
    // Função que o Pathfinding vai chamar
    public int GetMovementCost(UnityEngine.Vector3Int cellPos, UnitType unitType)
    {
        TileBase tile = gameBoard.GetTile(cellPos);
        if (tile == null) return 999; // Fora do mapa

        // Se não configurou, assume Planície (Custo 1)
        if (!categoryMap.ContainsKey(tile)) return 1;

        TerrainCategory terrain = categoryMap[tile];

        // --- CLASSIFICAÇÃO DA UNIDADE ---
        bool isAir = (unitType == UnitType.JetFighter || unitType == UnitType.Helicopter || unitType == UnitType.Plane);
        bool isSea = (unitType == UnitType.Ship || unitType == UnitType.Sub);
        bool isInfantry = (unitType == UnitType.Infantry || unitType == UnitType.Artillery);
        // --------------------------------

        switch (terrain)
        {
            case TerrainCategory.Plain:
                // Planície, Praia, Asfalto: Custo padrão para todos.
                return 1; 

            case TerrainCategory.Forest:
                // Aéreo e Marítimo ignoram a floresta (custo 1)
                if (isAir || isSea) return 1;
                
                // Terrestre aplica a regra: Infantaria sofre menos (1), Veículos sofrem mais (2)
                return isInfantry ? 1 : 2;

            case TerrainCategory.Mountain:
                // Aéreo ignora, Marítimo bloqueia
                if (isAir) return 1;
                if (isSea) return 99;
                
                // Terrestre aplica a regra: Infantaria (2), Veículos (6)
                return isInfantry ? 2 : 6;

            case TerrainCategory.Water:
                // Apenas unidades marítimas e aéreas podem entrar (Custo 1)
                if (isAir || isSea) return 1;
                return 99; // Bloqueio total para Terrestres
                
            case TerrainCategory.Obstacle:
                return 99; // Muro, bloqueia todos.

            default:
                return 1;
        }
    }
}