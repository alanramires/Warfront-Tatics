using UnityEngine;

// GameEnums.cs - Arquivo global de definições

public enum UnitType 
{ 
    // A lista completa de tipos de unidade
    Infantry, 
    Vehicle,
    Artillery,
    Tank,
    JetFighter,
    Helicopter,
    Plane,
    Ship,
    Sub
}

public enum TerrainCategory
{
    // A lista completa de categorias de terreno
    Plain,    
    Forest,   
    Mountain, 
    Beach,
    Water,    
    Obstacle  
}

// Maquina de estados do turno
public enum TurnState
{
    None,        // estado inicial, nenhum selecionado
    Selected,    // unidade escolhida, ainda não se mexeu
    Inspected,   // vendo info de outra unidade
    Moving,      // animação de movimento rolando
    ConfirmMove,   // já moveu (ou clicou nela mesma) e está “em pré-visualização”
    Aiming,     // Mirando em um alvo (próxima etapa)
    ConfirmTarget, // Confirmação de tiro (depois)
    Attacking,    // animação de ataque rolando
    Finished       // já agiu nesse turno
}


public static class GameColors
{
    public static readonly Color TeamGreen = new Color(144f/255f, 238f/255f, 144f/255f); 
    public static readonly Color TeamRed = new Color(255f/255f, 155f/255f, 155f/255f);   
    public static readonly Color TeamBlue = new Color(168f/255f, 168f/255f, 255f/255f);  
    public static readonly Color TeamYellow = new Color(255f/255f, 246f/255f, 141f/255f); 
}

public static class TeamUtils
{
    public const int Green = 0;
    public const int Red = 1;
    public const int Blue = 2;
    public const int Yellow = 3;

    public static Color GetColor(int teamId)
    {
        switch (teamId)
        {
            case Green:  return GameColors.TeamGreen;
            case Red:    return GameColors.TeamRed;
            case Blue:   return GameColors.TeamBlue;
            case Yellow: return GameColors.TeamYellow;
            default:     return Color.white;
        }
    }

    public static string GetName(int teamId)
    {
        switch (teamId)
        {
            case Green:  return "verde";
            case Red:    return "vermelho";
            case Blue:   return "azul";
            case Yellow: return "amarelo";
            default:     return $"time {teamId}";
        }
    }
}
