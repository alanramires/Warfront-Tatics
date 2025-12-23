using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public partial class UnitMovement : MonoBehaviour
{
    private Coroutine blinkRoutine;
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
        // ✅ reseta preview sempre que seleciona
        lastPathTaken.Clear();
        lastPathTaken.Add(currentCell);
        pendingCost = 0;
        lastMoveCost = 0;

        // Para qualquer rotina de piscar anterior
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        blinkRoutine = StartCoroutine(BlinkRoutine());
        ShowRange();
        if (boardCursor) boardCursor.LockMovement(navigableTiles);
    }

    // Chamado pelo Manager quando cancela seleção
    public void DeselectUnit()
    {
        ClearVisuals();
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        if (spriteRenderer) spriteRenderer.color = originalColor;
        if (boardCursor) boardCursor.ClearSelection();
    }
}
