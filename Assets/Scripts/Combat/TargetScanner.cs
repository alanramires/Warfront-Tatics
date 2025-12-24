using UnityEngine;
using System.Collections.Generic;

public class TargetScanner : MonoBehaviour
{
    [Header("Gerenciamento de Armas")]
    public List<WeaponConfig> myWeapons = new List<WeaponConfig>();

    [Header("ReferÃªncias")]
    public UnitMovement movement; 
    public UnitHUD hud;           
    public int teamId;    

    void Awake()
    {
        // Se nÃ£o tiver sido ligado via Inspector, pega automÃ¡tico
        if (movement == null)
            movement = GetComponent<UnitMovement>();

        if (movement != null)
        {
            // Garante que o time do ataque Ã© o mesmo da unidade
            teamId = movement.teamId;

            // Copia as armas da unidade se a lista local estiver vazia
            if (myWeapons == null)
                myWeapons = new List<WeaponConfig>();

            if (myWeapons.Count == 0 && movement.myWeapons != null)
            {
                myWeapons.Clear();
                myWeapons.AddRange(movement.myWeapons);
            }

            // HUD: se ninguÃ©m ligou, usa o da unidade
            if (hud == null)
                hud = movement.hud;
        }
    }      

    public void SetupAttack(UnitProfile data, UnitHUD _hud, int _teamId)
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

    
   // Retorna a LISTA de alvos (MÃ©todo que o TurnStateManager vai chamar)
    public List<UnitMovement> GetValidTargets(bool hasMoved)
    {
        HashSet<UnitMovement> targetSet = new HashSet<UnitMovement>();

        // Garante referÃªncia Ã  unidade
        if (movement == null)
            movement = GetComponent<UnitMovement>();

        if (movement == null)
        {
            Debug.LogWarning($"[TargetScanner] {name} sem UnitMovement associado.");
            return new List<UnitMovement>();
        }

        // Escolhe de onde vem a lista de armas:
        // 1) PreferÃªncia: armas do UnitMovement (montadas a partir do UnitProfile)
        // 2) Se por algum motivo estiver vazio, usa myWeapons local
        List<WeaponConfig> weaponSource = null;

        if (movement.myWeapons != null && movement.myWeapons.Count > 0)
            weaponSource = movement.myWeapons;
        else
            weaponSource = myWeapons;

        if (weaponSource == null || weaponSource.Count == 0)
        {
            Debug.Log($"[TargetScanner] {name} nÃ£o tem armas configuradas. hasMoved={hasMoved}");
            return new List<UnitMovement>();
        }

        // Garante que o teamId do ataque estÃ¡ sincronizado com a unidade
        teamId = movement.teamId;

        // Todas as unidades em cena
        var allUnits = UnitMovement.All;

        Debug.Log($"[TargetScanner] GetValidTargets: hasMoved={hasMoved}, armas={weaponSource.Count}, meuTime={teamId}");

        foreach (var weapon in weaponSource)
        {
            if (weapon.data == null) continue;

            int effectiveMin = weapon.minRange;
            int effectiveMax = weapon.maxRange;

            // Regra â€œmoveu x ficou paradoâ€
            if (hasMoved)
            {
                // Morteiro 2â€“3: se moveu, nÃ£o atira
                if (weapon.minRange > 1)
                    continue;

                // Bazooka 1â€“2: se moveu, sÃ³ alcance 1
                if (weapon.maxRange > 1)
                {
                    effectiveMin = 1;
                    effectiveMax = 1;
                }
            }

            foreach (var target in allUnits)
            {
                if (target == null) continue;
                if (target == movement) continue;           // nÃ£o mira em si mesmo
                if (target.teamId == teamId) continue;      // nÃ£o mira aliado
                if (target.currentHP <= 0) continue;        // morto nÃ£o conta

                int dx = movement.currentCell.x - target.currentCell.x;
                int dy = movement.currentCell.y - target.currentCell.y;
                int dist = HexUtils.HexDistance(movement.currentCell, target.currentCell, HexLayout.OddR);


                if (dist >= effectiveMin && dist <= effectiveMax)
                {
                    if (targetSet.Add(target))
                    {
                        Debug.Log(
                            $"ðŸŽ¯ {name} pode mirar em {target.name} (dist={dist}) " +
                            $"arma={weapon.data.weaponName} rangeEfetivo={effectiveMin}-{effectiveMax} moveu={hasMoved}"
                        );
                    }
                }
            }
        }

        Debug.Log($"[TargetScanner] {name} encontrou {targetSet.Count} alvo(s).");

        return new List<UnitMovement>(targetSet);
    }
}
