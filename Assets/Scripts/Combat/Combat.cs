using System;
using System.Text;
using System.IO;
using System.Collections;
using UnityEngine;

public static class Combat
{
    enum RoundMode { Floor, Standard, Ceil }

    // --- helpers de DPQ (diff = attackerQP - defenderQP) ---
    static (RoundMode atkMode, RoundMode defMode, int exactAtkDelta, int exactDefDelta) GetDpqRounding(int diff)
    {
        if (diff >= 2)
        {
            // +2 ou mais: atacante arredonda pra cima, defensor pra baixo
            return (RoundMode.Ceil, RoundMode.Floor, +1, -1);
        }
        if (diff >= 0)
        {
            // 0 ou +1: atacante pra cima, defensor padrão
            return (RoundMode.Ceil, RoundMode.Standard, +1, 0);
        }
        if (diff == -1)
        {
            // -1: ambos padrão
            return (RoundMode.Standard, RoundMode.Standard, 0, 0);
        }
        // <= -2: atacante pra baixo, defensor pra cima
        return (RoundMode.Floor, RoundMode.Ceil, -1, +1);
    }

    // arredonda eliminações conforme modo
    static int RoundElims(float raw, bool isExact, RoundMode mode, int exactDelta)
    {
        if (raw < 0f) raw = 0f;

        if (isExact)
        {
            // divisão exata: aplica delta (-1/0/+1)
            int v = Mathf.RoundToInt(raw);
            v += exactDelta;
            return Mathf.Max(0, v);
        }

        return mode switch
        {
            RoundMode.Ceil => Mathf.CeilToInt(raw),
            RoundMode.Floor => Mathf.FloorToInt(raw),
            _ => Mathf.RoundToInt(raw), // Standard (0.5+)
        };
    }

    // verifica se divisão é exata
    static bool IsExactDivision(int numerator, int denominator)
    {
        if (denominator == 0) return false;
        return (numerator % denominator) == 0;
    }

    // --- leitura de DPQ/defesa da posição (MVP: só terreno; construção entra depois) ---
    static int GetDefenseBonusFromPosition(UnitMovement u)
    {
        if (u == null) return 0;
        if (TerrainManager.Instance == null) return 0;

        // Ideal: TerrainManager.GetDefenseBonus(cell) devolve o defenseBonus do PositionProfile
        return TerrainManager.Instance.GetDefenseBonus(u.currentCell);
    }

    static int GetQualityPointsFromPosition(UnitMovement u)
    {
        if (u == null) return 0;
        if (TerrainManager.Instance == null) return 0;

        // Ideal: TerrainManager.GetQualityPoints(cell) devolve qualityPoints do PositionProfile
        return TerrainManager.Instance.GetQualityPoints(u.currentCell);
    }

    // --- stats efetivos ---
    static int EffectiveAttack(UnitMovement u)
    {
        if (u == null) return 0;
        if (u.myWeapons == null || u.myWeapons.Count == 0) return 0;
        if (u.myWeapons[0].data == null) return 0;

        int hp = Mathf.Max(0, u.currentHP);
        int fa = u.myWeapons[0].data.baseAttackPower; // força base da arma (asset)
        return hp * fa;
    }

    static int EffectiveDefense(UnitMovement u)
    {
        if (u == null || u.data == null) return 0;

        int baseDef = u.data.defense;
        int posDef = GetDefenseBonusFromPosition(u);
        return baseDef + posDef;
    }

    static int Distance(UnitMovement a, UnitMovement b)
    {
        return HexUtils.HexDistance(a.currentCell, b.currentCell, HexLayout.OddR);
    }

    // --- munição / ataques de esquadrão (arma[0]) ---
    static bool HasAmmoForWeapon(UnitMovement unit, int weaponIndex = 0)
    {
        if (unit == null || unit.myWeapons == null) return false;
        if (weaponIndex < 0 || weaponIndex >= unit.myWeapons.Count) return false;
        return unit.myWeapons[weaponIndex].squadAttacks > 0;
    }

    public static bool HasAmmoForWeapon0(UnitMovement unit)
    {
        return HasAmmoForWeapon(unit, 0);
    }

