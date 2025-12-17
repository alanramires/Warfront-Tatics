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

    [Header("Item único (mesmo prefab do SelectTarget)")]
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private GameObject targetItemTemplate;

    [Header("Textos / dicas")]
    [SerializeField] private TextMeshProUGUI hintText; // opcional

    [Header("Botões")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    public event Action OnConfirm;
    public event Action OnCancel;

    private GameObject spawnedItem;
    private UnitMovement currentTarget;

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

        if (btnConfirm != null) btnConfirm.onClick.AddListener(Confirm);
        if (btnCancel != null) btnCancel.onClick.AddListener(Cancel);

        Hide();
    }

    public bool IsOpen => root != null && root.activeSelf;
    public UnitMovement CurrentTarget => currentTarget;

    public void Show(UnitMovement target)
    {
        if (root == null) root = gameObject;

        currentTarget = target;

        root.SetActive(true);

        string unitName = (target != null && target.data != null && !string.IsNullOrEmpty(target.data.unitName))
            ? target.data.unitName
            : (target != null ? target.name : "Alvo");

        if (headerText != null) headerText.text = $"Atacar {unitName}?";
        if (hintText != null) hintText.text = "Confirmar [ENTER]    Cancelar [ESC]";

        EnsureSpawnedItem();
        FillTargetItem(target);
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemRoot);

    }

    public void Hide()
    {
        if (root == null) root = gameObject;
        root.SetActive(false);
        currentTarget = null;
    }

    public void Confirm()
    {
        OnConfirm?.Invoke();
    }

    public void Cancel()
    {
        OnCancel?.Invoke();
    }

    private void EnsureSpawnedItem()
    {
        if (spawnedItem != null) return;
        if (itemRoot == null || targetItemTemplate == null) return;

        spawnedItem = Instantiate(targetItemTemplate, itemRoot);
        spawnedItem.SetActive(true);

        var rt = spawnedItem.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsFirstSibling();
        }
    }


    private void FillTargetItem(UnitMovement target)
    {
        if (spawnedItem == null || target == null) return;

        // pega a PRIMEIRA Image filha (exceto a que estiver no próprio root do item, se existir)
        Image img = null;
        var images = spawnedItem.GetComponentsInChildren<Image>(true);
        foreach (var im in images)
        {
            if (im.gameObject == spawnedItem) continue;
            img = im;
            break;
        }

        // pega o PRIMEIRO TMP filho
        TextMeshProUGUI txt = null;
        var tmps = spawnedItem.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmps != null && tmps.Length > 0) txt = tmps[0];

        // sprite
        if (img != null && target.TryGetComponent<SpriteRenderer>(out var sr))
        {
            img.sprite = sr.sprite;
            img.color = GetTeamColor(target.teamId);
        }

        // texto + cor
        if (txt != null)
        {
            string unitName = (target.data != null && !string.IsNullOrEmpty(target.data.unitName))
                ? target.data.unitName
                : target.name;

            txt.text = $"{unitName} ({target.currentCell.x},{target.currentCell.y})";
            txt.color = GetTeamColor(target.teamId);
        }

        // header "Atacar <nome colorido>?"
        if (headerText != null)
        {
            string unitName = (target.data != null && !string.IsNullOrEmpty(target.data.unitName))
                ? target.data.unitName
                : target.name;

            var c = GetTeamColor(target.teamId);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            headerText.text = $"Atacar <color=#{hex}>{unitName}</color>?";
        }
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
