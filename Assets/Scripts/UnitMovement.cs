using UnityEngine;
using UnityEngine.Tilemaps; 
using System.Collections;   
using System.Collections.Generic; 

public class UnitMovement : MonoBehaviour 
{
    [Header("DADOS DA UNIDADE (A FICHA)")]
    public UnitData data; // <--- VARIÁVEL NECESSÁRIA ADICIONADA AQUI

    [Header("Interface")]
    public UnitHUD hud; // <--- Arraste o Canvas/Script aqui depois

    [Header("Estado de Combate (Runtime)")]
    public int currentHP;        // Quantos soldados restam
    
    // Lista de armas DESTE soldado específico (com munição gasta individualmente)
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

    // Estados
    private bool isSelected = false; 
    private bool isMoving = false; 
    private bool isPreMoved = false; 
    private bool isFinished = false; 
    
    private Vector3Int posicaoOriginal; 
    private SpriteRenderer spriteRenderer;
    private Color originalColor; 
    
    // Listas de Controle
    private List<Vector3Int> validMoveTiles = new List<Vector3Int>();
    private List<Vector3Int> navigableTiles = new List<Vector3Int>(); 

    void Start()
    {
        // ---------------------------------------------------------
        // 1. AUTO-CONEXÃO (Busca referências necessárias)
        // ---------------------------------------------------------
        if (boardCursor == null) boardCursor = FindFirstObjectByType<CursorController>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 

        if (rangeTilemap == null)
        {
            GameObject mapObj = GameObject.Find("RangeMap");
            if (mapObj != null) rangeTilemap = mapObj.GetComponent<Tilemap>();
        }

        // ---------------------------------------------------------
        // 2. CONFIGURAÇÃO BASEADA NA FICHA (DATA)
        // ---------------------------------------------------------
        if (data != null)
        {
            // --- A. CONFIGURAÇÃO VISUAL (SKIN & COR) ---
            Color teamColor = Color.white;
            Sprite specificSkin = null;

            switch (teamId)
            {
                case 0: teamColor = Color.green; specificSkin = data.spriteGreen; break;
                case 1: teamColor = Color.red; specificSkin = data.spriteRed; break;
                case 2: teamColor = Color.blue; specificSkin = data.spriteBlue; break;    
                case 3: teamColor = Color.yellow; specificSkin = data.spriteYellow; break;  
                default: teamColor = Color.white; break;
            }

            // Define o Sprite (Específico ou Padrão)
            if (specificSkin != null) spriteRenderer.sprite = specificSkin;
            else spriteRenderer.sprite = data.spriteDefault;

            // Define a Tinta
            spriteRenderer.color = teamColor;
            
            // Define o Flip (Direção do olhar)
            if (teamId == 1 || teamId == 3) spriteRenderer.flipX = true; // 1 e 3 olham p/ esquerda
            else spriteRenderer.flipX = false;

            // --- B. DADOS DE COMBATE (HP & ARSENAL) ---
            
            // Inicializa HP do Esquadrão
            currentHP = data.maxHP;

            // Inicializa o Arsenal (Copia da ficha para a memória da unidade)
            myWeapons.Clear();
            foreach (var w in data.weapons)
            {
                myWeapons.Add(w); 
                // Importante: Adicionamos uma cópia. Gastar munição aqui não altera o UnitData original.
            }

            // LIGA O HUD
            if (hud != null)
            {
                hud.UpdateHP(currentHP);       
                hud.SetupWeapons(myWeapons);   
                
                // --- NOVO: PASSA A TINTA PRO HUD ---
                // Usa a mesma cor que pintou o soldado (spriteRenderer.color)
                hud.SetTeamColor(spriteRenderer.color); 
            }
        }
        else
        {
            Debug.LogError($"ERRO CRÍTICO: Unidade {name} não tem Ficha de Dados (UnitData) atribuída!");
        }

        // ---------------------------------------------------------
        // 3. FINALIZAÇÃO DE ESTADO
        // ---------------------------------------------------------
        originalColor = spriteRenderer.color; // Salva a cor final para o efeito de piscar
        
        if (lockIcon != null) lockIcon.SetActive(false);

        // 4. ALINHAMENTO NO GRID
        if (boardCursor != null && boardCursor.mainGrid != null)
        {
            Vector3 worldPos = boardCursor.mainGrid.CellToWorld(currentCell);
            worldPos.y += visualOffset;
            transform.position = worldPos;
        }
    }
    public void TryToggleSelection(Vector3Int cursorPosition)
    {
        if (isMoving || isFinished) return; 

        // 1. SELEÇÃO INICIAL
        if (!isSelected && !isPreMoved)
        {
            if (cursorPosition == currentCell)
            {
                isSelected = true;
                posicaoOriginal = currentCell; 
                
                StartCoroutine("BlinkRoutine");
                ShowRange(); 
                
                if (boardCursor) boardCursor.LockMovement(navigableTiles); 
                
                Debug.Log($"Unidade Time {teamId} Selecionada!");
            }
            return;
        }

        // 2. CONFIRMAÇÃO FINAL
        if (isPreMoved)
        {
            ConfirmMove(); 
            return;
        }

        // 3. INICIA MOVIMENTO
        if (isSelected && !isPreMoved)
        {
            if (cursorPosition == currentCell) 
            {
                if (boardCursor) boardCursor.PlaySFX(boardCursor.sfxConfirm);
                isPreMoved = true;
                if (boardCursor) boardCursor.LockMovement(new List<Vector3Int> { currentCell });
            }
            else if (validMoveTiles.Contains(cursorPosition)) 
            {
                // 1. Determina qual som de unidade tocar
                AudioClip moveClip = (data.unitType == UnitType.Infantry) ? boardCursor.sfxMarch : boardCursor.sfxVehicle;

                // 2. Toca o som da UNIDADE (Marcha/Veículo) em vez do som genérico de UI
                if (boardCursor) boardCursor.PlaySFX(moveClip); 
                
                // 3. Inicia o movimento
                StartCoroutine(MoveRoutine(cursorPosition));
            }
            else
            {
                if (boardCursor) boardCursor.PlayError(); 
                Debug.Log("Destino Inválido.");
            }
        }
    }

