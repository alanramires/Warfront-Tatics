using UnityEngine;
using UnityEngine.Tilemaps; 
using System.Collections;   
using System.Collections.Generic; 

[RequireComponent(typeof(TurnStateManager))] // Garante que o cérebro existe
public partial class UnitMovement : MonoBehaviour 
{
    public static readonly List<UnitMovement> All = new List<UnitMovement>();

    [Header("DADOS DA UNIDADE (A FICHA)")]
    public UnitData data;

    [Header("Interface")]
    public UnitHUD hud;
    public TurnStateManager stateManager; // Link com o Cérebro

    [Header("Combustível")]
    public int currentFuel;
    private int pendingCost = 0;

    // ✅ expõe só leitura pro painel/TurnStateManager
    public int PendingCost => pendingCost;

    [Header("Undo History")]
    public List<Vector3Int> lastPathTaken = new List<Vector3Int>(); 
    
    public int lastMoveCost = 0;


    [Header("Sistema de Combate")]
    public GameObject masterProjectilePrefab;
    public int currentHP;        
    public List<WeaponConfig> myWeapons = new List<WeaponConfig>();

    [Header("Configurações de Time")]
    public int teamId = 0; 

    [Header("Referências Gerais")]
    public CursorController boardCursor; 
    public Tilemap rangeTilemap; 
    public Vector3Int currentCell = new Vector3Int(0, 0, 0); 

    [Header("Assets")]
    public GameObject lockIcon; 
    public TileBase rangeTile;  

    [Header("Movimento")]
    public float moveSpeed = 20f; 
    public float visualOffset = 0f; 

    [Header("Estado do Turno")]
    public bool isFinished = false; 
        
    // Necessário ser public para o Undo do TurnStateManager ler
    public Vector3Int posicaoOriginal; 

    private SpriteRenderer spriteRenderer;
    private Color originalColor; 
    
    private List<Vector3Int> validMoveTiles = new List<Vector3Int>();
    public List<Vector3Int> navigableTiles = new List<Vector3Int>(); 

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Start()
    {
        // 1. AUTO-CONEXÃO
        if (boardCursor == null) boardCursor = FindFirstObjectByType<CursorController>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        stateManager = GetComponent<TurnStateManager>(); 

        if (rangeTilemap == null)
        {
            GameObject mapObj = GameObject.Find("RangeMap");
            if (mapObj != null) rangeTilemap = mapObj.GetComponent<Tilemap>();
        }

        // 2. CONFIGURAÇÃO BASEADA NA FICHA
        if (data != null)
        {
            Color teamColor = TeamUtils.GetColor(teamId);
            Sprite specificSkin = null;
            currentFuel = data.maxFuel;

            switch (teamId)
            {
                case TeamUtils.Green:  specificSkin = data.spriteGreen; break;
                case TeamUtils.Red:    specificSkin = data.spriteRed; break;
                case TeamUtils.Blue:   specificSkin = data.spriteBlue; break;
                case TeamUtils.Yellow: specificSkin = data.spriteYellow; break;
            }

            if (specificSkin != null) spriteRenderer.sprite = specificSkin;
            else spriteRenderer.sprite = data.spriteDefault;

            spriteRenderer.color = teamColor;
            
            if (teamId == 1 || teamId == 3) spriteRenderer.flipX = true; 
            else spriteRenderer.flipX = false;

            currentHP = data.maxHP;

            myWeapons.Clear();
            foreach (var w in data.weapons) myWeapons.Add(w); 

            if (hud != null)
            {
                hud.UpdateHP(currentHP);       
                hud.SetupWeapons(myWeapons);  
                hud.SetVisuals(teamId, spriteRenderer.color);  
                hud.UpdateFuel(currentFuel, data.maxFuel);
            }
        }

        originalColor = spriteRenderer.color; 
        if (lockIcon != null) lockIcon.SetActive(false);

        if (boardCursor != null && boardCursor.mainGrid != null)
        {
            Vector3 worldPos = boardCursor.mainGrid.GetCellCenterWorld(currentCell);
            worldPos.y += visualOffset;
            transform.position = worldPos;
        }

        // Inicializa o estado no Manager
        if(stateManager) stateManager.SetState(TurnState.None);
    }

    void Update()
    {
        if (hud != null)
        {
            hud.SetLockState(isFinished);
            hud.UpdateHP(currentHP);      
            if (data != null) hud.UpdateFuel(currentFuel, data.maxFuel);
        }

        if (!isFinished && stateManager.currentState == TurnState.Finished)
        {
            Debug.Log("⚡ GOD MODE DETECTADO: Ressuscitando unidade...");
            ResetTurn(); // Força o reset completo (Estado -> None, Cor -> Original)
        }
    }

    public void FinishTurn()
    {
        isFinished = true;
        if (stateManager != null) stateManager.SetState(TurnState.Finished);

        pendingCost = 0; // Agora sim zera o custo
        StopAllCoroutines();     
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        if (hud != null) hud.SetLockState(true);   
        ClearRange(); 
        
        if (boardCursor) 
        {
            boardCursor.PlaySFX(boardCursor.sfxDone); 
            boardCursor.ClearSelection(); 
        }
    }

    public void ResetTurn()
    {
        isFinished = false;
        spriteRenderer.color = originalColor;
        if (hud != null) hud.SetLockState(false);
        if(stateManager) stateManager.SetState(TurnState.None);
    }

}