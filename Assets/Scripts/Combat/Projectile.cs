using UnityEngine;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    private SpriteRenderer sr;

    Vector3 start;
    Vector3 end;
    float t;

    float speed = 12f;
    TrajectoryType traj;
    float arcHeight = 0.5f;

    Action onArrive;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Função que o combate chama assim que atira
    public void Init(
        Vector3 from,
        Vector3 to,
        Sprite visual,
        TrajectoryType type,
        float projectileSpeed,
        float parabolicArcHeight,
        Action onArriveCallback)
    {
        start = from;
        end = to;
        traj = type;
        speed = projectileSpeed;
        arcHeight = parabolicArcHeight;
        onArrive = onArriveCallback;

        if (visual != null) sr.sprite = visual;

        transform.position = start;
        t = 0f;
    }

    void Update()
    {
        Vector3 prev = transform.position;
        float dist = Vector3.Distance(start, end);
        if (dist <= 0.001f)
        {
            Arrive();
            return;
        }

        t += Time.deltaTime * (speed / dist);
        t = Mathf.Clamp01(t);

        // posição linear entre start e end
        Vector3 pos = Vector3.Lerp(start, end, t);

        // ajuste de arco, se necessário
        if (traj == TrajectoryType.Parabolic)
        {
            // arco simples (seno) — Game Boy vibes
            float h = Mathf.Sin(t * Mathf.PI) * arcHeight;
            pos.y += h;
        }

        // gira pra direção do movimento (2D top-down geralmente usa "right" ou "up" dependendo do sprite)
        Vector3 delta = pos - prev;
        if (delta.sqrMagnitude > 0.000001f)
        {
            float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, ang);
        }

        transform.position = pos;

        if (t >= 1f)
            Arrive();
    }

    private void Arrive()
    {
        onArrive?.Invoke();
        Destroy(gameObject);
    }
}