    public static bool ConsumeAmmoWeapon0(UnitMovement unit)
    {
        if (!HasAmmoForWeapon0(unit)) return false;

        var w = unit.myWeapons[0]; // struct (cópia)
        w.squadAttacks -= 1;
        unit.myWeapons[0] = w;     // escreve de volta

        unit.hud?.SetupWeapons(unit.myWeapons);
        return true;
    }

    public static bool EnsureAmmo(UnitMovement unit, int weaponIndex = 0)
    {
        if (HasAmmoForWeapon(unit, weaponIndex)) return true;

        Debug.Log("mano vc ta sem bala :D");
        unit?.boardCursor?.PlayError();
        return false;
    }

    // teto de eliminações: max 10 OU HP do atirador OU HP do alvo
    static int ClampElims(int elims, int shooterHP, int targetHP)
    {
        elims = Mathf.Max(0, elims);
        int cap = Mathf.Min(10, Mathf.Max(0, shooterHP), Mathf.Max(0, targetHP));
        return Mathf.Min(elims, cap);
    }

    // --- RESOLUÇÃO PRINCIPAL (MVP) ---
    public static IEnumerator ResolveAttackWeapon0_MVP(UnitMovement attacker, UnitMovement defender, Action onDone)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("[Combat] attacker/defender null.");
            onDone?.Invoke();
            yield break;
        }
        if (!defender.gameObject.activeInHierarchy || defender.currentHP <= 0)
        {
            Debug.LogWarning("[Combat] alvo inválido/morto. Abortando ataque.");
            onDone?.Invoke();
            yield break;
        }

        attacker.StopBlinking();
        defender.StopBlinking();

        // 0) Descobre trajetória pela arma 0 (MVP atual)
        var weaponData = (attacker != null && attacker.myWeapons != null && attacker.myWeapons.Count > 0)
            ? attacker.myWeapons[0].data
            : null;

        bool isParabolic = (weaponData != null && weaponData.trajectory == TrajectoryType.Parabolic);

        // 1) Toca o SFX “narrativo” (coyote caindo)
        var cursor = attacker != null ? attacker.boardCursor : null;

        // OBS: esses campos precisam existir no CursorController (ver abaixo)
        AudioClip preClip = null;
        if (cursor != null)
            preClip = isParabolic ? cursor.sfxArtillery : cursor.sfxAttacking;

        if (cursor != null && preClip != null)
        {
            cursor.PlaySFX(preClip);

            if (isParabolic)
            {
                // encolhe o ALVO durante o som de artilharia
                yield return CombatAnimations.CoShrinkWhile(defender.transform, preClip.length, 0.25f);
            }
            else
            {
                // --- MELEE/TIRO DIRETO: "bicar" estilo Civ 1 ---
                int distFX = Distance(attacker, defender);
                bool isDirectContact = (distFX == 1) && !isParabolic;

                // atacante sempre bica se for contato direto
                if (isDirectContact)
                {
                    bool defenderPulledTriggerFX = CanShootWeapon0(defender, distFX);

                    if (defenderPulledTriggerFX)
                        yield return CombatAnimations.CoBumpTogether(attacker.transform, defender.transform);
                    else
                        yield return CombatAnimations.CoBumpTowards(attacker.transform, defender.transform.position);
                }


            }
        }
        else
        {
            // fallback, se clip não estiver setado
            if (isParabolic)
                yield return CombatAnimations.CoShrinkWhile(defender.transform, 3.0f, 0.25f);
            else
                yield return new WaitForSeconds(0.8f);

        }

        // Se nao tem municao, nem puxa o gatilho
        if (!EnsureAmmo(attacker))
        {
            onDone?.Invoke();
            yield break;
        }

        int dist = Distance(attacker, defender);

        bool attackerPulledTrigger = CanShootWeapon0(attacker, dist);
        if (!attackerPulledTrigger)
        {
            attacker.boardCursor?.PlayError();
            onDone?.Invoke();
            yield break;
        }

        bool defenderPulledTrigger = (dist == 1) && CanShootWeapon0(defender, dist);
        var defWeapon = (defender.myWeapons != null && defender.myWeapons.Count > 0) ? defender.myWeapons[0].data : null;

        // 2) Agora entra seu "0.25f" (proj?til voando + sfx de arma depois)
        yield return CombatAnimations.CoFireProjectiles(
            attacker,
            defender,
            weaponData,
            defenderPulledTrigger ? defWeapon : null,
            attackerPulledTrigger,
            defenderPulledTrigger,
            cursor
        );




        // 3) Só então faz as contas (o resto do teu ResolveAttackWeapon0_MVP continua)

        // snapshot (HP antes do combate) – para aplicar simultâneo
        int atkHP0 = Mathf.Max(0, attacker.currentHP);
        int defHP0 = Mathf.Max(0, defender.currentHP);

        int atkAmmo0 = SafeAmmo(attacker);
        int defAmmo0 = SafeAmmo(defender);

        int atkBaseDef = attacker.data != null ? attacker.data.defense : 0;
        int defBaseDef = defender.data != null ? defender.data.defense : 0;

        int atkPosDef = GetDefenseBonusFromPosition(attacker);
        int defPosDef = GetDefenseBonusFromPosition(defender);

        int atkEffDef = EffectiveDefense(attacker);
        int defEffDef = EffectiveDefense(defender);

        int atkWeaponPower = SafeWeaponPower(attacker);
        int defWeaponPower = SafeWeaponPower(defender);

        int atkFA = atkHP0 * atkWeaponPower;
        int defFA = defHP0 * defWeaponPower; // só vira relevante se revidar

        string atkPosSource = GetPositionSourceStr(attacker);
        string defPosSource = GetPositionSourceStr(defender);

        string atkPosUsed = GetPositionUsedForCalcStr(attacker);
        string defPosUsed = GetPositionUsedForCalcStr(defender);





        // FUTURO (pós-MVP): mesmo com dist==1, pode NÃO revidar se domínio não for compatível.
        // Ex: AAA sendo atacada por bazooka (alvo terrestre) não revida porque arma só atinge DOMÍNIO AÉREO.
        // defenderCanRetaliate = defenderCanRetaliate && DomainAllowsRetaliation(defender, attacker);

        // “puxou o gatilho” = tentou atacar (tem munição e ataque é permitido)
        //bool attackerPulledTrigger = true;

        // defensor só “puxa gatilho” se revida E tem munição
        //bool defenderPulledTrigger = defenderCanRetaliate && HasAmmoForWeapon0(defender);


        // DPQ diff (do atacante em relação ao defensor)
        int qpA = GetQualityPointsFromPosition(attacker);
        int qpD = GetQualityPointsFromPosition(defender);
        int diff = qpA - qpD;

        var dpq = GetDpqRounding(diff);

        // Atacante elimina defensores
        int faA = EffectiveAttack(attacker);
        int fdD = Mathf.Max(1, EffectiveDefense(defender)); // evita div/0
        float rawA = (float)faA / fdD;
        bool exactA = IsExactDivision(faA, fdD);
        int elimDef = RoundElims(rawA, exactA, dpq.atkMode, dpq.exactAtkDelta);

        // Defensor elimina atacantes (só se revidar e tiver “puxado gatilho”)
        int elimAtk = 0;
        float rawD = 0f;
        if (defenderPulledTrigger)
        {
            int faD = EffectiveAttack(defender);
            int fdA = Mathf.Max(1, EffectiveDefense(attacker));
            rawD = (float)faD / fdA;
            bool exactD = IsExactDivision(faD, fdA);

            // IMPORTANTE: usa o MESMO diff do atacante (como você especificou)
            elimAtk = RoundElims(rawD, exactD, dpq.defMode, dpq.exactDefDelta);
        }

        // Aplica tetos (max 10 OU HP do atirador OU HP do alvo)
        elimDef = ClampElims(elimDef, atkHP0, defHP0);
        elimAtk = ClampElims(elimAtk, defHP0, atkHP0);

        // Dano bruto (pode ser letal)
        int damageToDefender = elimDef;
        int damageToAttacker = elimAtk;

        // Consumo de “ataques de esquadrão”:
        // se puxou gatilho, consome 1 mesmo com dano 0
        if (attackerPulledTrigger)
            ConsumeAmmoWeapon0(attacker);

        if (defenderPulledTrigger)
            ConsumeAmmoWeapon0(defender);

        // Aplica simultâneo (com snapshot)
        attacker.currentHP = Mathf.Max(0, atkHP0 - damageToAttacker);
        defender.currentHP = Mathf.Max(0, defHP0 - damageToDefender);

        Debug.Log($"[Combat] dist={dist} diff(QP)={diff} | A elimina {elimDef} (raw {rawA:0.00}) | D elimina {elimAtk} (raw {rawD:0.00}) | revida={defenderPulledTrigger}");

        // MVP: morte = pisca → explode → some (pode morrer dupla)
        bool atkDied = attacker.currentHP <= 0;
        bool defDied = defender.currentHP <= 0;

        // hit visuals (before death, if any damage) - wait only for flash
        IEnumerator CoHitFlash(UnitMovement unit, Action onDone)
        {
            if (unit != null)
                yield return CombatAnimations.CoHitFlash(unit, 0.10f, 0.02f);
            onDone?.Invoke();
        }

        void StartShake(UnitMovement unit, MonoBehaviour runner)
        {
            if (unit == null || runner == null) return;
            runner.StartCoroutine(CombatAnimations.CoShake(unit.transform, 0.10f, 0.025f));
        }

        MonoBehaviour hitRunner = attacker != null ? attacker : defender;
        if ((damageToDefender > 0 || damageToAttacker > 0) && hitRunner != null)
        {
            bool defDone = !(defender != null && damageToDefender > 0);
            bool atkDone = !(attacker != null && damageToAttacker > 0);

            if (!defDone)
            {
                StartShake(defender, hitRunner);
                hitRunner.StartCoroutine(CoHitFlash(defender, () => defDone = true));
            }

            if (!atkDone)
            {
                StartShake(attacker, hitRunner);
                hitRunner.StartCoroutine(CoHitFlash(attacker, () => atkDone = true));
            }

            if (!defDone || !atkDone)
                yield return new WaitUntil(() => defDone && atkDone);
        }
        else
        {
            if (defender != null && damageToDefender > 0)
            {
                yield return CombatAnimations.CoHitFlash(defender, 0.10f, 0.02f);
                yield return CombatAnimations.CoShake(defender.transform, 0.10f, 0.025f);
            }

            if (attacker != null && damageToAttacker > 0)
            {
                yield return CombatAnimations.CoHitFlash(attacker, 0.10f, 0.02f);
                yield return CombatAnimations.CoShake(attacker.transform, 0.10f, 0.025f);
            }
        }

        // animação de morte
        if (atkDied || defDied)
        {
            var deathCursor = attacker != null ? attacker.boardCursor : null;
            if (deathCursor == null && defender != null)
                deathCursor = defender.boardCursor;

            if (atkDied)
                yield return CombatAnimations.CoBlinkThenExplodeAndHideMany(deathCursor, attacker);

            if (defDied)
                yield return CombatAnimations.CoBlinkThenExplodeAndHideMany(deathCursor, defender);
        }

        // TODO: checkSobreviventes() / animação de morte
        // - quando você fizer animação, aqui vira “toca animação” e depois desativa/destrói.
        int atkHP1 = Mathf.Max(0, attacker.currentHP);
        int defHP1 = Mathf.Max(0, defender.currentHP);

        int atkAmmo1 = SafeAmmo(attacker);
        int defAmmo1 = SafeAmmo(defender);

        bool atkSurvived = attacker.currentHP > 0;
        bool defSurvived = defender.currentHP > 0;

        // “sobreviveu” aqui significa HP>0. Visibilidade você já está setando via SetActive(false)

        var sb = new StringBuilder(2048);

        sb.AppendLine("══════════════════════════════════════════════════════════════════════");
        sb.AppendLine("⚔️ COMBAT REPORT (MVP) — DUEL");
        sb.AppendLine("══════════════════════════════════════════════════════════════════════");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Distance: {dist} | DefenderRetaliates: {defenderPulledTrigger}");
        sb.AppendLine($"DPQ Points: Attacker={qpA} Defender={qpD} | diff(A-D)={diff}");
        sb.AppendLine($"DPQ Rounding: Atk={RoundModeStr(dpq.atkMode, exactA, dpq.exactAtkDelta)} | Def={RoundModeStr(dpq.defMode, defenderPulledTrigger ? IsExactDivision(EffectiveAttack(defender), Mathf.Max(1, EffectiveDefense(attacker))) : false, dpq.exactDefDelta)}");
        sb.AppendLine();

        sb.AppendLine("— ATTACKER —");
        sb.AppendLine($"Unit: {SafeUnitName(attacker)}");
        sb.AppendLine($"Cell: {CellStr(attacker)} | Tile: {TileStr(attacker)} | PositionSource: {atkPosSource} | UsedForCalc: {atkPosUsed}");
        sb.AppendLine($"HP: {atkHP0} -> {atkHP1} | Survived: {atkSurvived}");
        sb.AppendLine($"Ammo(Weapon0 squadAttacks): {atkAmmo0} -> {atkAmmo1} | PulledTrigger: {attackerPulledTrigger}");
        sb.AppendLine($"Weapon0: {SafeWeaponName(attacker)} | WeaponPower(FA): {atkWeaponPower}");
        sb.AppendLine($"EffectiveAttack: HP({atkHP0}) x FA({atkWeaponPower}) = {atkFA}");
        sb.AppendLine($"DefenseBase: {atkBaseDef} | DefensePosBonus: {atkPosDef} | EffectiveDefense: {atkEffDef}");
        sb.AppendLine();

        sb.AppendLine("— DEFENDER —");
        sb.AppendLine($"Unit: {SafeUnitName(defender)}");
        sb.AppendLine($"Cell: {CellStr(defender)} | Tile: {TileStr(defender)} | PositionSource: {defPosSource} | UsedForCalc: {defPosUsed}");
        sb.AppendLine($"HP: {defHP0} -> {defHP1} | Survived: {defSurvived}");
        sb.AppendLine($"Ammo(Weapon0 squadAttacks): {defAmmo0} -> {defAmmo1} | PulledTrigger: {defenderPulledTrigger}");
        sb.AppendLine($"Weapon0: {SafeWeaponName(defender)} | WeaponPower(FA): {defWeaponPower}");
        sb.AppendLine($"EffectiveAttack(if retaliates): HP({defHP0}) x FA({defWeaponPower}) = {defFA}");
        sb.AppendLine($"DefenseBase: {defBaseDef} | DefensePosBonus: {defPosDef} | EffectiveDefense: {defEffDef}");
        sb.AppendLine();

        sb.AppendLine("— RAW & ROUNDING —");
        sb.AppendLine($"Attacker raw (FA/FD): {faA}/{fdD} = {rawA:0.####} | ExactDivision: {exactA}");
        sb.AppendLine($"Attacker pre-cap elims: {RoundElims(rawA, exactA, dpq.atkMode, dpq.exactAtkDelta)} | cap=min(10, shooterHP={atkHP0}, targetHP={defHP0}) => final={elimDef}");

        if (defenderPulledTrigger)
        {
            // você já tem rawD variável no método
            sb.AppendLine($"Defender raw (FA/FD): {EffectiveAttack(defender)}/{Mathf.Max(1, EffectiveDefense(attacker))} = {rawD:0.####}");
            sb.AppendLine($"Defender pre-cap elims: {RoundElims(rawD, IsExactDivision(EffectiveAttack(defender), Mathf.Max(1, EffectiveDefense(attacker))), dpq.defMode, dpq.exactDefDelta)} | cap=min(10, shooterHP={defHP0}, targetHP={atkHP0}) => final={elimAtk}");
        }
        else
        {
            sb.AppendLine("Defender: no retaliation (range > 1 OR no ammo OR future-domain-block)");
            sb.AppendLine($"Defender final elims: {elimAtk}");
        }

        sb.AppendLine();
        sb.AppendLine("— OUTCOME —");
        sb.AppendLine($"Elims: Attacker→Defender={elimDef} | Defender→Attacker={elimAtk}");
        sb.AppendLine($"Survivors: Attacker={atkHP1} | Defender={defHP1}");
        sb.AppendLine("══════════════════════════════════════════════════════════════════════");

        string report = sb.ToString();
        Debug.Log(report);

        // (Opcional) salvar arquivo
        // WriteCombatLogToFile(report);
            

        onDone?.Invoke();

        
    }

    // ----------------------------------------------------------------
    //  DEBUG: log detalhado de combate
    // ----------------------------------------------------------------
    static string SafeUnitName(UnitMovement u)
    {
        if (u == null) return "(null)";
        if (u.data != null)
        {
            // Troque o campo abaixo se o seu UnitProfile usar outro nome
            // Exemplos comuns: unitName, nome, displayName
            var t = u.data.unitName;
            if (!string.IsNullOrWhiteSpace(t)) return t;
        }
        return u.name;
    }

    static string SafeWeaponName(UnitMovement u)
    {
        if (u == null || u.myWeapons == null || u.myWeapons.Count == 0) return "(sem arma)";
        var wd = u.myWeapons[0].data;
        if (wd == null) return "(arma0 sem data)";
        // Troque se seu WeaponProfile usar outro nome
        return string.IsNullOrWhiteSpace(wd.weaponName) ? wd.name : wd.weaponName;
    }

    static int SafeAmmo(UnitMovement u)
    {
        if (u == null || u.myWeapons == null || u.myWeapons.Count == 0) return 0;
        return u.myWeapons[0].squadAttacks;
    }

    static int SafeWeaponPower(UnitMovement u)
    {
        if (u == null || u.myWeapons == null || u.myWeapons.Count == 0) return 0;
        if (u.myWeapons[0].data == null) return 0;
        return u.myWeapons[0].data.baseAttackPower;
    }

    static string CellStr(UnitMovement u) => (u == null) ? "(?)" : $"({u.currentCell.x},{u.currentCell.y},{u.currentCell.z})";

    static string TileStr(UnitMovement u)
    {
        if (u == null) return "(?)";
        if (TerrainManager.Instance == null) return "(TerrainManager null)";
        var tile = TerrainManager.Instance.gameBoard.GetTile(u.currentCell); // se gameBoard for private, use getter ou faça helper no TerrainManager
        return tile != null ? tile.name : "(sem tile)";
    }

    static string GetPositionSourceStr(UnitMovement u)
    {
        // MVP: só hex/tile.
        // FUTURO: se houver construção no hex, reporte "Construção: <nome>" e use dpq dela.
        return "HEX";
    }

    static string GetPositionUsedForCalcStr(UnitMovement u) => "HEX"; // idem acima (futuro: "CONSTRUÇÃO" quando implementar)

    static string RoundModeStr(RoundMode mode, bool isExact, int exactDelta)
    {
        if (isExact)
        {
            if (exactDelta > 0) return $"+{exactDelta} (divisão exata)";
            if (exactDelta < 0) return $"{exactDelta} (divisão exata)";
            return "+0 (divisão exata)";
        }

        return mode switch
        {
            RoundMode.Ceil => "ARRED_CIMA (ceil)",
            RoundMode.Floor => "ARRED_BAIXO (floor)",
            _ => "ARRED_PADRAO (0.5+)"
        };
    }

    static void WriteCombatLogToFile(string text)
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "logs");
            Directory.CreateDirectory(dir);

            string file = Path.Combine(dir, $"combat_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(file, text, Encoding.UTF8);

            Debug.Log($"[Combat] Log salvo em: {file}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Combat] Falha ao salvar arquivo de log: {e.Message}");
        }
    }

    // --- checagens auxiliares de alcance/retaliação ---
    static bool Weapon0InRange(UnitMovement shooter, int dist)
    {
        if (shooter == null || shooter.myWeapons == null || shooter.myWeapons.Count == 0) return false;
        var cfg = shooter.myWeapons[0];
        if (cfg.data == null) return false;

        // Ajuste os nomes dos campos conforme seu WeaponData:
        // exemplos comuns: minRange/maxRange, rangeMin/rangeMax, minDistance/maxDistance
        int min = cfg.minRange;
        int max = cfg.maxRange;

        return dist >= min && dist <= max;
    }

    // checa se defensor pode revidar com arma 0
    static bool CanShootWeapon0(UnitMovement shooter, int dist)
    {
        return HasAmmoForWeapon0(shooter) && Weapon0InRange(shooter, dist);
    }



}
