using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponSlot : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI ammoText;

    // Essa funÃ§Ã£o vincula os dados da arma aos elementos da UI
    public void Bind(WeaponConfig weapon)
    {
        if (weapon.data == null) return;

        // MVP: por enquanto usa o Ã­cone que vocÃª jÃ¡ tem no WeaponProfile
        if (iconImage != null)
            iconImage.sprite = weapon.data.HUDSprite;

        if (ammoText != null)
            ammoText.text = "x" + weapon.squadAttacks.ToString();
    }
}

