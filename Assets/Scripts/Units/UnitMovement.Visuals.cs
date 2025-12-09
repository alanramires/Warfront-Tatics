using UnityEngine;
using System.Collections;

public partial class UnitMovement : MonoBehaviour
{
    // ✨ VISUAL / FEEDBACK
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
