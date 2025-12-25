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

    [Header("Item unico (mesmo prefab do SelectTarget)")]
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private GameObject targetItemTemplate;

    [Header("Textos / dicas")]
    [SerializeField] private TextMeshProUGUI hintText; // opcional

    [Header("Botoes")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    public event Action OnConfirm;
    public event Action OnCancel;

    private GameObject spawnedItem;
    private UnitMovement currentTarget;
    private UnitMovement currentAttacker;

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

    public void Show(UnitMovement attacker, UnitMovement target)
    {
        // procura o root se nao tiver setado, ou seja, o proprio GO/painel
        if (root == null) root = gameObject;
        root.SetActive(true);

        currentAttacker = attacker;
        currentTarget = target;
        
        // foca no alvo ao abrir confirm target
        if (currentAttacker != null && currentAttacker.boardCursor != null)
            currentAttacker.boardCursor.TeleportToCell(currentTarget.currentCell, playSfx: true, adjustCamera: true);


        TargetUiUtils.SetAttackHeader(headerText, target);
        if (hintText != null) hintText.text = "Confirmar [ENTER]    Cancelar [ESC]";

        EnsureSpawnedItem();
        FillTargetItem(target);
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemRoot);

        // Preview correto: attacker REAL + target REAL
        AttackPreviewUtils.Show(currentAttacker, currentTarget);
    }

    public void Hide()
    {
        if (root == null) root = gameObject;
        root.SetActive(false);

        currentAttacker = null;
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

        // pega a PRIMEIRA Image filha (exceto a que estiver no proprio root do item, se existir)
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
        if (img != null)
        {
            if (TargetUiUtils.TryGetSprite(target, out var sprite, out var color, true))
            {
                img.sprite = sprite;
                img.color = color;
            }
        }

        // texto + cor
        if (txt != null)
        {
            string unitName = TargetUiUtils.GetUnitDisplayName(target, target != null ? target.name : "Alvo");
            string cellLabel = TargetUiUtils.GetCellLabel(target);

            txt.text = $"{unitName} ({cellLabel})";
            txt.color = TeamUtils.GetColor(target != null ? target.teamId : 0);
        }
    }
}