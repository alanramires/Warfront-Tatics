using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayerController : MonoBehaviour
{
    [Header("UI (opcional)")]
    public GameObject miniPanel;

    [Header("Tracks (arraste aqui)")]
    public AudioClip[] tracks;

    [Header("Hotkeys")]
    public KeyCode toggleKey = KeyCode.F10;
    public KeyCode playPauseKey = KeyCode.P;
    public KeyCode nextKey = KeyCode.RightBracket; // ]
    public KeyCode prevKey = KeyCode.LeftBracket;  // [
    public KeyCode volUpKey = KeyCode.Equals;      // =
    public KeyCode volDownKey = KeyCode.Minus;     // -
    [Range(0f, 1f)] public float volumeStep = 0.05f;

    [Header("Behavior")]
    public bool playOnAwake = true;

    private AudioSource source;
    private int index = 0;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;

        if (tracks != null && tracks.Length > 0)
        {
            source.clip = tracks[0];
            if (playOnAwake) source.Play();
        }
        else
        {
            Debug.LogWarning("[MusicPlayerController] Nenhuma track atribuída (arraste no Inspector).");
        }
    }

    void Update()
    {
        if (miniPanel && Input.GetKeyDown(toggleKey))
            miniPanel.SetActive(!miniPanel.activeSelf);

        if (Input.GetKeyDown(volUpKey))
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + volumeStep);

        if (Input.GetKeyDown(volDownKey))
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume - volumeStep);

        if (tracks == null || tracks.Length == 0 || source.clip == null) return;

        if (Input.GetKeyDown(playPauseKey))
        {
            if (source.isPlaying) source.Pause();
            else source.Play();
        }

        if (Input.GetKeyDown(nextKey)) Next();
        if (Input.GetKeyDown(prevKey)) Prev();
    }

    public void Next()
    {
        if (tracks == null || tracks.Length == 0) return;
        index = (index + 1) % tracks.Length;
        source.clip = tracks[index];
        source.time = 0f;
        source.Play();
    }

    public void Prev()
    {
        if (tracks == null || tracks.Length == 0) return;
        index = (index - 1 + tracks.Length) % tracks.Length;
        source.clip = tracks[index];
        source.time = 0f;
        source.Play();
    }
}
