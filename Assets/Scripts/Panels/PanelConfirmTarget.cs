using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelConfirmTarget : MonoBehaviour
{
    public static PanelConfirmTarget Instance;

    [Header("Refs (arraste no Inspector)")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Item único (usa o mesmo prefab TargetItem)")]
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private GameObject targetItemTemplate;

    [Header("Texto extra")]
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Botões")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    public event Action OnConfirm;
    public event Action OnCancel;

    private GameObject spawnedItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            enabled = false;
            return;
        }
        Instance = this;

        if (root == null) root = gameObject;

        if (btnConfirm != null) btnConfirm.onClick.AddListener(() => OnConfirm?.Invoke());
        if (btnCancel != null) btnCancel.onClick.AddListener(() => OnCancel?.Invoke());

        Hide();
    }

    public bool IsOpen()
    {
        return root != null && root.activeSelf;
    }

    public void Show(UnitMovement target, int index)
    {
        if (root == null) root = gameObject;
        root.SetActive(true);

        if (headerText != null) headerText.text = "Confirmar Alvo";
        if (warningText != null) warningText.text = "É esse mesmo? (ENTER confirma / ESC volta)";

        EnsureSpawnedItem();

        if (spawnedItem == null || target == null) return;

        // Preenche o TargetItem (Icon + Text)
        var img = spawnedItem.transform.Find("Icon")?.GetComponent<Image>();
        var txt = spawnedItem.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        if (img != null && target.TryGetComponent<SpriteRenderer>(out var sr))
            img.sprite = sr.sprite;

        if (txt != null)
        {
            string unitName = target.data != null ? target.data.unitName : target.name;
            txt.text = $"[{index + 1}] {unitName} ({target.currentCell.x},{target.currentCell.y})";
            txt.color = GetTeamColor(target.teamId);
        }
    }

    public void Hide()
    {
        if (root == null) root = gameObject;
        root.SetActive(false);
    }

    private void EnsureSpawnedItem()
    {
        if (spawnedItem != null) return;
        if (itemRoot == null || targetItemTemplate == null) return;

        spawnedItem = Instantiate(targetItemTemplate, itemRoot);
        spawnedItem.SetActive(true);
    }

    private Color GetTeamColor(int teamId)
    {
        return teamId switch
        {
            0 => GameColors.TeamGreen,
            1 => GameColors.TeamRed,
            2 => GameColors.TeamBlue,
            3 => GameColors.TeamYellow,
            _ => Color.white
        };
    }
}
