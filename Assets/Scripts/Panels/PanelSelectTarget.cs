using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelSelectTarget : MonoBehaviour
{
    public static PanelSelectTarget Instance { get; private set; }

    [Header("Refs (arraste no Inspector)")]
    [SerializeField] private GameObject root;                 // Panel_SelectTarget (pode ser o próprio GO)
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            enabled = false;
            return;
        }
        Instance = this;

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root == null) root = gameObject;

        // Auto-find por nome, se você esquecer de ligar no Inspector
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
                // ESC é tratado no TurnStateManager; botão só “simula” ESC via log.
                Debug.Log("[SelectTarget] Cancel clicado (use ESC também).");
            });
        }

        // Importante: template não deve ficar visível
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
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    icon.sprite = sr.sprite;
                    icon.enabled = true;
                    icon.preserveAspect = true;
                    icon.color = (sr != null) ? sr.color : Color.white;

                }
                else
                {
                    icon.enabled = false;
                }
            }


            // Texto: "[ENTER] Apache (0,16)" ou "[1] ... "
            if (txt != null)
            {
                string unitName = (t.data != null && !string.IsNullOrEmpty(t.data.unitName))
                    ? t.data.unitName
                    : t.name;

                string hotkey = (i == 0) ? "ENTER" : i.ToString();
                //txt.text = $"[{hotkey}] {unitName}";
                txt.text = $"[{hotkey}] {unitName} \n ({t.currentCell.y},{t.currentCell.x})";
                txt.color = GetTeamColor(attacker != null ? attacker.teamId : 0);
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

    private Color GetTeamColor(int teamId)
    {
        // Usa suas cores globais
        // (GameColors está no GameEnums.cs) :contentReference[oaicite:1]{index=1}
        switch (teamId)
        {
            case 0: return GameColors.TeamGreen;
            case 1: return GameColors.TeamRed;
            case 2: return GameColors.TeamBlue;
            case 3: return GameColors.TeamYellow;
            default: return Color.white;
        }
    }
}
