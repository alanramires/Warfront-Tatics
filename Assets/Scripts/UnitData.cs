using UnityEngine;
using System.Collections.Generic; // Necessário para usar Listas

[CreateAssetMenu(fileName = "NovaUnidade", menuName = "Warfront/Ficha de Unidade")]
public class UnitData : ScriptableObject
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

    [Header("Estatísticas do Esquadrão")]
    public int maxHP = 10;        // "Unidades Iniciais" (Soldados vivos)
    public int defense = 8;       // "Defesa do Conjunto" (Fixo)

    [Header("Armamento (Arsenal)")]
    public List<WeaponConfig> weapons; // Lista flexível (pode ter 1, 2 ou mais armas)
}

// A "Ficha da Arma" que fica dentro da unidade
[System.Serializable] 
public struct WeaponConfig
{
    public string weaponName;       // Ex: "Rifle", "Granada"
    public Sprite icon; // <--- NOVO: O ícone (Rifle, Míssil, etc)
    public int attackPower;         // Força de Ataque (por membro)
    public int minRange;            // Alcance Mínimo (1 para rifle, 2 para morteiro/granada)
    public int maxRange;            // Alcance Máximo
    public int squadAttacks;        // "Munição" (Quantas vezes o esquadrão pode usar isso)
    
    // Futuro: public bool isIndirectFire; (Parábola)
    // Futuro: public UnitType preferredTarget; (Bônus contra tanque?)
}