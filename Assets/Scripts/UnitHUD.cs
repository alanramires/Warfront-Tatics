using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitHUD : MonoBehaviour
{
    [Header("Referências Visuais")]
    public TextMeshProUGUI hpText;       
    public Image hpBackground;           // O quadradinho colorido
    
    // MUDANÇA 1: Agora é do tipo Image, para podermos pintar
    public Image lockIcon; 
    
    [Header("Armas (Opcional por enquanto)")]
    public Transform weaponContainer;    
    public GameObject weaponSlotPrefab;  

    // Configura Visual Completo (Cores e Fontes)
    public void SetVisuals(int teamId, Color teamColor)
    {
        // 1. Pinta o quadradinho de fundo
        if (hpBackground != null) 
        {
            hpBackground.color = teamColor;
        }

        // 2. Decide a cor da Fonte
        if (hpText != null)
        {
            if (teamId == 0 || teamId == 3) hpText.color = Color.black;
            else hpText.color = Color.white;
        }

        // 3. Pinta o Cadeado com a cor do time
        if (lockIcon != null)
        {
            lockIcon.color = teamColor;
            lockIcon.gameObject.SetActive(false); // Garante que começa escondido
        }
    }

    // Atualiza apenas o número do HP
    public void UpdateHP(int currentHP)
    {
        if (hpText != null)
        {
            hpText.text = currentHP.ToString();
        }
    }

    // --- CORREÇÃO AQUI ---
    // Liga/Desliga o Cadeado usando a nova variável 'lockIcon'
    public void SetLockState(bool isLocked)
    {
        if (lockIcon != null)
        {
            // Como lockIcon é uma Image, acessamos o .gameObject para ativar/desativar
            lockIcon.gameObject.SetActive(isLocked);
        }
    }

    // (Mantido por compatibilidade, mas o SetVisuals já faz isso)
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
        if (weaponContainer == null || weaponSlotPrefab == null) return; 

        // 1. Limpa armas antigas
        foreach (Transform child in weaponContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Cria os novos ícones
        foreach (WeaponConfig weapon in loadout)
        {
            if (weapon.data == null) continue; 

            GameObject newSlot = Instantiate(weaponSlotPrefab, weaponContainer);
            
            Transform iconTransform = newSlot.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImg = iconTransform.GetComponent<Image>();
                if (weapon.data.icon != null && iconImg != null) iconImg.sprite = weapon.data.icon; 
            }

            Transform ammoTransform = newSlot.transform.Find("Ammo");
            if (ammoTransform != null)
            {
                TextMeshProUGUI ammoText = ammoTransform.GetComponent<TextMeshProUGUI>();
                if (ammoText != null) ammoText.text = ":" + weapon.squadAttacks.ToString();
            }
        }
    }
}