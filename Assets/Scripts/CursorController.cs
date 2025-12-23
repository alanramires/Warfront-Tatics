using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class CursorController : MonoBehaviour
{
    [Header("Referências")]
    public Grid mainGrid; 

    [Header("Câmera")]
    public CameraController cameraController;


    [Header("Audio SFX")]
    public AudioClip sfxCursor;
    public AudioClip sfxConfirm;
    public AudioClip sfxCancel;
    public AudioClip sfxError; 
    public AudioClip sfxDone; 
    private AudioSource audioSource;
    public AudioClip sfxBeep;

    [Header("Audio Movimento")] // <--- NOVO
    public AudioClip sfxMarch;
    public AudioClip sfxVehicle;


    [Header("Estado")]
    public Vector3Int currentCell = new Vector3Int(0, 0, 0); 
    
    private UnitMovement selectedUnit = null; 
    private List<Vector3Int> navigationLimit = null; 

    void Start() 
    {
        audioSource = GetComponent<AudioSource>();
        // Inicializa a posição do cursor na célula atual
        transform.position = mainGrid.CellToWorld(currentCell);

        if (cameraController == null && Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }
    }
    
    // --- TOCADORES DE SOM ---
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    public void PlayError()
    {
        PlaySFX(sfxError);
    }
        public void PlayCursor()
    {
        PlaySFX(sfxCursor);
    }

        public void PlayConfirm()
    {
        PlaySFX(sfxConfirm);
    }
   

    public void StartMoveSound(UnitType unitType)
    {
        // Nota: Assumimos que sfxMarch e sfxVehicle estão declarados e configurados.
        AudioClip clipToPlay = (unitType == UnitType.Infantry) ? sfxMarch : sfxVehicle;

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.loop = true; // Essencial para o som se repetir
            audioSource.Play();
        }
    }

    public void StopMoveSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
    }
    
    // -----------------------------------------

    public void LockMovement(List<Vector3Int> allowedTiles)
    {
        navigationLimit = allowedTiles;
    }

    public void UnlockMovement()
    {
        navigationLimit = null;
    }

    public void ClearSelection()
    {
        selectedUnit = null;
        UnlockMovement();
    }

    void Update()
    {
        // 1. INPUT INTELIGENTE
        Vector3Int newCell = currentCell; 
        bool moveInputDetected = false;

        // --- MOVIMENTO HORIZONTAL (Direto) ---
        if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            newCell.x += 1;
            moveInputDetected = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) 
        {
            newCell.x -= 1;
            moveInputDetected = true;
        }
        
        // --- MOVIMENTO VERTICAL (Inteligente/Smart ZigZag) ---
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            newCell = GetSmartVerticalMove(currentCell, 1); // 1 = Cima
            moveInputDetected = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            newCell = GetSmartVerticalMove(currentCell, -1); // -1 = Baixo
            moveInputDetected = true;
        }

        // VALIDAÇÃO FINAL
        // Se houve input e o destino calculado é válido...
        if (moveInputDetected && IsValidMove(newCell))
        {
            // Move!
            Vector3 oldPos = transform.position;
            transform.position = mainGrid.CellToWorld(newCell);
            currentCell = newCell; 
            PlaySFX(sfxCursor);

            cameraController.AdjustCameraForCursor(transform.position);

        }

        // 2. AÇÕES (ENTER / ESC / TAB / ESPAÇO)
        if (Input.GetKeyDown(KeyCode.Return)) HandleEnterKey();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySFX(sfxCancel);
            if (selectedUnit != null) selectedUnit.HandleCancelInput();
        }

        // TAB: navega entre unidades do time atual que ainda não agiram
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Shift + Tab = volta na lista
            int direction = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? -1 : 1;
            CycleBetweenUnits(direction);
        }

        /* M: na fase de menu pós-movimento, escolhe "Apenas mover" (não atacar)
        if (Input.GetKeyDown(KeyCode.M))
        {
            TrySkipAttackAndFinishTurn();
        }*/
        
        // ESPAÇO
        if (Input.GetKeyDown(KeyCode.Space))
        {
            selectedUnit?.stateManager?.ProcessSpace();
        }


    }

    // --- HELPER: Checa se posso ir para lá ---
    bool IsValidMove(Vector3Int target)
    {
        // Se não tem trava, qualquer lugar é "válido" para o cursor andar
        if (navigationLimit == null) return true;
        
        // Se tem trava, só é válido se estiver na lista verde
        return navigationLimit.Contains(target);
    }

    // --- A LÓGICA MÁGICA: Tenta os dois vizinhos verticais ---
    Vector3Int GetSmartVerticalMove(Vector3Int current, int directionY)
    {
        // Em um grid "Pointed Top" (Topo Pontudo), mover Y sempre altera o X também.
        // As coordenadas dependem se a linha é PAR ou ÍMPAR.
        
        bool isOddRow = current.y % 2 != 0; 
        
        // Candidatos a vizinhos (Esquerda e Direita na linha de cima/baixo)
        int offsetLeft = isOddRow ? 0 : -1;
        int offsetRight = isOddRow ? 1 : 0;

        Vector3Int optionA = new Vector3Int(current.x + offsetRight, current.y + directionY, 0); // Direita-Vertical
        Vector3Int optionB = new Vector3Int(current.x + offsetLeft,  current.y + directionY, 0); // Esquerda-Vertical

        // LÓGICA DE DECISÃO:
        
        // 1. Tenta a Opção A (Padrão)
        if (IsValidMove(optionA)) return optionA;

        // 2. Se a A falhou, tenta a Opção B
        if (IsValidMove(optionB)) return optionB;

        // 3. Se as duas falharam, retorna a própria célula (não move)
        // OU retorna a Opção A padrão para fazer o cursor "bater" na parede visualmente
        return optionA; 
    }

    // HANDLE ENTER
    void HandleEnterKey()
    {
        if (selectedUnit != null)
        {
            selectedUnit.TryToggleSelection(currentCell);
            return;
        }

        UnitMovement unitUnderCursor = FindUnitAt(currentCell);

        if (unitUnderCursor != null)
        {
            // Se a unidade já agiu (Finished) ou é Inimiga...
            if (unitUnderCursor.isFinished || unitUnderCursor.teamId != 0)
            {
                // MUDANÇA: Toca o Beep em vez do Erro (Feedback mais suave)
                PlaySFX(sfxBeep); 
            }
            else
            {
                PlaySFX(sfxConfirm);
            }

            // Seleciona a unidade
            selectedUnit = unitUnderCursor;
            selectedUnit.TryToggleSelection(currentCell);
        }
    }


    UnitMovement FindUnitAt(Vector3Int cellPos)
    {
        var allUnits = UnitMovement.All;
        foreach (var unit in allUnits)
        {
            if (unit.currentCell == cellPos) return unit;
        }
        return null;
    }

    // Escolhe a opção "Apenas mover" no menu pós-movimento
    void TrySkipAttackAndFinishTurn()
    {
        if (selectedUnit == null) return;
        if (selectedUnit.stateManager == null) return;
       // if (selectedUnit.stateManager.currentState != TurnState.MenuOpen) return;

        Debug.Log("➡️ Jogador escolheu: APENAS MOVER. Encerrando turno sem atacar.");
        selectedUnit.FinishTurn();
        ClearSelection();
    }


    // Navega pelas unidades do time atual que ainda não finalizaram o turno
    void CycleBetweenUnits(int direction)
    {
        // direction: +1 = próximo, -1 = anterior

        // Cancela qualquer seleção atual para evitar estados estranhos
        if (selectedUnit != null)
        {
            selectedUnit.HandleCancelInput();
        }
        ClearSelection();

        // Coleta todas as unidades válidas na cena
        var allUnits = UnitMovement.All;
        List<UnitMovement> candidates = new List<UnitMovement>();

        int currentTeamId = 0; // TODO: integrar com sistema de turnos futuramente

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            if (unit.teamId != currentTeamId) continue;
            if (unit.isFinished) continue; // já agiu nesse turno
            candidates.Add(unit);
        }

        if (candidates.Count == 0)
        {
            PlayError();
            return;
        }

        // Ordena por linha (Y) e coluna (X) pra ter uma ordem previsível
        candidates.Sort((a, b) =>
        {
            int cmpY = a.currentCell.y.CompareTo(b.currentCell.y);
            if (cmpY != 0) return cmpY;
            return a.currentCell.x.CompareTo(b.currentCell.x);
        });

        // Descobre se o cursor já está sobre uma das unidades candidatas
        int currentIndex = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].currentCell == currentCell)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex;
        if (currentIndex == -1)
        {
            // Se não estiver em nenhuma, começa do início ou do fim da lista
            nextIndex = (direction > 0) ? 0 : candidates.Count - 1;
        }
        else
        {
            // Avança ou volta com wrap-around
            nextIndex = currentIndex + direction;
            if (nextIndex < 0) nextIndex = candidates.Count - 1;
            if (nextIndex >= candidates.Count) nextIndex = 0;
        }

        UnitMovement target = candidates[nextIndex];
        Vector3 oldPos = transform.position;
        currentCell = target.currentCell;
        transform.position = mainGrid.CellToWorld(currentCell);
        PlaySFX(sfxCursor);

        cameraController.AdjustCameraForCursor(transform.position);

    }




}