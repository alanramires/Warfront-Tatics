using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PanelMoveConfirm : MonoBehaviour
{
    public static PanelMoveConfirm Instance { get; private set; }

    [Header("Root")]
    public GameObject root;

    [Header("Texts (LeftInfo)")]
    public TextMeshProUGUI housesText;    // "Casas: 2"
    public TextMeshProUGUI fuelText;      // "Autonomia: 2" / "Combustível: 2"

    [Header("Buttons")]
    public Button btnPrimary;   // "Mirar [Enter]" OU "Mover [Enter]"
    public Button btnAltMove;   // "Mover [Espaço]" (liga só quando tiver alvo)
    public Button btnCancel;    // "Cancelar [ESC]"

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (root == null) root = gameObject;
        Hide();
    }

    public void Show(int houses, int fuelCost, bool hasTargets)
    {
        if (!root) root = gameObject;
         if (this == null) return; // protege contra "destroyed"

        if (housesText) housesText.text = $"Casas: {houses}";
        if (fuelText) fuelText.text = $"Autonomia: {fuelCost}";

        // MVP: sem menu real ainda, só muda o texto/visibilidade
        if (btnPrimary)
        {
            var t = btnPrimary.GetComponentInChildren<TextMeshProUGUI>();
            if (t) t.text = hasTargets ? "Mirar [Enter]" : "Mover [Enter]";
        }

        if (btnAltMove)
            btnAltMove.gameObject.SetActive(hasTargets); // só aparece se tem alvos

        root.SetActive(true);
    }

    public void Hide()
    {
        if (!root) return; // se root sumiu, só não faz nada

         if (this == null) return; // protege contra "destroyed"

        if (root) root.SetActive(false);
    }

        private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

}
