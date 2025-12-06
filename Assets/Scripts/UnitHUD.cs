using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitHUD : MonoBehaviour
{
    [Header("Referências Visuais")]
    public TextMeshProUGUI hpText;       
    public Image hpBackground;           // O quadradinho colorido
    
    [Header("Armas (Opcional por enquanto)")]
    public Transform weaponContainer;    
    public GameObject weaponSlotPrefab;  

    // Atualiza o HP (Ex: 10)
    public void UpdateHP(int currentHP)
    {
        if (hpText != null)
        {
            hpText.text = currentHP.ToString();
            hpText.color = Color.black;
            
            /* Troca de cor do TEXTO (Verde > Amarelo > Vermelho)
            if (currentHP >= 7) hpText.color = Color.green;
            else if (currentHP >= 4) hpText.color = Color.yellow;
            else hpText.color = Color.red;*/
        }
    }

    // Pinta o Fundo do HP com a cor do time
    public void SetTeamColor(Color color)
    {
        if (hpBackground != null)
        {
            hpBackground.color = color;
        }
    }

    // Gera os ícones das armas
    public void SetupWeapons(List<WeaponConfig> loadout)
    {
        // --- TRAVA DE SEGURANÇA ---
        // Se você não arrastou o container no Inspector, ele aborta essa função
        // e evita o erro, permitindo que o jogo continue.
        if (weaponContainer == null || weaponSlotPrefab == null) return; 

        // 1. Limpa armas antigas
        foreach (Transform child in weaponContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Cria os novos ícones
        foreach (WeaponConfig weapon in loadout)
        {
            GameObject newSlot = Instantiate(weaponSlotPrefab, weaponContainer);
            
            // Configura a Imagem com segurança
            Transform iconTransform = newSlot.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImg = iconTransform.GetComponent<Image>();
                if (weapon.icon != null && iconImg != null) iconImg.sprite = weapon.icon;
            }

            // Configura o Texto de Munição com segurança
            Transform ammoTransform = newSlot.transform.Find("Ammo");
            if (ammoTransform != null)
            {
                TextMeshProUGUI ammoText = ammoTransform.GetComponent<TextMeshProUGUI>();
                if (ammoText != null) ammoText.text = ":" + weapon.squadAttacks.ToString();
            }
        }
    }
}