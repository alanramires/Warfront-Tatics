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

    // --- NOVO: A BARRA DE COMBUSTÍVEL ---
    [Header("Combustível")]
    public Image fuelFillImage; // Arraste o "FuelBar_Fill" aqui
    public TextMeshProUGUI fuelText; // <--- NOVO: Arraste o texto aqui

    // --- NOVO: PALETA DE CORES ---
    [Header("Configuração de Cores")]
    public Color fuelSafeColor = new Color(0.0f, 0.5f, 0.0f); // Verde Escuro padrão
    public Color fuelDangerColor = Color.red;
    
    [Header("Armas (Opcional por enquanto)")]
    public Transform weaponContainer;    
    public GameObject weaponSlotPrefab;  

    // --- NOVO: ATUALIZA A BARRA ---
    public void UpdateFuel(int current, int max)
    {
        if (fuelFillImage != null)
        {
            // O ERRO PROVAVELMENTE ESTÁ AQUI:
            // Errado: float pct = current / max;  <-- Dá 0
            
            // Certo: Tem que ter (float) antes
            float pct = (float)current / max; 
            //Debug.Log($"Gasolina: {current}/{max} = {pct}"); // <--- OLHE O CONSOLE

            fuelFillImage.fillAmount = pct; // Atualiza a barra

            // Muda a cor
            if (pct <= 0.2f) fuelFillImage.color = fuelDangerColor;
            else fuelFillImage.color = fuelSafeColor;

            // --- ATUALIZA O TEXTO ---
            if (fuelText != null)
            {
                // Opção A: Mostra "14/70" (Mais preciso)
                fuelText.text = $"{current}";
                //fuelText.text = $"{current}/{max}";

                // Opção B: Mostra só o atual "14" (Mais limpo, se o max não importar tanto)
                // fuelText.text = current.ToString();
            }
        }
    }

    // Configura Visual Completo (Cores e Fontes)
    public void SetVisuals(int teamId, Color teamColor)
    {
        // 1. Pinta o quadradinho de fundo
        if (hpBackground != null) 
        {
           // hpBackground.color = teamColor;
        }

        // 2. Decide a cor da Fonte
        if (hpText != null)
        {
          //  if (teamId == 0 || teamId == 3) hpText.color = Color.black;
          //  else hpText.color = Color.white;
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

            var slot = newSlot.GetComponent<UIWeaponSlot>();
            if (slot != null)
            {
                slot.Bind(weapon);
            }
            else
            {
                Debug.LogWarning("weaponSlotPrefab não tem UIWeaponSlot.cs anexado.", newSlot);
            }
        }

    }
}