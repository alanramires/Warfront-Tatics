using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public static class Pathfinding 
{
    public class MoveMap
    {
        public List<Vector3Int> reachableTiles = new List<Vector3Int>();
        public Dictionary<Vector3Int, Vector3Int?> cameFrom = new Dictionary<Vector3Int, Vector3Int?>();
        public Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
    }

    // AGORA ACEITA "UnitType" PARA CALCULAR O CUSTO DO TERRENO
    public static MoveMap GenerateShadowMap(Vector3Int startNode, int maxRange, HashSet<Vector3Int> blocksMovement, UnitType unitType)
    {
        MoveMap map = new MoveMap();
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        
        frontier.Enqueue(startNode);
        map.cameFrom[startNode] = null; 
        map.costSoFar[startNode] = 0;
        map.reachableTiles.Add(startNode);

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            foreach (Vector3Int next in GetNeighbors(current))
            {
                // 1. Bloqueio Físico (Inimigos)
                if (blocksMovement.Contains(next)) continue;

                // 2. Custo do Terreno (Depende do Tipo da Unidade!)
                // Chama o TerrainManager passando o tipo (Infantaria vs Veículo)
                int terrainCost = TerrainManager.Instance != null ? TerrainManager.Instance.GetMovementCost(next, unitType) : 1;

                // Se for terreno intransponível (Montanha/Água), ignora
                if (terrainCost >= 99) continue;

                int newCost = map.costSoFar[current] + terrainCost; 

                if (!map.costSoFar.ContainsKey(next) || newCost < map.costSoFar[next])
                {
                    if (newCost <= maxRange)
                    {
                        map.costSoFar[next] = newCost;
                        map.cameFrom[next] = current; 
                        map.reachableTiles.Add(next);
                        frontier.Enqueue(next);
                    }
                }
            }
        }
        return map;
    }

    // --- MÉTODOS DE SUPORTE ATUALIZADOS PARA RECEBER 5 ARGUMENTOS ---

    public static List<Vector3Int> GetPathTo(Vector3Int startNode, Vector3Int endNode, int maxRange, HashSet<Vector3Int> blocksMovement, UnitType unitType)
    {
        MoveMap map = GenerateShadowMap(startNode, maxRange, blocksMovement, unitType);
        List<Vector3Int> path = new List<Vector3Int>();
        
        if (!map.cameFrom.ContainsKey(endNode)) return path; 

        Vector3Int? current = endNode;
        while (current != null)
        {
            path.Add(current.Value);
            current = map.cameFrom[current.Value];
        }
        path.Reverse(); 
        return path;
    }

    public static List<Vector3Int> GetReachableTiles(Vector3Int startNode, int maxRange, HashSet<Vector3Int> blocksMovement, HashSet<Vector3Int> blocksStopping, UnitType unitType)
    {
        // Gera o mapa considerando o tipo da unidade
        MoveMap map = GenerateShadowMap(startNode, maxRange, blocksMovement, unitType);
        
        List<Vector3Int> finalReachable = new List<Vector3Int>();

        // Filtra os tiles onde não pode parar (aliados)
        foreach(var tile in map.reachableTiles)
        {
            if (blocksStopping.Contains(tile) && tile != startNode)
            {
                continue; 
            }
            finalReachable.Add(tile);
        }

        return finalReachable;
    }

    private static List<Vector3Int> GetNeighbors(Vector3Int node)
    {
        return HexUtils.GetNeighborsOddR(node);
    }
}