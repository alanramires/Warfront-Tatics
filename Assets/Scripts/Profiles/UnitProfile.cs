using UnityEngine;
using System.Collections.Generic; // NecessÃ¡rio para usar Listas

[CreateAssetMenu(fileName = "NovaUnidade", menuName = "Warfront/Ficha de Unidade")]
public class UnitProfile : ScriptableObject
{
    [Header("Identidade Visual")]
    public string unitName;        
    public Sprite spriteDefault;   
    
    [Header("Skins por Time")]
    public Sprite spriteGreen;     
    public Sprite spriteRed;       
    public Sprite spriteBlue;      
    public Sprite spriteYellow;    

    [Header("Regras de Movimento")]
    public UnitType unitType;      
    public int moveRange;          

    [Header("EstatÃ­sticas do EsquadrÃ£o")]
    public int maxHP = 10;        // "Unidades Iniciais" (Soldados vivos)
    public int defense = 8;       // "Defesa do Conjunto" (Fixo)
    public int maxFuel = 70; // Tanque cheio (PadrÃ£o: 70)

    [Header("Armamento (Arsenal)")]
    public List<WeaponConfig> weapons; // Lista flexÃ­vel (pode ter 1, 2 ou mais armas)
}

// A "Ficha da Arma" que fica dentro da unidade
// AQUI Ã‰ A MUDANÃ‡A:
[System.Serializable] 
public struct WeaponConfig
{
    [Header("O Que Ã©?")]
    public WeaponProfile data;  // <--- VocÃª arrasta o arquivo "Ficha_Rifle" aqui

    [Header("Como estÃ¡ montada?")]
    public int squadAttacks; // MuniÃ§Ã£o: Varia (Jipe tem mais q soldado)
    public int minRange;     // Alcance MÃ­nimo
    public int maxRange;     // Alcance MÃ¡ximo
}
