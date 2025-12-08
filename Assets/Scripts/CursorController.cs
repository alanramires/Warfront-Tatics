using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class CursorController : MonoBehaviour
{
    [Header("Referências")]
    public Grid mainGrid; 

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
        transform.position = mainGrid.CellToWorld(currentCell);
    }
    
    // --- MUDANÇA AQUI: Adicionado "public" ---
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    public void PlayError()
    {
        PlaySFX(sfxError);
    }
   
   // --- REINSERINDO: LÓGICA DE SOM EM LOOP ---
    // (O UnitMovement depende disso para tocar som durante a caminhada)

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
            transform.position = mainGrid.CellToWorld(newCell);
            currentCell = newCell; 
            PlaySFX(sfxCursor);
        }

        // 2. AÇÕES (ENTER / ESC / TAB - IGUAIS AO ANTERIOR)
        if (Input.GetKeyDown(KeyCode.Return)) HandleEnterKey();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySFX(sfxCancel);
            if (selectedUnit != null) selectedUnit.HandleCancelInput();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
             var allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);
             foreach(var u in allUnits) u.ResetTurn();
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
        UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit.currentCell == cellPos) return unit;
        }
        return null;
    }
}