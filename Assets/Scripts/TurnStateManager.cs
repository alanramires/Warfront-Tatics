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

            // --- DEGRAU 1: SELECTED ---
            case TurnState.Selected:
                if (cursorPosition == unit.currentCell)
                {
                    SetState(TurnState.Moving); 
                    unit.MoveDirectlyToMenu(); 
                }
                else if (unit.IsValidMove(cursorPosition)) 
                {
                    SetState(TurnState.Moving);
                    unit.StartPhysicalMove(cursorPosition); 
                }
                else
                {
                    ProcessCancel(); 
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
            // CORREÇÃO CRÍTICA: Adicionei o caso Finished.
            // Se por acaso o cursor pegar a unidade no estado Finished, o ESC solta ela.
            case TurnState.Finished:
                if (unit.boardCursor) unit.boardCursor.ClearSelection();
                break;

            case TurnState.Inspected:
                unit.ClearVisuals();
                
                // Se a unidade já acabou, ela volta para o estado lógico Finished
                // Se era apenas inspeção de inimigo, volta para None
                if (unit.isFinished) SetState(TurnState.Finished);
                else SetState(TurnState.None);

                if (unit.boardCursor) unit.boardCursor.ClearSelection();
                break;

            case TurnState.Selected:
                unit.DeselectUnit();
                SetState(TurnState.None);
                break;

            case TurnState.MenuOpen:
                SetState(TurnState.Moving); 
                unit.StartUndoMove(); 
                break;

            case TurnState.Aiming:
                SetState(TurnState.MenuOpen);
                break;

            case TurnState.ConfirmTarget:
                SetState(TurnState.Aiming);
                break;
        }
    }

    public void SetState(TurnState newState)
    {
        currentState = newState;
    }
}