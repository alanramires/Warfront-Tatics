using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelMovement : MonoBehaviour
{
    public static PanelMovement Instance { get; private set; }

    [Header("Root")]
    public GameObject root;           // Panel_Movement

    [Header("Texts")]
    public TextMeshProUGUI actionText;    // "Movimentando / Pilotando..."
    public TextMeshProUGUI unitNameText;  // "Soldado (verde)"

    [Header("Buttons")]
    public Button moveButton;
    public Button cancelButton;

    private UnitMovement currentUnit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null) root = gameObject;
        Hide();

        // Por enquanto só dão log; depois a gente conecta no fluxo real.
        if (moveButton != null)
            moveButton.onClick.AddListener(OnClickMove);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnClickCancel);
    }

    public void Show(UnitMovement unit)
    {
        currentUnit = unit;

        if (unit == null || unit.data == null)
        {
            Hide();
            return;
        }

        if (actionText != null)
            actionText.text = GetActionVerb(unit.data.unitType);

        if (unitNameText != null)
            unitNameText.text = $"{unit.data.unitName} ({GetTeamLabel(unit.teamId)})";
            unitNameText.color = TeamUtils.GetColor(unit.teamId);

        root.SetActive(true);
    }

    public void Hide()
    {
        currentUnit = null;
        if (root != null) root.SetActive(false);
    }

    private string GetActionVerb(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry:   return "Movimentando";
            case UnitType.Vehicle:
            case UnitType.Artillery:
            case UnitType.Tank:       return "Dirigindo";
            case UnitType.JetFighter:
            case UnitType.Helicopter:
            case UnitType.Plane:      return "Pilotando";
            case UnitType.Ship:
            case UnitType.Sub:        return "Navegando";
            default:                  return "Movendo";
        }
    }

    private string GetTeamLabel(int teamId)
    {
        switch (teamId)
        {
            case 0: return "verde";
            case 1: return "vermelho";
            case 2: return "azul";
            case 3: return "amarelo";
            default: return $"time {teamId}";
        }
    }

    private void OnClickMove()
    {
        Debug.Log("[PanelMovement] Clique em MOVER (equivalente ao ENTER).");
        // depois a gente chama aqui o mesmo fluxo do Enter
    }

    private void OnClickCancel()
    {
        Debug.Log("[PanelMovement] Clique em CANCELAR (equivalente ao ESC).");
        // depois chamamos o mesmo que ESC
    }
}
