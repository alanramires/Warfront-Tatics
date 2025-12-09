using UnityEngine;
using UnityEngine.Tilemaps; 
using System.Collections;   
using System.Collections.Generic; 

[RequireComponent(typeof(TurnStateManager))] // Garante que o cérebro existe
public class UnitMovement : MonoBehaviour 
{
    [Header("DADOS DA UNIDADE (A FICHA)")]
    public UnitData data;

    [Header("Interface")]
    public UnitHUD hud;
    public TurnStateManager stateManager; // Link com o Cérebro

    [Header("Combustível")]
    public int currentFuel;
    private int pendingCost = 0; 

    [Header("Undo History")]
    public List<Vector3Int> lastPathTaken = new List<Vector3Int>(); 
    private int lastMoveCost = 0; // Para guardar o custo antes de pagar

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
    private List<Vector3Int> navigableTiles = new List<Vector3Int>(); 

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
            Color teamColor = Color.white;
            Sprite specificSkin = null;
            currentFuel = data.maxFuel;

            switch (teamId)
            {
                case 0: teamColor = GameColors.TeamGreen; specificSkin = data.spriteGreen; break;
                case 1: teamColor = GameColors.TeamRed; specificSkin = data.spriteRed; break;
                case 2: teamColor = GameColors.TeamBlue; specificSkin = data.spriteBlue; break;    
                case 3: teamColor = GameColors.TeamYellow; specificSkin = data.spriteYellow; break;  
                default: teamColor = Color.white; break;
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
            Vector3 worldPos = boardCursor.mainGrid.CellToWorld(currentCell);
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

    // ========================================================================
    // 🕹️ INPUT DELEGADO (O Cérebro Decide)
    // ========================================================================

    public void TryToggleSelection(Vector3Int cursorPosition)
    {
        // O TurnStateManager assume o controle total aqui
        if (stateManager != null) 
        {
            stateManager.ProcessInteraction(cursorPosition);
        }
    }

    public void HandleCancelInput()
    {
        // O TurnStateManager assume o controle total aqui
        if (stateManager != null) 
        {
            stateManager.ProcessCancel();
        }
    }

    // ========================================================================
    // 🦾 COMANDOS PÚBLICOS (O Corpo Obedece o Cérebro)
    // ========================================================================

    // Chamado pelo Manager quando entra no estado Selected
    public void SelectUnit()
    {
        posicaoOriginal = currentCell;
        StartCoroutine("BlinkRoutine");
        ShowRange();
        if (boardCursor) boardCursor.LockMovement(navigableTiles);
    }

    // Chamado pelo Manager quando cancela seleção
    public void DeselectUnit()
    {
        ClearVisuals();
        StopCoroutine("BlinkRoutine");
        if (spriteRenderer) spriteRenderer.color = originalColor;
        if (boardCursor) boardCursor.ClearSelection();
    }

    // Chamado pelo Manager para mover
    public void StartPhysicalMove(Vector3Int destination)
    {
        AudioClip moveClip = (data.unitType == UnitType.Infantry) ? boardCursor.sfxMarch : boardCursor.sfxVehicle;
        if (boardCursor) boardCursor.PlaySFX(moveClip);
        StartCoroutine(MoveRoutine(destination));
    }

    public void MoveDirectlyToMenu()
    {
        if (boardCursor) boardCursor.PlaySFX(boardCursor.sfxConfirm);
        
        // Simula que "Já moveu" e chama o final
        if (boardCursor) boardCursor.LockMovement(new List<Vector3Int> { currentCell });
        OnMoveFinished(); 
    }

    public void StartUndoMove()
    {
        StartCoroutine(UndoMoveRoutine());
    }

    public void ClearVisuals()
    {
        ClearRange();
    }

    // Helper para o Manager saber se pode andar
    public bool IsValidMove(Vector3Int pos)
    {
        return validMoveTiles.Contains(pos);
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

    // ========================================================================
    // ⚙️ ROTINAS FÍSICAS E LÓGICA INTERNA (INTOCADAS)
    // ========================================================================

    HashSet<Vector3Int> GetMovementBlockers()
    {
        HashSet<Vector3Int> blockers = new HashSet<Vector3Int>();
        UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);
        foreach (UnitMovement unit in allUnits)
        {
            if (unit == this) continue;
            if (unit.teamId != this.teamId) blockers.Add(unit.currentCell);
        }
        return blockers;
    }

    HashSet<Vector3Int> GetStoppingBlockers()
    {
        HashSet<Vector3Int> blockers = new HashSet<Vector3Int>();
        UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);
        foreach (UnitMovement unit in allUnits)
        {
            if (unit == this) continue;
            if (unit.teamId == this.teamId) blockers.Add(unit.currentCell);
        }
        return blockers;
    }

