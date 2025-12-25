using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Preview animado (reta ou parábola) do atacante até o alvo.
/// Desenha no MUNDO (sorting layer "effects") usando LineRenderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AttackPreviewLine : MonoBehaviour
{
    public static AttackPreviewLine Instance;

    [Header("Render")]
    private LineRenderer line;
    [SerializeField] private int trailLength = 12;   // rastro
    [SerializeField] private float zOverride = -0.2f; // garante ficar por cima do mapa
    [SerializeField] private string sortingLayerName = "Effects";
    [SerializeField] private int sortingOrder = 0;

    [Header("Curva (legado setaDeAtaque.js)")]
    [Tooltip("Altura máxima do arco (mundo).")]
    [SerializeField] private float maxArcHeight = 1.5f;
    [Tooltip("Divisor da distância para calcular altura (magnitude = min(max, dist/divisor)).")]
    [SerializeField] private float arcDivisor = 3f;
    [Tooltip("Quantos pontos usar para amostrar a parábola (mais = mais suave).")]
    [Range(8, 64)]
    [SerializeField] private int curveSegments = 24;

    [Header("Animação")]
    [Tooltip("Duração de um ciclo de animação (segundos).")]
    [SerializeField] private float cycleDuration = 1.2f;
    [Tooltip("Loop infinito enquanto o painel estiver aberto.")]
    [SerializeField] private bool loop = true;
    [Tooltip("Opacidade quando termina o ciclo (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float endAlpha = 0.75f;

    private readonly List<Vector3> fullPoints = new List<Vector3>(64);
    private float headFloatIndex;

    private bool playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (line == null) line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.useWorldSpace = true;
            // NÃO forçar width aqui. Configure no Inspector.


            line.enabled = false;
            line.sortingLayerName = sortingLayerName;
            line.sortingOrder = sortingOrder;
        }
    }

    public void Hide()
    {
        playing = false;
        headFloatIndex = 0f;


        if (line != null)
        {
            line.positionCount = 0;
            line.enabled = false;

            // reset alpha (pra não acumular)
            var c = line.startColor; c.a = 1f; line.startColor = c;
            c = line.endColor; c.a = 1f; line.endColor = c;
        }

        fullPoints.Clear();
    }

    /// <summary>
    /// Centro do hex (GetCellCenterWorld) -> Centro do hex.
    /// </summary>
    public void Show(Grid grid, Vector3Int attackerCell, Vector3Int targetCell, TrajectoryType trajectory)
    {
        if (grid == null)
        {
            Debug.LogWarning("[AttackPreviewLine] Show abort: grid is null.");
            return;
        }
        if (line == null)
        {
            Debug.LogWarning("[AttackPreviewLine] Show abort: LineRenderer is null.");
            return;
        }

        Vector3 p0 = GridUtils.GetCellCenterWorld(grid, attackerCell);
        Vector3 p1 = GridUtils.GetCellCenterWorld(grid, targetCell);

        Debug.Log($"[AttackPreviewLine] Show OK: attacker={attackerCell} target={targetCell} traj={trajectory}");
        Debug.Log($"[AttackPreviewLine] p0={p0} p1={p1}");
        ShowWorldPoints(p0, p1, trajectory);
    }

    public void ShowWorldPoints(Vector3 origin, Vector3 destination, TrajectoryType trajectory)
    {
        line.enabled = false;
        line.positionCount = 0;

        if (line == null)
        {
            Debug.LogWarning("[AttackPreviewLine] ShowWorldPoints abort: LineRenderer is null.");
            return;
        }

        BuildPoints(origin, destination, trajectory);
        if (fullPoints.Count < 2)
        {
            Debug.LogWarning("[AttackPreviewLine] ShowWorldPoints abort: not enough points.");
            return;
        }

        // aplica zOverride - ou sejaa, força ficar na frente do mapa
        for (int i = 0; i < fullPoints.Count; i++)
        {
            var p = fullPoints[i];
            p.z = zOverride;
            fullPoints[i] = p;
        }


        line.enabled = true;
        headFloatIndex = 0f;
        playing = true;
        ApplyAlpha(1f); // garante que a linha sempre     

        // aparece instantâneo
        line.positionCount = 2;
        line.SetPosition(0, fullPoints[0]);
        line.SetPosition(1, fullPoints[1]);
    }

    private void Update()
    {
        if (!playing || line == null || fullPoints.Count < 2) return;

        // head anda em "pontos por segundo"
        float speed = (fullPoints.Count / Mathf.Max(0.01f, cycleDuration));
        headFloatIndex += speed * Time.deltaTime;

        int last = fullPoints.Count - 1;

        // Quando o tail já passou do fim, reinicia o ciclo
        // (head vai até last + trailLength e só então reseta)
        if (headFloatIndex > last + trailLength)
        {
            if (loop) headFloatIndex = 0f;
            else
            {
                playing = false;
                ApplyAlpha(endAlpha);
                return;
            }
        }

        // head clampa no fim (pra "entrar no alvo" e ficar lá enquanto o tail termina)
        float headClamped = Mathf.Min(headFloatIndex, last);

        // tail segue atrás do head, mas nunca abaixo de 0
        float tailFloat = headFloatIndex - (trailLength - 1);
        float tailClamped = Mathf.Max(0f, tailFloat);

        int start = Mathf.FloorToInt(tailClamped);
        int end = Mathf.FloorToInt(headClamped);

        // garante pelo menos 2 pontos pra desenhar
        if (end <= start)
        {
            line.positionCount = 0;
            return;
        }

        int count = end - start + 1;
        line.positionCount = count;

        for (int i = 0; i < count; i++)
            line.SetPosition(i, fullPoints[start + i]);
    }



    private void ApplyAlpha(float alpha)
    {
        var c = line.startColor; c.a = alpha; line.startColor = c;
        c = line.endColor; c.a = alpha; line.endColor = c;
    }

    private void BuildPoints(Vector3 origin, Vector3 destination, TrajectoryType trajectory)
    {
        fullPoints.Clear();

        // === Reta simples ===
        if (trajectory == TrajectoryType.Straight)
        {
            int straightSeg = Mathf.Max(8, curveSegments); // reaproveita o mesmo slider
            for (int i = 0; i < straightSeg; i++)
            {
                float tt = i / (float)(straightSeg - 1);
                fullPoints.Add(Vector3.Lerp(origin, destination, tt));
            }
            return;
        }


        // === Bezier quadrática (igual ideia do setaDeAtaque.js) ===
        Vector3 mid = (origin + destination) * 0.5f;

        float dx = destination.x - origin.x;
        float dy = destination.y - origin.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        if (dist < 0.001f)
        {
            fullPoints.Add(origin);
            fullPoints.Add(destination);
            return;
        }

        float normalX = -dy / dist;
        float normalY = dx / dist;

        // Inverte curva dependendo do lado
        float invert = (origin.x > destination.x) ? -1f : 1f;

        float magnitude = Mathf.Min(maxArcHeight, dist / Mathf.Max(0.001f, arcDivisor));

        Vector3 ctrl = new Vector3(
            mid.x + normalX * magnitude * invert,
            mid.y + normalY * magnitude * invert,
            mid.z
        );

        int seg = Mathf.Max(8, curveSegments);
        for (int i = 0; i < seg; i++)
        {
            float tt = i / (float)(seg - 1);
            fullPoints.Add(QuadBezier(origin, ctrl, destination, tt));
        }
    }

    private static Vector3 QuadBezier(Vector3 p0, Vector3 c, Vector3 p1, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + (2f * u * t) * c + (t * t) * p1;
    }
}
