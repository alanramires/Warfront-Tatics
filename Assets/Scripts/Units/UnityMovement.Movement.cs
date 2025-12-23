using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public partial class UnitMovement : MonoBehaviour
{
    // Aqui vão ficar só as coisas de MOVIMENTO / UNDO
     public void StartPhysicalMove(Vector3Int destination)
    {
        AudioClip moveClip = (data.unitType == UnitType.Infantry) ? boardCursor.sfxMarch : boardCursor.sfxVehicle;
        if (boardCursor) boardCursor.PlaySFX(moveClip);
        StartCoroutine(MoveRoutine(destination));
    }

    public void MoveDirectlyToMenu()
    {
        if (boardCursor) boardCursor.PlaySFX(boardCursor.sfxConfirm);

        // ✅ garante que o path preview seja consistente
        lastPathTaken.Clear();
        lastPathTaken.Add(currentCell);
        pendingCost = 0;
        lastMoveCost = 0;

        if (boardCursor) boardCursor.LockMovement(new List<Vector3Int> { currentCell });
        OnMoveFinished();
    }


    public void StartUndoMove()
    {
        StartCoroutine(UndoMoveRoutine());
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
            Vector3 targetPos = boardCursor.mainGrid.GetCellCenterWorld(nextTile);


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
                Vector3 targetPos = boardCursor.mainGrid.GetCellCenterWorld(nextTile);

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

        // No fim do UndoMoveRoutine()
        if (PanelMovement.Instance) PanelMovement.Instance.Show(this);

        if (PathPreviewLine.Instance) PathPreviewLine.Instance.Hide();
    }

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

        Debug.Log("Movimento concluído. Avaliando opções de ação...");

         bool hasMoved = (currentCell != posicaoOriginal);

        if (stateManager != null)
        {
            stateManager.EnterMoveConfirmation(hasMoved);
        }
    }

    int GetEffectiveRange()
    {
        if (data == null) return 0;
        return Mathf.Min(data.moveRange, currentFuel);
    }

    HashSet<Vector3Int> GetMovementBlockers()
    {
        HashSet<Vector3Int> blockers = new HashSet<Vector3Int>();
        var allUnits = UnitMovement.All;
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
        var allUnits = UnitMovement.All;
        foreach (UnitMovement unit in allUnits)
        {
            if (unit == this) continue;
            if (unit.teamId == this.teamId) blockers.Add(unit.currentCell);
        }
        return blockers;
    }
}
