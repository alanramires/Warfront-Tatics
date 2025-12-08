using UnityEngine;

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
    Plain,    
    Forest,   
    Mountain, 
    Water,    
    Obstacle  
}

// AQUI ESTÁ A CORREÇÃO: A lista completa de marchas
public enum TurnState
{
    None,             // 0. Cursor Livre
    Inspected,        // 0. Olhando (Inimigo/Aliado já agiu)
    
    Selected,         // 1. Unidade Selecionada
    Moving,           // 2. Andando fisicamente
    
    MenuOpen,      // 3. O HUB (Essa é a que estava faltando!)
    
    // --- Ramos de Ação ---
    Aiming,           // 3.1 Mirando
    ConfirmTarget,    // 3.2 Confirmando
    
    Finished          // 4. Já agiu
}

public static class GameColors
{
    public static readonly Color TeamGreen = new Color(144f/255f, 238f/255f, 144f/255f); 
    public static readonly Color TeamRed = new Color(255f/255f, 155f/255f, 155f/255f);   
    public static readonly Color TeamBlue = new Color(168f/255f, 168f/255f, 255f/255f);  
    public static readonly Color TeamYellow = new Color(255f/255f, 246f/255f, 141f/255f); 
}