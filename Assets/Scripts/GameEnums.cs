using UnityEngine; // Necessário para a classe Color

// GameEnums.cs - Arquivo global de definições
public enum UnitType 
{ 
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
    Plain,    // Planicie (Custo 1)
    Forest,   // Floresta (Custo varia)
    Mountain, // Montanha (Custo varia)
    Water,    // Mar (Bloqueio)
    Obstacle  // Muros
}

// --- NOVO: PALETA DE CORES SUAVES ---
public static class GameColors
{
    // Convertendo seus valores (0-255) para Unity (0.0 - 1.0)
    
    // Green: 144, 238, 144
    public static readonly Color TeamGreen = new Color(144f/255f, 238f/255f, 144f/255f); 
    
    // Red: 255, 155, 155
    public static readonly Color TeamRed = new Color(255f/255f, 155f/255f, 155f/255f);   

    // Blue: 168, 168, 255
    public static readonly Color TeamBlue = new Color(168f/255f, 168f/255f, 255f/255f);  
    
    // Yellow: 255, 246, 141
    public static readonly Color TeamYellow = new Color(255f/255f, 246f/255f, 141f/255f); 
}