using UnityEngine;

[CreateAssetMenu(fileName = "NovaConstrucao", menuName = "Warfront/Ficha de Construcao")]
public class BuildingProfile : ScriptableObject
{
    [Header("Identidade Visual")]
    public string buildingName;
    public Sprite spriteDefault;

    [Header("Skins por Time")]
    public Sprite spriteGreen;
    public Sprite spriteRed;
    public Sprite spriteBlue;
    public Sprite spriteYellow;

    [Header("DPQ (Posição/Construção)")]
    public PositionProfile positionProfile; // DPQ usado quando uma unidade está em cima

    [Header("Regras (MVP)")]
    public int capturePointsMax = 20;
    public int incomePerTurn = 1000;
    public bool canProduce = true;
    public bool isHQ = false;
}