    IEnumerator MoveRoutine(Vector3Int destination)
    {
        if (boardCursor) boardCursor.UnlockMovement();

        int range = GetEffectiveRange();
        UnitType type = data != null ? data.unitType : UnitType.Infantry;
        List<Vector3Int> path = Pathfinding.GetPathTo(currentCell, destination, range, GetMovementBlockers(), type);

        // --- GERAÇÃO DO PENDING COST (Onde o custo é calculado) ---
        pendingCost = 0;
        foreach (Vector3Int tilePos in path)
        {
            if (tilePos == currentCell) continue; 
            if (TerrainManager.Instance != null) pendingCost += TerrainManager.Instance.GetMovementCost(tilePos, type);
            else pendingCost += 1; 
        }

        // --- GRAVAÇÃO DO HISTÓRICO (AGORA ESTÁ CORRETO) ---
        lastPathTaken.Clear();
        lastPathTaken.AddRange(path); // Salva o caminho
        lastMoveCost = pendingCost; // Salva o custo para reembolso
        // ----------------------------------------------------

        for (int i = 1; i < path.Count; i++)
        {
            Vector3Int nextTile = path[i];
            Vector3 targetPos = boardCursor.mainGrid.CellToWorld(nextTile);
            targetPos.y += visualOffset; 

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                if (boardCursor) boardCursor.transform.position = transform.position; 
                yield return null; 
            }
            transform.position = targetPos;
            currentCell = nextTile; 
            if (boardCursor) boardCursor.currentCell = currentCell;
        }

        OnMoveFinished(); // Chama o Hub Final
    }

    IEnumerator UndoMoveRoutine()
    {
        // 1. REEMBOLSO DO CUSTO (Usando o valor pendente que foi pago em OnMoveFinished)
        currentFuel += pendingCost; 
        if (currentFuel > data.maxFuel) currentFuel = data.maxFuel;
        if (hud) hud.UpdateFuel(currentFuel, data.maxFuel);
        
        // Zera o custo pendente após o reembolso
        pendingCost = 0;

        // 2. VERIFICAÇÃO DE CAMINHO
        // Se a unidade se moveu (lista tem mais de 1 elemento), anima a volta
        if (lastPathTaken.Count > 1)
        {
            // Começa do penúltimo (o destino) até o primeiro (a origem)
            // i = lastPathTaken.Count - 2 é o tile que estava antes do destino final.
            for (int i = lastPathTaken.Count - 2; i >= 0; i--)
            {
                Vector3Int nextTile = lastPathTaken[i];
                Vector3 targetPos = boardCursor.mainGrid.CellToWorld(nextTile);
                targetPos.y += visualOffset; 

                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    if (boardCursor) boardCursor.transform.position = transform.position;
                    yield return null; 
                }
                transform.position = targetPos;
                currentCell = nextTile; // Unidade está voltando
                if (boardCursor) boardCursor.currentCell = currentCell;
            }
        }
        
        // 3. LIMPEZA FINAL
        lastPathTaken.Clear(); // Limpa o histórico
        
        // Volta para o estado Selecionado
        if(stateManager) stateManager.SetState(TurnState.Selected);
        
        ShowRange();
        if (boardCursor) boardCursor.LockMovement(navigableTiles);
    }

    // Renomeado de ConfirmMove para OnMoveFinished para padronizar
    void OnMoveFinished()
    {
        if (boardCursor) boardCursor.LockMovement(new List<Vector3Int> { currentCell });

        if (pendingCost > 0)
        {
            currentFuel -= pendingCost; 
            if (currentFuel < 0) currentFuel = 0; 
            if (hud != null && data != null) hud.UpdateFuel(currentFuel, data.maxFuel);
            
            // CORREÇÃO: Não zera o pendingCost, pois o Undo precisa da informação do custo!
        }

        Debug.Log("Movimento Concluido. Menu Aberto.");
        
        if (stateManager != null) stateManager.SetState(TurnState.MenuOpen);
    }

    int GetEffectiveRange()
    {
        if (data == null) return 0;
        return Mathf.Min(data.moveRange, currentFuel);
    }

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

    IEnumerator BlinkRoutine()
    {
        Color invisible = originalColor;
        invisible.a = 0f; 
        while (true)
        {
            spriteRenderer.color = invisible;
            yield return new WaitForSeconds(0.2f); 
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(1.0f); 
        }
    }

    // Helper para o Manager verificar se pode mover para este tile
    public bool IsValidDestination(Vector3Int pos)
    {
        // O Pathfinding já calculou que esta lista não contém tiles de aliados ou inimigos
        return validMoveTiles.Contains(pos); 
    }
}