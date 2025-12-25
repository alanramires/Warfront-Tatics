using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelSelectTarget : MonoBehaviour
{
    public static readonly List<PanelSelectTarget> All = new List<PanelSelectTarget>();
    public static PanelSelectTarget Instance { get; private set; }

    [Header("Refs (arraste no Inspector)")]
    [SerializeField] private GameObject root;                 // Panel_SelectTarget (pode ser o proprio GO)
    [SerializeField] private TextMeshProUGUI headerText;      // HeaderText
    [SerializeField] private Transform listRoot;              // ListRoot
    [SerializeField] private GameObject targetItemTemplate;   // TargetItem (seu template/prefab)
    [SerializeField] private Button btnCancel;                // BtnCancel (opcional)

    // runtime
    private readonly List<UnitMovement> targets = new List<UnitMovement>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private UnitMovement attacker;
    public int LastChosenIndex { get; private set; } = -1;

    public bool IsOpen
    {
        get { return root != null && root.activeSelf; }
    }

    public event Action<UnitMovement> OnTargetChosen;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

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

        // Auto-find por nome, se voce esquecer de ligar no Inspector
        if (headerText == null)
        {
            Transform t = transform.Find("HeaderText");
            if (t != null) headerText = t.GetComponent<TextMeshProUGUI>();
        }

        if (listRoot == null)
        {
            Transform t = transform.Find("ListRoot");
            if (t != null) listRoot = t;
        }

        if (targetItemTemplate == null && listRoot != null)
        {
            Transform t = listRoot.Find("TargetItem");
            if (t != null) targetItemTemplate = t.gameObject;
        }

        if (btnCancel == null)
        {
            Transform t = transform.Find("BtnCancel");
            if (t != null) btnCancel = t.GetComponent<Button>();
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() =>
            {
                // ESC e tratado no TurnStateManager; botao so simula via log.
                Debug.Log("[SelectTarget] Cancel clicado (use ESC tambem).");
            });
        }

        // Importante: template nao deve ficar visivel
        if (targetItemTemplate != null)
            targetItemTemplate.SetActive(false);

        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;

        // ENTER = alvo 0
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ChooseIndex(0);
            return;
        }

        // 1..9 (teclado normal + numpad) = alvo 1..9
        for (int n = 1; n <= 9; n++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + n) || Input.GetKeyDown(KeyCode.Keypad0 + n))
            {
                ChooseIndex(n);
                return;
            }
        }
    }

    public void Show(UnitMovement attackerUnit, List<UnitMovement> newTargets)
    {
        attacker = attackerUnit;

        targets.Clear();
        if (newTargets != null) targets.AddRange(newTargets);

        if (headerText != null) headerText.text = "Alvos em Alcance";

        RebuildList();

        if (root == null) root = gameObject;
        root.SetActive(true);
    }

    public void Hide()
    {
        ClearList();

        if (root == null) root = gameObject;
        root.SetActive(false);
    }

    // Alvo escolhido
    private void ChooseIndex(int index)
    {
        if (index < 0 || index >= targets.Count) return;

        LastChosenIndex = index;

        UnitMovement chosen = targets[index];
        if (chosen == null) return;

        OnTargetChosen?.Invoke(chosen);
    }

    private void RebuildList()
    {
        ClearList();

        if (listRoot == null || targetItemTemplate == null) return;

        int count = Mathf.Min(10, targets.Count);

        for (int i = 0; i < count; i++)
        {
            UnitMovement t = targets[i];
            if (t == null) continue;

            GameObject go = Instantiate(targetItemTemplate, listRoot);
            go.name = $"TargetItem_{i + 1}";
            go.SetActive(true);
            spawned.Add(go);

            // Pega Icon + Text dentro do item
            Image icon = null;
            TextMeshProUGUI txt = null;

            Transform iconTr = go.transform.Find("Icon");
            if (iconTr != null) icon = iconTr.GetComponent<Image>();

            Transform textTr = go.transform.Find("Text");
            if (textTr != null) txt = textTr.GetComponent<TextMeshProUGUI>();

            // Icon: tenta puxar sprite do SpriteRenderer do alvo
            if (icon != null)
            {
                if (TargetUiUtils.TryGetSprite(t, out var sprite, out var color, false))
                {
                    icon.sprite = sprite;
                    icon.enabled = true;
                    icon.preserveAspect = true;
                    icon.color = color;
                }
                else
                {
                    icon.enabled = false;
                }
            }

            // Texto: "[ENTER] Apache (0,16)" ou "[1] ... "
            if (txt != null)
            {
                string unitName = TargetUiUtils.GetUnitDisplayName(t, t != null ? t.name : "Alvo");
                string cellLabel = TargetUiUtils.GetCellLabel(t);

                string hotkey = (i == 0) ? "ENTER" : i.ToString();
                //txt.text = $"[{hotkey}] {unitName}";
                txt.text = $"[{hotkey}] {unitName} \n ({cellLabel})";
                txt.color = TeamUtils.GetColor(attacker != null ? attacker.teamId : 0);
            }
        }
    }

    private void ClearList()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
        spawned.Clear();
    }
}