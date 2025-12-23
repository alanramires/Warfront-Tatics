using UnityEngine;
using System.Collections.Generic;

public class TurnStateManager : MonoBehaviour
{
    [Header("Estado Atual")]
    public TurnState currentState = TurnState.None;

    [Header("Referências")]
    public UnitMovement unit;
    public PathPreviewLine pathLine;


    [HideInInspector] public List<UnitMovement> cachedTargets = new List<UnitMovement>();
    [HideInInspector] public bool lastMoveWasActualMovement = false;
        [HideInInspector] public UnitMovement selectedTarget = null;

    void Start()
    {
        unit = GetComponent<UnitMovement>();

        // ✅ Reset defensivo: fecha TODOS os painéis encontrados (mesmo duplicados)
        HideAllPanelsDefensive();

        SetState(TurnState.None);
    }

    void OnDestroy()
    {
        UnsubscribeSelectTargetDefensive();
    }

    // =========================================================
    // MOVE FINISHED -> CONFIRM MOVE (SEMPRE)
    // =========================================================
    public void EnterMoveConfirmation(bool hasMoved)
    {
        if (unit == null) unit = GetComponent<UnitMovement>();
        if (unit.lastPathTaken == null) unit.lastPathTaken = new List<Vector3Int>();

        if (unit.lastPathTaken.Count == 0)
            unit.lastPathTaken.Add(unit.posicaoOriginal);

        if (unit.lastPathTaken.Count == 1 && unit.currentCell != unit.lastPathTaken[0])
            unit.lastPathTaken.Add(unit.currentCell);


        lastMoveWasActualMovement = hasMoved;
        selectedTarget = null;
        cachedTargets.Clear();

        // ✅ Blindagem: fecha lista de alvos SEMPRE quando entra em ConfirmMove
        HideAllSelectTargetPanels();

        // Scan
        UnitAttack attack = unit.GetComponent<UnitAttack>();
        if (attack != null)
        {
            cachedTargets = attack.GetValidTargets(hasMoved);
            Debug.Log($"[TurnState] Scan pós-movimento: moveu={hasMoved}, alvos={cachedTargets.Count}");
        }
        else
        {
            Debug.Log("[TurnState] Unidade sem UnitAttack. Só pode mover.");
        }

        SetState(TurnState.ConfirmMove);

        // UI: fecha Movement, abre ConfirmMove + linha
        HideAllMovementPanels();
        ShowConfirmMovePanel();
        PathPreviewLine.Instance?.Show(unit);

    }

    // =========================================================
    // ENTER / CLIQUE (cursor)
    // =========================================================
    public void ProcessInteraction(Vector3Int cursorPosition)
    {
        if (currentState == TurnState.Moving) return;

        switch (currentState)
        {
            case TurnState.Inspected:
                unit.ClearVisuals();
                SetState(TurnState.None);
                unit.boardCursor?.ClearSelection();
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

                        HideAllConfirmMovePanels();
                        HideAllSelectTargetPanels();
                        HideAllPathLines();

                        ShowMovementPanel();
                    }
                }
                break;

            case TurnState.Selected:
                if (cursorPosition == unit.currentCell)
                {
                    HideAllMovementPanels();
                    unit.MoveDirectlyToMenu(); // no fim chama EnterMoveConfirmation(false)
                }
                else
                {
                    if (unit.IsValidDestination(cursorPosition))
                    {
                        HideAllMovementPanels();
                        SetState(TurnState.Moving);
                        unit.StartPhysicalMove(cursorPosition); // no fim chama EnterMoveConfirmation(true)
                    }
                    else
                    {
                        unit.boardCursor?.PlayError();
                    }
                }
                break;

            case TurnState.ConfirmMove:
                // fecha o painel confirm move
                HideAllConfirmMovePanels();

                if (cachedTargets.Count == 0)
                {
                    Debug.Log("✅ Confirmado: sem alvos. Encerrando turno.");
                    unit.FinishTurn();
                    HideAllPathLines();
                }
                else
                {
                    Debug.Log("👁️ Abrindo lista de alvos (ENTER=0, 1–9). ESC volta pro ConfirmMove.");

                    // importante: ao entrar no Aiming, some com o pathline do ConfirmMove
                    HideAllPathLines();

                    SetState(TurnState.Aiming);
                    unit.boardCursor?.PlayConfirm();

                    SubscribeSelectTargetDefensive();
                    ShowSelectTargetPanel();
                }
                break;


            case TurnState.Aiming:
                Debug.Log("🎯 Alvo selecionado. ENTER para confirmar ataque, ESC para voltar à lista.");
                break;

            case TurnState.ConfirmTarget:
                PanelConfirmTarget.Instance?.Hide();
                HideAllSelectTargetPanels();
                HideAllPathLines();

