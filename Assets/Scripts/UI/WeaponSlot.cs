using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponSlot : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI ammoText;

    public void Bind(WeaponConfig weapon)
    {
        if (weapon.data == null) return;

        // MVP: por enquanto usa o ícone que você já tem no WeaponData
        if (iconImage != null)
            iconImage.sprite = weapon.data.HUDSprite;

        if (ammoText != null)
            ammoText.text = "x" + weapon.squadAttacks.ToString();
    }
}
