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

    [Header("Estatísticas do Esquadrão")]
    public int maxHP = 10;        // "Unidades Iniciais" (Soldados vivos)
    public int defense = 8;       // "Defesa do Conjunto" (Fixo)
    public int maxFuel = 70; // Tanque cheio (Padrão: 70)

    [Header("Armamento (Arsenal)")]
    public List<WeaponConfig> weapons; // Lista flexÃ­vel (pode ter 1, 2 ou mais armas)
}

// A "Ficha da Arma" que fica dentro da unidade
// AQUI É A MUDANÇA:
[System.Serializable] 
public struct WeaponConfig
{
    [Header("O Que é?")]
    public WeaponProfile data;  // <--- Você arrasta o arquivo "Ficha_Rifle" aqui

    [Header("Como está montada?")]
    public int squadAttacks; // Munição: Varia (Jipe tem mais q soldado)
    public int minRange;     // Alcance Mínimo
    public int maxRange;     // Alcance Máximo
}
