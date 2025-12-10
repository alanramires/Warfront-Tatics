using UnityEngine;
using System.Collections.Generic;

public class TurnStateManager : MonoBehaviour
{
    [Header("Estado Atual")]
    public TurnState currentState = TurnState.None;

    [Header("Referências")]
    public UnitMovement unit; 
    // Cache pós-movimento
    [HideInInspector] public List<UnitMovement> cachedTargets = new List<UnitMovement>();
    [HideInInspector] public bool lastMoveWasActualMovement = false;

    private void Start()
    {
        unit = GetComponent<UnitMovement>();
    }

    /// <summary>
    /// Chamado ao final do movimento físico (ou movimento parado).
    /// Decide se o turno termina ou se abrimos o menu de ação (Mirar / Apenas Mover).
    /// </summary>

        public void EnterMoveConfirmation(bool hasMoved)
    {
        if (unit == null)
            unit = GetComponent<UnitMovement>();

        lastMoveWasActualMovement = hasMoved;
        cachedTargets.Clear();

        // Tenta pegar o UnitAttack
        UnitAttack attack = unit.GetComponent<UnitAttack>();
        if (attack != null)
        {
            // Scan de alvos já acontece aqui, mas NÃO decide nada ainda
            cachedTargets = attack.GetValidTargets(hasMoved);
            Debug.Log($"[TurnState] Scan pós-movimento: moveu={hasMoved}, alvos={cachedTargets.Count}");
        }
        else
        {
            Debug.Log("[TurnState] Unidade sem UnitAttack. Só pode mover.");
        }

        // Entramos no estado de confirmação de movimento
        SetState(TurnState.ConfirmMove);

        if (cachedTargets.Count == 0)
        {
            Debug.Log("🟢 Posição segura. ENTER = confirmar movimento, ESC = desfazer e escolher outro lugar.");
        }
        else
        {
            Debug.Log("⚠️ Inimigos ao alcance. ENTER = abrir opções (Mirar / Apenas mover), ESC = desfazer e escolher outro lugar.");
        }
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
                case TurnState.Inspected:
                    unit.ClearVisuals();
                    SetState(TurnState.None);
                    if (unit.boardCursor) unit.boardCursor.ClearSelection();
                    break;

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
            case TurnState.ConfirmMove:
            // ENTER dentro dessa fase
            if (cachedTargets.Count == 0)
            {
                // Não tem alvo: confirma o movimento e termina o turno
                Debug.Log("✅ Movimento confirmado. Sem alvos ao alcance. Turno encerrado.");
                unit.FinishTurn();
            }
            else
            {
                // Tem alvo: abre o "menu" Mirar / Apenas mover
                Debug.Log("📋 Opções: ENTER = Mirar | M = Apenas mover | ESC = desfazer movimento.");
                SetState(TurnState.MenuOpen);
            }
            break;

            case TurnState.MenuOpen:
                // ENTER = Mirar
                Debug.Log("👁️ Escolheu MIRAR: montando lista de alvos no alcance - aguarde.");
                SetState(TurnState.Aiming);
                // Próxima etapa: usar cachedTargets para escolha de alvo
                break;

            case TurnState.Aiming:
                // ENTER aqui depois vai confirmar alvo, por enquanto você pode só dar um log genérico
                Debug.Log("📌 (placeholder) Confirmando alvo escolhido...");
                SetState(TurnState.ConfirmTarget);
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
                // Cancela seleção e limpa tudo
                Debug.Log("🔙 Cancelou seleção da unidade.");
                unit.DeselectUnit();
                SetState(TurnState.None);
                break;
                
            case TurnState.ConfirmMove:
                Debug.Log("🔙 Cancelou movimento. Voltando à posição original.");

                if (lastMoveWasActualMovement)
                {
                    // Desfaz movimento animado
                    unit.StartUndoMove();
                }
                else
                {
                    // Não moveu de verdade (clicou na mesma casa): só volta pro estado Selected
                    unit.ShowRange();
                    if (unit.boardCursor) unit.boardCursor.LockMovement(unit.navigableTiles);
                    SetState(TurnState.Selected);
                }
                break;

            case TurnState.MenuOpen:
                // Volta um passo: sai do menu Mirar/Mover, mas mantém o movimento
                Debug.Log("🔙 Saiu do menu de ação. Ainda em confirmação de movimento.");
                SetState(TurnState.ConfirmMove);
                break;

            case TurnState.Aiming:
                // Volta pro menu Mirar/Mover
                Debug.Log("🔙 Cancelou mira. Voltando para opções Mirar / Apenas mover.");
                SetState(TurnState.MenuOpen);
                break;

            case TurnState.ConfirmTarget:
                Debug.Log("🔙 Cancelou confirmação de alvo.");
                SetState(TurnState.Aiming);
                break;

            case TurnState.Finished:
                // já era, nada pra cancelar
                break;
        }
    }

    public void SetState(TurnState newState)
    {
        currentState = newState;
    }

}