                Debug.Log("🔥 balas voando, aguarde...");

                // placeholder: por enquanto só encerra o turno (pra não travar o fluxo)
                unit.FinishTurn();
                SetState(TurnState.Finished);
                break;


            

            case TurnState.Finished:
                if (cursorPosition == unit.currentCell)
                {
                    SetState(TurnState.Inspected);
                    unit.ShowRange();
                }
                break;
        }
    }

    // =========================================================
    // ESC
    // =========================================================
    public void ProcessCancel()
    {
        if (currentState == TurnState.Moving) return;

        switch (currentState)
        {
            case TurnState.Selected:
                unit.DeselectUnit();
                HideAllMovementPanels();
                SetState(TurnState.None);
                break;

            case TurnState.ConfirmMove:
                HideAllConfirmMovePanels();
                HideAllPathLines();

                if (lastMoveWasActualMovement)
                {
                    Debug.Log("🔙 Undo do movimento.");
                    unit.StartUndoMove();
                }
                else
                {
                    Debug.Log("🔙 Voltando pro Selected (ficou parado).");
                    SetState(TurnState.Selected);
                    unit.SelectUnit();
                    ShowMovementPanel();
                }
                break;

            case TurnState.Aiming:
                Debug.Log("🔙 Lista de alvos -> volta ConfirmMove.");
                HideAllSelectTargetPanels();
                UnsubscribeSelectTargetDefensive();

                SetState(TurnState.ConfirmMove);
                ShowConfirmMovePanel();
                PathPreviewLine.Instance?.Show(unit);
                break;

            case TurnState.ConfirmTarget:
                Debug.Log("🔙 ConfirmTarget -> volta para Aiming (lista).");

                PanelConfirmTarget.Instance?.Hide();

                // volta pra lista de alvos
                if (PanelSelectTarget.Instance)
                {
                    PanelSelectTarget.Instance.OnTargetChosen -= OnTargetChosen;
                    PanelSelectTarget.Instance.OnTargetChosen += OnTargetChosen;
                    PanelSelectTarget.Instance.Show(unit, cachedTargets);
                }

                SetState(TurnState.Aiming);
                break;



            case TurnState.Inspected:
                unit.ClearVisuals();
                SetState(TurnState.None);
                unit.boardCursor?.ClearSelection();
                break;
        }
    }

    // DEFINIR ESTADO
    
    public void SetState(TurnState newState)
    {
        Debug.Log($"---------------> TurnState: {currentState} -> {newState}", this);
        if (currentState == TurnState.ConfirmTarget && newState != TurnState.ConfirmTarget)
        {
            if (PanelConfirmTarget.Instance) PanelConfirmTarget.Instance.Hide();
            if (AttackPreviewLine.Instance) AttackPreviewLine.Instance.Hide();
        }

        currentState = newState;
    }



    // ========================================================
    //           ESPAÇO
    // ========================================================

    public void ProcessSpace()
    {
        // Só faz algo quando estamos confirmando e tem alvos
        if (currentState != TurnState.ConfirmMove) return;

        if (cachedTargets.Count > 0)
        {
            Debug.Log("🟦 Apenas mover (Space). Turno encerrado sem atacar.");
            // “Apenas mover”
            PanelMoveConfirm.Instance?.Hide();
            HideAllPathLines();
            unit.FinishTurn(); // toca done, cadeado, etc
        }
        else
        {
            // opcional: Space confirma também quando não há alvo
            // Debug.Log("🟦 Confirmado (Space). Turno encerrado.");
           // unit.FinishTurn();
        }
    }


    // =========================================================
    // UI Helpers (defensivos contra duplicata)
    // =========================================================

    void HideAllPanelsDefensive()
    {
        HideAllMovementPanels();
        HideAllConfirmMovePanels();
        HideAllSelectTargetPanels();
        HideAllPathLines();
    }

    void HideAllMovementPanels()
    {
        var panels = new List<PanelMovement>(PanelMovement.All);
        foreach (var p in panels)
            if (p != null) p.Hide();
    }

    void HideAllConfirmMovePanels()
    {
        var panels = new List<PanelMoveConfirm>(PanelMoveConfirm.All);
        foreach (var p in panels)
            if (p != null) p.Hide();
    }

    void HideAllSelectTargetPanels()
    {
        var panels = new List<PanelSelectTarget>(PanelSelectTarget.All);
        foreach (var p in panels)
            if (p != null) p.Hide();
    }

    void HideAllPathLines()
    {
        var panels = new List<PathPreviewLine>(PathPreviewLine.All);
        foreach (var p in panels)
            if (p != null) p.Hide();
    }

    void ShowMovementPanel()
    {
        PanelMovement.Instance?.Show(unit);
    }

    void ShowConfirmMovePanel()
    {
        int houses = Mathf.Max(0, unit.lastPathTaken.Count - 1);
        int fuelCost = ComputeFuelCostFromLastPath();
        bool hasTargets = cachedTargets.Count > 0;

        PanelMoveConfirm.Instance?.Show(houses, fuelCost, hasTargets);
        pathLine?.Show(unit);
    }

    void ShowSelectTargetPanel()
    {
        PanelSelectTarget.Instance?.Show(unit, cachedTargets);
    }

    int ComputeFuelCostFromLastPath()
    {
        if (unit == null || unit.data == null) return 0;
        if (unit.lastPathTaken == null || unit.lastPathTaken.Count == 0) return 0;

        int cost = 0;
        UnitType type = unit.data.unitType;
        Vector3Int start = unit.lastPathTaken[0];

        for (int i = 0; i < unit.lastPathTaken.Count; i++)
        {
            var tile = unit.lastPathTaken[i];
            if (tile == start) continue;

            if (TerrainManager.Instance != null) cost += TerrainManager.Instance.GetMovementCost(tile, type);
            else cost += 1;
        }
        return cost;
    }

    // =========================================================
    // SelectTarget callbacks (defensivos)
    // =========================================================
    void SubscribeSelectTargetDefensive()
    {
        if (PanelSelectTarget.Instance == null) return;
        PanelSelectTarget.Instance.OnTargetChosen -= OnTargetChosen;
        PanelSelectTarget.Instance.OnTargetChosen += OnTargetChosen;
    }

    void UnsubscribeSelectTargetDefensive()
    {
        if (PanelSelectTarget.Instance == null) return;
        PanelSelectTarget.Instance.OnTargetChosen -= OnTargetChosen;
    }

    private void OnTargetChosen(UnitMovement target)
    {
        if (target == null) return;

        // fecha a lista
        if (PanelSelectTarget.Instance) PanelSelectTarget.Instance.Hide();

        // toca o som de confirmar
        if (unit != null && unit.boardCursor != null)
            unit.boardCursor.PlaySFX(unit.boardCursor.sfxConfirm); // <-- confirm.mp3 no Inspector do Cursor

        // abre confirm
        int idx = (PanelSelectTarget.Instance != null) ? PanelSelectTarget.Instance.LastChosenIndex : 0;
        PanelConfirmTarget.Instance?.Show(unit,target);
        SetState(TurnState.ConfirmTarget);

    }

    // ========================================================================
    // 🎯 CONFIRMAR ALVO (EnterConfirmTarget)
    // ========================================================================
    public void EnterConfirmTarget(UnitMovement target)
    {
        if (unit == null) unit = GetComponent<UnitMovement>();

        SetState(TurnState.ConfirmTarget);

        if (PanelConfirmTarget.Instance != null)
        {
            PanelConfirmTarget.Instance.OnConfirm -= ConfirmAttack;
            PanelConfirmTarget.Instance.OnCancel  -= CancelConfirmTarget;
            PanelConfirmTarget.Instance.OnConfirm += ConfirmAttack;
            PanelConfirmTarget.Instance.OnCancel  += CancelConfirmTarget;

            PanelConfirmTarget.Instance.Show(unit, target);
        }

        ShowAttackPreview(target);
    }

    private void ShowAttackPreview(UnitMovement target)
    {
        if (AttackPreviewLine.Instance == null) return;
        if (unit == null || unit.boardCursor == null || unit.boardCursor.mainGrid == null) return;
        if (target == null) return;

        var grid = unit.boardCursor.mainGrid;
        var traj = GetPrimaryTrajectory(unit);
        AttackPreviewLine.Instance.Show(grid, unit.currentCell, target.currentCell, traj);
    }

    private static TrajectoryType GetPrimaryTrajectory(UnitMovement attacker)
    {
        if (attacker != null && attacker.myWeapons != null && attacker.myWeapons.Count > 0)
        {
            var cfg = attacker.myWeapons[0];
            if (cfg.data != null)
                return cfg.data.trajectory;
        }
        return TrajectoryType.Straight;
    }

    private void CancelConfirmTarget()
    {
        if (PanelConfirmTarget.Instance) PanelConfirmTarget.Instance.Hide();
        if (AttackPreviewLine.Instance) AttackPreviewLine.Instance.Hide();
        SetState(TurnState.Aiming);
    }

    private void ConfirmAttack()
    {
        if (PanelConfirmTarget.Instance) PanelConfirmTarget.Instance.Hide();
        if (AttackPreviewLine.Instance) AttackPreviewLine.Instance.Hide();
        // depois você pluga aqui o disparo real
    }



}