    public void HandleCancelInput()
    {
        if (isMoving) return;
        if (isPreMoved) StartCoroutine(UndoMoveRoutine());
        else if (isSelected) CancelSelectionComplete();
    }

    public void ResetTurn()
    {
        isFinished = false;
        spriteRenderer.color = originalColor;
        if (lockIcon != null) lockIcon.SetActive(false);
    }

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

    // --- COROUTINE DE MOVIMENTO ---
    IEnumerator MoveRoutine(Vector3Int destination)
    {
        isMoving = true; 
        if (boardCursor) boardCursor.UnlockMovement();

        // INTEGRAÇÃO: Lê os dados da ficha
        int range = data != null ? data.moveRange : 3;
        UnitType type = data != null ? data.unitType : UnitType.Infantry;

        List<Vector3Int> path = Pathfinding.GetPathTo(currentCell, destination, range, GetMovementBlockers(), type);

        for (int i = 1; i < path.Count; i++)
        {
            // ... (Lógica de MoveTowards e atualização de currentCell) ...
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

        isMoving = false; 
        isPreMoved = true;
        if (boardCursor) boardCursor.LockMovement(new List<Vector3Int> { currentCell });
    }

    IEnumerator UndoMoveRoutine()
    {
        isMoving = true;
        isPreMoved = false; 

        // INTEGRAÇÃO: Lê os dados da ficha
        int range = data != null ? data.moveRange : 3;
        UnitType type = data != null ? data.unitType : UnitType.Infantry;

        List<Vector3Int> path = Pathfinding.GetPathTo(currentCell, posicaoOriginal, range, GetMovementBlockers(), type);

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

        isMoving = false;
        ShowRange();
        if (boardCursor) boardCursor.LockMovement(navigableTiles);
    }

    void ConfirmMove()
    {
        isSelected = false;
        isPreMoved = false;
        
        StopAllCoroutines();
        
        spriteRenderer.color = originalColor; 
        if (lockIcon != null) lockIcon.SetActive(false);

        ClearRange(); 
        
        if (boardCursor) 
        {
            boardCursor.PlaySFX(boardCursor.sfxDone); 
            boardCursor.ClearSelection(); 
        }

        Debug.Log("Movimento Concluído.");
    }

    void CancelSelectionComplete()
    {
        isSelected = false;
        isPreMoved = false;
        StopAllCoroutines();
        spriteRenderer.color = originalColor;
        ClearRange(); 
        if (boardCursor) boardCursor.ClearSelection(); 
    }

    void ShowRange()
    {
        if (rangeTilemap == null) return;

        // INTEGRAÇÃO: Lê os dados da ficha
        int range = data != null ? data.moveRange : 3;
        UnitType type = data != null ? data.unitType : UnitType.Infantry;

        navigableTiles = Pathfinding.GetReachableTiles(
            currentCell, 
            range, 
            GetMovementBlockers(), 
            new HashSet<Vector3Int>(), 
            type
        );

        validMoveTiles = Pathfinding.GetReachableTiles(
            currentCell, 
            range, 
            GetMovementBlockers(), 
            GetStoppingBlockers(), 
            type
        );
        
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
}