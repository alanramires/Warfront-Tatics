using UnityEngine;
using System.Collections;

public class ProjectileProfile : MonoBehaviour
{
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Função que a Unidade chama assim que atira
    public void Setup(Sprite visual, TrajectoryType type)
    {
        // 1. Veste a "roupa" correta (Bala ou Míssil)
        if (visual != null && sr != null)
        {
            sr.sprite = visual;
        }

        // 2. Define o tamanho (opcional, mísseis podem ser maiores)
        // Aqui você pode adicionar lógica: Se for Parábola, aumenta o scale, etc.
        
        // 3. Inicia o voo (Lógica futura)
        // StartCoroutine(FlyRoutine(target, type));
    }
}