using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public partial class UnitMovement : MonoBehaviour
{
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
        StartCoroutine("BlinkRoutine");
        ShowRange();
        if (boardCursor) boardCursor.LockMovement(navigableTiles);
    }

    // Chamado pelo Manager quando cancela seleção
    public void DeselectUnit()
    {
        ClearVisuals();
        StopCoroutine("BlinkRoutine");
        if (spriteRenderer) spriteRenderer.color = originalColor;
        if (boardCursor) boardCursor.ClearSelection();
    }
}
