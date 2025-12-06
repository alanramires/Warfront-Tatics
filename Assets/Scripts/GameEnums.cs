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