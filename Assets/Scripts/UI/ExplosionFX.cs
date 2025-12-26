using UnityEngine;

public class ExplosionFX : MonoBehaviour
{
    [Header("Frames (em ordem)")]
    public Sprite[] frames;

    [Header("Timing")]
    public float frameDuration = 0.06f; // 4 frames => ~0.24s
    public bool destroyOnFinish = true;

    private SpriteRenderer sr;
    private float t;
    private int idx;

    public float TotalDuration => (frames == null ? 0f : frames.Length * frameDuration);

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        t = 0f;
        idx = 0;
        if (sr != null && frames != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || sr == null) return;

        t += Time.deltaTime;
        int newIdx = Mathf.FloorToInt(t / Mathf.Max(0.0001f, frameDuration));

        if (newIdx != idx)
        {
            idx = newIdx;

            if (idx >= frames.Length)
            {
                if (destroyOnFinish) Destroy(gameObject);
                else gameObject.SetActive(false);
                return;
            }

            sr.sprite = frames[idx];
        }
    }
}
