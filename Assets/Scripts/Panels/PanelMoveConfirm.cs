using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelMoveConfirm : MonoBehaviour
{
    public static readonly List<PanelMoveConfirm> All = new List<PanelMoveConfirm>();
    public static PanelMoveConfirm Instance { get; private set; }

    [Header("Root")]
    public GameObject root;

    [Header("Texts (LeftInfo)")]
    public TextMeshProUGUI housesText;
    public TextMeshProUGUI fuelText;

    [Header("Buttons")]
    public Button btnPrimary;
    public Button btnAltMove;
    public Button btnCancel;

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Awake()
    {
        // 🔥 IMPORTANTE: NÃO DESTRUIR duplicado (isso é o que estava “sumindo” com seu painel)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PanelMoveConfirm] Duplicate detected -> disabling this copy: {name}");
            gameObject.SetActive(false);
            enabled = false;
            return;
        }

        Instance = this;

        // 🔒 Segurança: root SEMPRE é esse próprio GO (evita você arrastar errado no Inspector)
        root = gameObject;

        Hide();
    }

    public void Show(int houses, int fuelCost, bool hasTargets)
    {
        if (!root) root = gameObject;

        if (housesText) housesText.text = $"Casas: {houses}";
        if (fuelText) fuelText.text = $"Autonomia: {fuelCost}";

        if (btnPrimary)
        {
            var t = btnPrimary.GetComponentInChildren<TextMeshProUGUI>();
            if (t) t.text = hasTargets ? "MIRAR [ENTER]" : "MOVER [ENTER]";
        }

        if (btnAltMove)
            btnAltMove.gameObject.SetActive(hasTargets);

        root.SetActive(true);
    }

    public void Hide()
    {
        if (!root) root = gameObject;
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
