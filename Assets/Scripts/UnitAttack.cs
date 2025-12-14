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

    void Awake()
    {
        // Se não tiver sido ligado via Inspector, pega automático
        if (movement == null)
            movement = GetComponent<UnitMovement>();

        if (movement != null)
        {
            // Garante que o time do ataque é o mesmo da unidade
            teamId = movement.teamId;

            // Copia as armas da unidade se a lista local estiver vazia
            if (myWeapons == null)
                myWeapons = new List<WeaponConfig>();

            if (myWeapons.Count == 0 && movement.myWeapons != null)
            {
                myWeapons.Clear();
                myWeapons.AddRange(movement.myWeapons);
            }

            // HUD: se ninguém ligou, usa o da unidade
            if (hud == null)
                hud = movement.hud;
        }
    }      

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

    
   // Retorna a LISTA de alvos (Método que o TurnStateManager vai chamar)
    public List<UnitMovement> GetValidTargets(bool hasMoved)
    {
        HashSet<UnitMovement> targetSet = new HashSet<UnitMovement>();

        // Garante referência à unidade
        if (movement == null)
            movement = GetComponent<UnitMovement>();

        if (movement == null)
        {
            Debug.LogWarning($"[UnitAttack] {name} sem UnitMovement associado.");
            return new List<UnitMovement>();
        }

        // Escolhe de onde vem a lista de armas:
        // 1) Preferência: armas do UnitMovement (montadas a partir do UnitData)
        // 2) Se por algum motivo estiver vazio, usa myWeapons local
        List<WeaponConfig> weaponSource = null;

        if (movement.myWeapons != null && movement.myWeapons.Count > 0)
            weaponSource = movement.myWeapons;
        else
            weaponSource = myWeapons;

        if (weaponSource == null || weaponSource.Count == 0)
        {
            Debug.Log($"[UnitAttack] {name} não tem armas configuradas. hasMoved={hasMoved}");
            return new List<UnitMovement>();
        }

        // Garante que o teamId do ataque está sincronizado com a unidade
        teamId = movement.teamId;

        // Todas as unidades em cena
        UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);

        Debug.Log($"[UnitAttack] GetValidTargets: hasMoved={hasMoved}, armas={weaponSource.Count}, meuTime={teamId}");

        foreach (var weapon in weaponSource)
        {
            if (weapon.data == null) continue;

            int effectiveMin = weapon.minRange;
            int effectiveMax = weapon.maxRange;

            // Regra “moveu x ficou parado”
            if (hasMoved)
            {
                // Morteiro 2–3: se moveu, não atira
                if (weapon.minRange > 1)
                    continue;

                // Bazooka 1–2: se moveu, só alcance 1
                if (weapon.maxRange > 1)
                {
                    effectiveMin = 1;
                    effectiveMax = 1;
                }
            }

            foreach (var target in allUnits)
            {
                if (target == null) continue;
                if (target == movement) continue;           // não mira em si mesmo
                if (target.teamId == teamId) continue;      // não mira aliado
                if (target.currentHP <= 0) continue;        // morto não conta

                int dx = movement.currentCell.x - target.currentCell.x;
                int dy = movement.currentCell.y - target.currentCell.y;
                int dist = HexUtils.HexDistance(movement.currentCell, target.currentCell, HexLayout.OddR);


                if (dist >= effectiveMin && dist <= effectiveMax)
                {
                    if (targetSet.Add(target))
                    {
                        Debug.Log(
                            $"🎯 {name} pode mirar em {target.name} (dist={dist}) " +
                            $"arma={weapon.data.weaponName} rangeEfetivo={effectiveMin}-{effectiveMax} moveu={hasMoved}"
                        );
                    }
                }
            }
        }

        Debug.Log($"[UnitAttack] {name} encontrou {targetSet.Count} alvo(s).");

        return new List<UnitMovement>(targetSet);
    }
}