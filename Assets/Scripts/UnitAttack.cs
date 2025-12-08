using UnityEngine;
using System.Collections.Generic;

public class UnitAttack : MonoBehaviour
{
    [Header("Gerenciamento de Armas")]
    public List<WeaponConfig> myWeapons = new List<WeaponConfig>();

    [Header("Referências")]
    public UnitMovement movement; 
    public UnitHUD hud;           
    public int teamId;            

    public void SetupAttack(UnitData data, UnitHUD _hud, int _teamId)
    {
        hud = _hud;
        teamId = _teamId;
        myWeapons.Clear();
        if (data != null)
        {
            foreach (var w in data.weapons) myWeapons.Add(w); 
        }
        if (hud != null) hud.SetupWeapons(myWeapons);
    }

    // Retorna a LISTA de alvos (Método que o UnitMovement vai chamar)
    public List<UnitMovement> GetValidTargets(bool hasMoved)
    {
        List<UnitMovement> allTargets = new List<UnitMovement>();
        List<WeaponConfig> validWeapons = GetUsableWeapons(hasMoved);

        if (validWeapons.Count == 0) return allTargets;

        HashSet<UnitMovement> uniqueTargets = new HashSet<UnitMovement>();

        foreach (WeaponConfig weapon in validWeapons)
        {
            ScanRadiusForEnemies(weapon, uniqueTargets);
        }

        allTargets = new List<UnitMovement>(uniqueTargets);
        return allTargets;
    }

    List<WeaponConfig> GetUsableWeapons(bool hasMoved)
    {
        List<WeaponConfig> usable = new List<WeaponConfig>();
        foreach (var w in myWeapons)
        {
            if (w.squadAttacks <= 0) continue;
            if (hasMoved && w.minRange > 1) continue;
            usable.Add(w);
        }
        return usable;
    }

    void ScanRadiusForEnemies(WeaponConfig weapon, HashSet<UnitMovement> targetSet)
    {
        // --- CORREÇÃO DO WARNING AMARELO ---
        // Usamos FindObjectsByType com SortMode.None (Mais rápido e moderno)
        UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None); 

        foreach (UnitMovement target in allUnits)
        {
            if (target == movement) continue; 
            if (target.teamId == this.teamId) continue; 
            if (target.currentHP <= 0) continue; 

            int dist = Mathf.Abs(movement.currentCell.x - target.currentCell.x) + 
                       Mathf.Abs(movement.currentCell.y - target.currentCell.y);

            if (dist >= weapon.minRange && dist <= weapon.maxRange)
            {
                targetSet.Add(target);
                Debug.Log($"🎯 Alvo Detectado: {target.name} (Distância: {dist}) com arma {weapon.data.weaponName}");
            }
        }
    }
}