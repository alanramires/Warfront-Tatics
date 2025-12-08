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

    [Header("Combustível")]
    public int currentFuel;
    private int pendingCost = 0; // Quanto gastou neste movimento (ainda não confirmado)

    [Header("Sistema de Combate")]
    public GameObject masterProjectilePrefab;

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

    [Header("Estado do Turno")]
    public bool isFinished = false; // Agora aparece no Inspector como um checkbox!
        
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
            // INICIALIZA O TANQUE
            currentFuel = data.maxFuel;

            switch (teamId)
            {
                // MUDANÇA AQUI: Usando as novas cores suaves
                case 0: 
                    teamColor = GameColors.TeamGreen; 
                    specificSkin = data.spriteGreen; 
                    break;
                case 1: 
                    teamColor = GameColors.TeamRed; 
                    specificSkin = data.spriteRed; 
                    break;
                case 2: 
                    teamColor = GameColors.TeamBlue; 
                    specificSkin = data.spriteBlue; 
                    break;    
                case 3: 
                    teamColor = GameColors.TeamYellow; 
                    specificSkin = data.spriteYellow; 
                    break;  
                default: 
                    teamColor = Color.white; 
                    break;
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
                hud.SetVisuals(teamId, spriteRenderer.color);  
                // ATUALIZA A BARRA INICIAL
                hud.UpdateFuel(currentFuel, data.maxFuel);

               
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

     // --- NOVO: SINCRONIA EM TEMPO REAL (GOD MODE) ---
        void Update()
        {
            // Isso garante que se você mexer no Inspector, o HUD obedece na hora!
            if (hud != null)
            {
                hud.SetLockState(isFinished); // Atualiza o Cadeado
                hud.UpdateHP(currentHP);      // Atualiza o HP
                if (data != null) hud.UpdateFuel(currentFuel, data.maxFuel);
            }
        }
    public void TryToggleSelection(Vector3Int cursorPosition)
    {
        if (isMoving) return; 

        // ---------------------------------------------------------------
        // 1. O FIX DO TRAVAMENTO (Unidade Já Agiu)
        // ---------------------------------------------------------------
        if (isFinished)
        {
            // Se o clique foi EXATAMENTE nesta unidade travada
            if (cursorPosition == currentCell)
            {
                Debug.Log("Unidade já agiu. Cursor liberado.");
                
                if (boardCursor)
                {
                    // Toca o som de "negado" (opcional)
                    boardCursor.PlayError(); 
                    
                    // --- O PULO DO GATO ---
                    // Força o cursor a esquecer essa unidade imediatamente.
                    // Isso impede que ele fique "preso" esperando movimento.
                    boardCursor.ClearSelection(); 
                }
            }
            return; // Aborta e não deixa selecionar para andar
        }

        // ---------------------------------------------------------------
        // 2. LÓGICA DE MOVIMENTO NORMAL (Se não estiver finished)
        // ---------------------------------------------------------------

        // A. SELEÇÃO INICIAL (Primeiro clique)
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

        // B. CONFIRMAÇÃO (Segundo clique no mesmo lugar)
        if (isPreMoved)
        {
            ConfirmMove(); 
            return;
        }

        // C. TENTATIVA DE MOVIMENTO (Clicou num quadrado azul ou fora)
        if (isSelected && !isPreMoved)
        {
            // Clicou nela mesma para esperar
            if (cursorPosition == currentCell) 
            {
                if (boardCursor) boardCursor.PlaySFX(boardCursor.sfxConfirm);
                isPreMoved = true;
                if (boardCursor) boardCursor.LockMovement(new List<Vector3Int> { currentCell });
            }
            // Clicou num quadrado válido
            else if (validMoveTiles.Contains(cursorPosition)) 
            {
                AudioClip moveClip = (data.unitType == UnitType.Infantry) ? boardCursor.sfxMarch : boardCursor.sfxVehicle;
                if (boardCursor) boardCursor.PlaySFX(moveClip); 
                
                StartCoroutine(MoveRoutine(cursorPosition));
            }
            // Clicou fora (Cancelamento)
            else
            {
                CancelSelectionComplete(); // Solta a unidade
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
        
        // DESLIGA O CADEADO
        if (hud != null) hud.SetLockState(false); // <--- AQUI
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
        int range = GetEffectiveRange();
        UnitType type = data != null ? data.unitType : UnitType.Infantry;
        List<Vector3Int> path = Pathfinding.GetPathTo(currentCell, destination, range, GetMovementBlockers(), type);

        // --- CÁLCULO DE CUSTO USANDO SEU MANAGER ---
        pendingCost = 0;
        
        foreach (Vector3Int tilePos in path)
        {
            if (tilePos == currentCell) continue; // Não paga para ficar parado

            // Chama o SEU TerrainManager (que já tem a lógica de Forest=2, Mountain=6)
            if (TerrainManager.Instance != null)
            {
                pendingCost += TerrainManager.Instance.GetMovementCost(tilePos, type);
            }
            else
            {
                // Fallback de segurança (se esqueceu de criar o manager na cena)
                pendingCost += 1; 
            }
        }

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
        int range = GetEffectiveRange();
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
        // --- 1. COBRANÇA DO COMBUSTÍVEL (ISSO QUE FALTA) ---
        if (pendingCost > 0)
        {
            currentFuel -= pendingCost; // Desconta do tanque
            if (currentFuel < 0) currentFuel = 0; // Não deixa negativo
            
            // Chama o HUD para atualizar a barra
            if (hud != null && data != null)
            {
                hud.UpdateFuel(currentFuel, data.maxFuel);
            }

            Debug.Log($"Pagou gasolina: {pendingCost}. Sobrou: {currentFuel}");
            
            pendingCost = 0; // Zera a conta pra não cobrar 2x
        }

        // 2. FINALIZAÇÃO DO TURNO
        isSelected = false;
        isPreMoved = false;
        isFinished = true; // Marca que já agiu
        
        StopAllCoroutines();     

        // Fix do Fantasma (Garante visibilidade)
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // Liga o cadeado
        if (hud != null) hud.SetLockState(true);   

        ClearRange(); 
        
        if (boardCursor) 
        {
            boardCursor.PlaySFX(boardCursor.sfxDone); 
            boardCursor.ClearSelection(); 
        }
        Debug.Log("Movimento Concluido");
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

    int GetEffectiveRange()
    {
        if (data == null) return 0;
        
        // Retorna o MENOR valor. 
        // Se Speed=6 e Gas=2 -> Retorna 2.
        // Se Speed=6 e Gas=50 -> Retorna 6.
        return Mathf.Min(data.moveRange, currentFuel);
    }

    void ShowRange()
    {
        if (rangeTilemap == null) return;

        // INTEGRAÇÃO: Lê os dados da ficha
       int range = GetEffectiveRange(); // <--- O desenho agora respeita a gasolina!

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