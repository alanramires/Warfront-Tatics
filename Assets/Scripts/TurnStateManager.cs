using UnityEngine;

public class TurnStateManager : MonoBehaviour
{
    [Header("Estado Atual")]
    public TurnState currentState = TurnState.None;

    [Header("Referências")]
    public UnitMovement unit; 

    private void Start()
    {
        unit = GetComponent<UnitMovement>();
    }

    // ========================================================================
    // 🚀 AVANÇAR MARCHA (Enter / Clique)
    // ========================================================================
    public void ProcessInteraction(Vector3Int cursorPosition)
    {
        // CORREÇÃO CRÍTICA: Removi 'currentState == TurnState.Finished' daqui.
        // Agora podemos interagir com unidades finalizadas (para inspecionar).
        if (currentState == TurnState.Moving) return;

        switch (currentState)
        {
            // --- DEGRAU 0: NONE ---
            case TurnState.None:
                if (cursorPosition == unit.currentCell)
                {
                    if (unit.isFinished || unit.teamId != 0) 
                    {
                        SetState(TurnState.Inspected);
                        unit.ShowRange(); 
                    }
                    else 
                    {
                        SetState(TurnState.Selected);
                        unit.SelectUnit(); 
                    }
                }
                break;
            
            // --- NOVO: SE JÁ ESTÁ FINALIZADA ---
            // Se clicamos nela de novo, apenas garantimos que vá para Inspecionar
            case TurnState.Finished:
                if (cursorPosition == unit.currentCell)
                {
                    SetState(TurnState.Inspected);
                    unit.ShowRange();
                }
                break;

            // --- DEGRAU 1: SELECTED (Tenta mover) ---
        case TurnState.Selected:
            // 1. Clicou na PRÓPRIA UNIDADE -> Vai para o Menu
            if (cursorPosition == unit.currentCell)
            {
                unit.MoveDirectlyToMenu(); // Chama OnMoveFinished -> MenuOpen
            }
            else
            {
                // 2. Clicou em OUTRO LUGAR
                if (unit.IsValidDestination(cursorPosition)) 
                {
                    // DESTINO VÁLIDO: Inicia o movimento físico
                    SetState(TurnState.Moving); 
                    unit.StartPhysicalMove(cursorPosition);
                }
                else
                {
                    // DESTINO INVÁLIDO (Aliado, Inimigo, ou Terreno intransponível)
                    
                    // **CORREÇÃO: Toca som de erro e PERMANECE no estado 'Selected'.**
                    if (unit.boardCursor != null)
                    {
                        unit.boardCursor.PlayError(); // Toca o som de erro (sfxError)
                    }
                    
                    // Não há SetState() aqui. A função simplesmente retorna,
                    // mantendo o estado 'Selected' e a seleção ativa.
                }
            }
            break;
            // --- DEGRAU 3: MENU ---
            case TurnState.MenuOpen:
                // Lógica simulada de "Mirar com Enter"
                bool seMoveu = (unit.currentCell != unit.posicaoOriginal);
                if (unit.GetComponent<UnitAttack>().GetValidTargets(seMoveu).Count > 0)
                {
                    SetState(TurnState.Aiming);
                    Debug.Log("🎯 MIRA ATIVA.");
                }
                else
                {
                    Debug.Log("Sem alvos. Encerrando turno.");
                    unit.FinishTurn();
                }
                break;

            case TurnState.Aiming:
                SetState(TurnState.ConfirmTarget);
                break;

            case TurnState.ConfirmTarget:
                unit.FinishTurn();
                break;

            // Adicionei o caso Inspected aqui para garantir que se clicar fora, ele solta
            case TurnState.Inspected:
                ProcessCancel();
                break;
        }
    }

    // ========================================================================
    // 🔙 VOLTAR MARCHA (ESC)
    // ========================================================================
    public void ProcessCancel()
    {
        if (currentState == TurnState.Moving) return;

        switch (currentState)
        {
            case TurnState.Inspected:
                unit.ClearVisuals();
                SetState(TurnState.None);
                if (unit.boardCursor) unit.boardCursor.ClearSelection();
                break;

            case TurnState.Selected:
                unit.DeselectUnit();
                SetState(TurnState.None);
                break;
                
            case TurnState.MenuOpen:
                Debug.Log("Voltando (Undo)...");
                SetState(TurnState.Moving); 
                unit.StartUndoMove(); // Chama sua rotina de Undo estável
                break;

            case TurnState.Aiming:
                SetState(TurnState.MenuOpen);
                Debug.Log("Voltou para o Menu.");
                break;

            case TurnState.ConfirmTarget:
                SetState(TurnState.Aiming);
                Debug.Log("Cancelou confirmação.");
                break;
                
            // Handle Finished state case if necessary based on your current logic
            case TurnState.Finished:
                if (unit.boardCursor) unit.boardCursor.ClearSelection();
                break;
        }
    }

    public void SetState(TurnState newState)
    {
        currentState = newState;
    }
}