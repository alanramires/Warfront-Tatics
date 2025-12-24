using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public PositionProfile positionProfile; // DPQ (FavorÃ¡vel/Melhorado/PadrÃ£o/DesfavorÃ¡vel)

    }

    public List<TerrainProfile> TerrainProfiles; // arrasta seus .asset aqui

    public Tilemap gameBoard; // ReferÃªncia ao chÃ£o

    // DicionÃ¡rio para busca rÃ¡pida
    private Dictionary<TileBase, TerrainCategory> categoryMap = new Dictionary<TileBase, TerrainCategory>();
    private Dictionary<TileBase, int> defenseBonusMap = new Dictionary<TileBase, int>();
    private Dictionary<TileBase, int> qualityPointsMap = new Dictionary<TileBase, int>();

    [SerializeField] private string terrainsFolder = "Assets/DB/Terrenos";


    // Editor-only: Auto load TerrainProfiles from folder
    #if UNITY_EDITOR
    private void OnValidate()
    {
        AutoLoadTerrainProfilesFromFolder();
    }

    [ContextMenu("AutoLoad TerrainProfiles (Assets/DB/Terrenos)")]
    private void AutoLoadTerrainProfilesFromFolder()
    {
        if (string.IsNullOrWhiteSpace(terrainsFolder)) return;

        string[] guids = AssetDatabase.FindAssets("t:TerrainProfile", new[] { terrainsFolder });

        if (TerrainProfiles == null)
            TerrainProfiles = new List<TerrainProfile>();
        else
            TerrainProfiles.Clear();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<TerrainProfile>(path);
            if (asset != null)
                TerrainProfiles.Add(asset);
        }

        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
    }
    #endif

    // Inicializa os dicionários de custo
    void Awake()
    {
        // Singleton: Uma instância global
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        int count = (TerrainProfiles == null) ? 0 : TerrainProfiles.Count;
        Debug.Log($"[TerrainManager] TerrainProfiles.Count = {count}");

        if (TerrainProfiles != null)
        {
            foreach (var def in TerrainProfiles)
            {
                if (def == null) { Debug.Log("[TerrainManager] def NULL"); continue; }
                Debug.Log($"[TerrainManager] {def.terrainName} tile={(def.tile ? def.tile.name : "NULL")} qp={(def.positionProfile ? def.positionProfile.qualityPoints : -999)} def={(def.positionProfile ? def.positionProfile.defenseBonus : -999)}");
            }
        }

        InitializeCostMap();
    }


    void InitializeCostMap()
{
    categoryMap.Clear();
    defenseBonusMap.Clear();
    qualityPointsMap.Clear();

    int count = (TerrainProfiles == null) ? 0 : TerrainProfiles.Count;
    Debug.Log($"[Terrain] Loaded profiles: {count}");

    if (TerrainProfiles == null) return;

    foreach (var def in TerrainProfiles)
    {
        if (def == null || def.tile == null) continue;

        int defBonus = 0;
        int qp = 0;

        if (def.positionProfile != null)
        {
            defBonus = def.positionProfile.defenseBonus;
            qp = def.positionProfile.qualityPoints;
        }

        // Categoria (movimento)
        categoryMap[def.tile] = def.category;

        // DPQ (defesa/qualidade)
        defenseBonusMap[def.tile] = defBonus;
        qualityPointsMap[def.tile] = qp;

        // debug (opcional)
        Debug.Log($"[Terrain] {def.terrainName} tile={def.tile.name} qp={qp} def={defBonus}");
    }
}



    // A LÃ“GICA DE OURO: Recebe o tile E o tipo da unidade
    // FunÃ§Ã£o que o Pathfinding vai chamar
    public int GetMovementCost(UnityEngine.Vector3Int cellPos, UnitType unitType)
    {
        TileBase tile = gameBoard.GetTile(cellPos);
        if (tile == null) return 999; // Fora do mapa

        // Se nÃ£o configurou, assume PlanÃ­cie (Custo 1)
        if (!categoryMap.ContainsKey(tile)) return 1;

        TerrainCategory terrain = categoryMap[tile];

        // --- CLASSIFICAÃ‡ÃƒO DA UNIDADE ---
        bool isAir = (unitType == UnitType.JetFighter || unitType == UnitType.Helicopter || unitType == UnitType.Plane);
        bool isSea = (unitType == UnitType.Ship || unitType == UnitType.Sub);
        bool isInfantry = (unitType == UnitType.Infantry);
        // --------------------------------

        switch (terrain)
        {
            case TerrainCategory.Plain:
                // PlanÃ­cie, Praia, Asfalto: Custo padrÃ£o para todos.
                return 1; 

            case TerrainCategory.Forest:
                // AÃ©reo e MarÃ­timo ignoram a floresta (custo 1)
                if (isAir || isSea) return 1;
                
                // Terrestre aplica a regra: Infantaria sofre menos (1), VeÃ­culos sofrem mais (2)
                return isInfantry ? 1 : 2;

            case TerrainCategory.Mountain:
                // AÃ©reo ignora, MarÃ­timo bloqueia
                if (isAir) return 1;
                if (isSea) return 99;
                
                // Terrestre aplica a regra: Infantaria (2), VeÃ­culos (6)
                return isInfantry ? 2 : 6;

            case TerrainCategory.Water:
                // Apenas unidades marÃ­timas e aÃ©reas podem entrar (Custo 1)
                if (isAir || isSea) return 1;
                return 99; // Bloqueio total para Terrestres
                
            case TerrainCategory.Obstacle:
                return 99; // Muro, bloqueia todos.

            default:
                return 1;
        }
    }

    public int GetDefenseBonus(Vector3Int cellPos)
    {
        TileBase tile = gameBoard.GetTile(cellPos);
        if (tile == null) return 0;

        if (defenseBonusMap.TryGetValue(tile, out int bonus))
            return bonus;

        return 0;
    }

    public int GetQualityPoints(Vector3Int cellPos)
    {
        TileBase tile = gameBoard.GetTile(cellPos);
        if (tile == null) return 0;

        if (qualityPointsMap.TryGetValue(tile, out int qp))
            return qp;

        return 0;
    }

}


