using System;
using System.Collections;
using UnityEngine;

public static class Combat
{
    public static bool HasAmmoForWeapon0(UnitMovement attacker)
    {
        if (attacker == null) return false;
        if (attacker.myWeapons == null || attacker.myWeapons.Count == 0) return false;
        return attacker.myWeapons[0].squadAttacks > 0;
    }

    public static bool ConsumeAmmoWeapon0(UnitMovement attacker)
    {
        if (!HasAmmoForWeapon0(attacker)) return false;

        var w = attacker.myWeapons[0];   // struct -> cópia
        w.squadAttacks -= 1;
        attacker.myWeapons[0] = w;       // escreve de volta

        // MVP: atualiza HUD recriando slots (depois a gente otimiza)
        attacker.hud?.SetupWeapons(attacker.myWeapons);

        return true;
    }

    public static IEnumerator ResolveAttackWeapon0_MVP(UnitMovement attacker, UnitMovement target, Action onDone)
    {
        // MVP: nada de validação chata ainda (rifle vs submarino liberado 😂)
        // Só o mínimo: munição

        if (!ConsumeAmmoWeapon0(attacker))
        {
            Debug.Log("mano vc ta sem bala :D");
            onDone?.Invoke();
            yield break;
        }

        // placeholder de “balas voando”
        yield return new WaitForSeconds(0.25f);

        // TODO (futuro):
        // - spawn projétil e animar
        // - dano determinístico
        // - reduzir HP
        // - destruir alvo se morreu

        onDone?.Invoke();
    }
}
