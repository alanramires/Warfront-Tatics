using UnityEngine;

[CreateAssetMenu(fileName = "NovaUnidade", menuName = "Warfront/Ficha de Unidade")]
public class UnitData : ScriptableObject
{
    [Header("Identidade Visual")]
    public string unitName;        
    public Sprite spriteDefault;   // Coringa

    [Header("Skins Específicas por Time")]
    public Sprite spriteGreen;     // Time 0
    public Sprite spriteRed;       // Time 1
    public Sprite spriteBlue;      // Time 2 (Novo)
    public Sprite spriteYellow;    // Time 3 (Novo)

    [Header("Regras do Jogo")]
    public UnitType unitType;      
    public int moveRange;          
}