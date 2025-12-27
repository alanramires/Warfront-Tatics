using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetItemUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image iconImage;          // Icon
    [SerializeField] private TextMeshProUGUI mainText; // Text
    [SerializeField] private Image heartImage;         // Heart
    [SerializeField] private TextMeshProUGUI hpText;   // HP_Text

    [Header("Heart Sprites")]
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartHalfSprite;
    [SerializeField] private Sprite heartDangerSprite;

    [Header("Thresholds")]
    [Range(0f, 1f)] [SerializeField] private float halfThreshold = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float dangerThreshold = 0.2f;

    // Chamada pelo painel
    public void Bind(UnitMovement attacker, UnitMovement target, int index)
    {
        if (target == null) return;

        // ICON
        if (iconImage != null)
        {
            if (TargetUiUtils.TryGetSprite(target, out var sprite, out var color, false))
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
                iconImage.preserveAspect = true;
                iconImage.color = color;
            }
            else iconImage.enabled = false;
        }

        // TEXTO PRINCIPAL
        if (mainText != null)
        {
            string unitName = TargetUiUtils.GetUnitDisplayName(target, target.name);
            string cellLabel = TargetUiUtils.GetCellLabel(target);
            string hotkey = (index == 0) ? "ENTER" : index.ToString();

            mainText.text = $"[{hotkey}] {unitName}\n({cellLabel})";
            mainText.color = TeamUtils.GetColor(attacker != null ? attacker.teamId : 0);
        }

        // HP TEXT
        if (hpText != null)
        {
            hpText.text = target.currentHP.ToString();
            // deixa neutro (não “papagaio”)
            hpText.color = Color.white;
        }

        // HEART (dinâmico)
        if (heartImage != null && target.data != null && target.data.maxHP > 0)
        {
            float pct = (float)target.currentHP / target.data.maxHP;

            if (pct <= dangerThreshold && heartDangerSprite != null)
                heartImage.sprite = heartDangerSprite;
            else if (pct <= halfThreshold && heartHalfSprite != null)
                heartImage.sprite = heartHalfSprite;
            else if (heartFullSprite != null)
                heartImage.sprite = heartFullSprite;

            heartImage.enabled = true;
            heartImage.preserveAspect = true;
        }
    }
}
