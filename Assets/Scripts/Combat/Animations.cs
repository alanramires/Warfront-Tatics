using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CombatAnimations
{

    // Struct auxiliar para agrupar info de morte
    private struct DeathTarget
    {
        public UnitMovement u;
        public Vector3Int cell;
        public Vector3 worldPos;
    }

    // Encolhe o transform ate minScale e volta ao normal
    public static IEnumerator CoShrinkWhile(Transform t, float duration, float minScale = 0.25f) // encolhe ate minScale e volta ao normal
    {
        if (t == null || duration <= 0f) yield break;

        Vector3 original = t.localScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);

            float s = Mathf.Lerp(original.x, minScale, p); // encolhe linearmente ate o min
            t.localScale = new Vector3(s, s, original.z);

            yield return null;
        }

        // volta ao normal
        t.localScale = original;
    }

    public static IEnumerator CoBumpTowards(Transform mover, Vector3 targetWorldPos, float distance = 0.15f, float duration = 0.10f) // mover se move em direcao ao targetWorldPos e volta
    {
        if (mover == null) yield break;

        Vector3 start = mover.position;
        Vector3 dir = (targetWorldPos - start).normalized;

        // seguranca se for NaN (mesma posicao)
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;

        Vector3 bump = start + dir * distance;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            mover.position = Vector3.Lerp(start, bump, t / duration);
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            mover.position = Vector3.Lerp(bump, start, t / duration);
            yield return null;
        }

        mover.position = start;
    }

    public static IEnumerator CoBumpTogether(Transform a, Transform b, float distance = 0.12f, float duration = 0.10f) // ambos se movem em direcao um ao outro e voltam
    {
        if (a == null || b == null) yield break;

        Vector3 a0 = a.position;
        Vector3 b0 = b.position;

        Vector3 dirAB = (b0 - a0).normalized;
        if (dirAB.sqrMagnitude < 0.0001f) dirAB = Vector3.right;

        Vector3 a1 = a0 + dirAB * distance;
        Vector3 b1 = b0 - dirAB * distance;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            a.position = Vector3.Lerp(a0, a1, p);
            b.position = Vector3.Lerp(b0, b1, p);
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            a.position = Vector3.Lerp(a1, a0, p);
            b.position = Vector3.Lerp(b1, b0, p);
            yield return null;
        }

        a.position = a0;
        b.position = b0;
    }

    public static IEnumerator CoBlinkThenExplodeAndHide(UnitMovement u, CursorController cursor, AudioClip explosionClip) // pisca a unidade, toca explosao e some
    {
        // garante que a camera "puxa" pro alvo que vai explodir
        if (cursor != null)
            cursor.TeleportToCell(u.currentCell, playSfx: true, adjustCamera: true);

        if (u == null) yield break;

        // pega todos os sprites da unidade (inclui HUD? nao; HUD costuma ser UI/Image, entao ok)
        var renderers = u.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            // sem sprite? so toca e some
            if (cursor != null && explosionClip != null) cursor.PlaySFX(explosionClip);
            u.gameObject.SetActive(false);
            yield break;
        }

        // piscada acelerando
        float interval = 0.12f;
        float minInterval = 0.03f;
        int blinks = 10;

        bool visible = true;
        for (int i = 0; i < blinks; i++)
        {
            visible = !visible; // alterna
            for (int r = 0; r < renderers.Length; r++)
                if (renderers[r] != null) renderers[r].enabled = visible;

            float wait = 0f;
            while (wait < interval)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            interval = Mathf.Max(minInterval, interval * 0.80f);
        }

        // garante que termina INVISIVEL
        for (int r = 0; r < renderers.Length; r++)
            if (renderers[r] != null) renderers[r].enabled = false;

        // toca explosao ja "sumido"
        float vfxDur = 0.2f;
        if (cursor != null && cursor.explosionPrefab != null)
        {
            var fxGO = Object.Instantiate(cursor.explosionPrefab, u.transform.position, Quaternion.identity);
            var fx = fxGO.GetComponent<ExplosionFX>();
            if (fx != null) vfxDur = Mathf.Max(0.01f, fx.TotalDuration);
        }

        float sfxDur = 0f;
        if (cursor != null && cursor.sfxExplosion != null)
        {
            cursor.PlaySFX(cursor.sfxExplosion);
            sfxDur = cursor.sfxExplosion.length;
        }

        // espera o suficiente pra ver/ouvir
        float finalWait = 0f;
        float finalDur = Mathf.Max(vfxDur, sfxDur);
        while (finalWait < finalDur)
        {
            finalWait += Time.unscaledDeltaTime;
            yield return null;
        }

        u.gameObject.SetActive(false);
    }

    // Pisca e explode varios, um por um
    public static IEnumerator CoBlinkThenExplodeAndHideMany(CursorController cursor, params UnitMovement[] units)
    {
        if (units == null || units.Length == 0) yield break;

        // 1) CACHEIA tudo ANTES de desativar qualquer unidade
        List<DeathTarget> list = new List<DeathTarget>(units.Length);
        for (int i = 0; i < units.Length; i++)
        {
            var u = units[i];
            if (u == null) continue;

            // Só anima morte se realmente morreu
            if (u.currentHP > 0) continue;

            list.Add(new DeathTarget
            {
                u = u,
                cell = u.currentCell,
                worldPos = u.transform.position
            });
        }

        // Nada pra fazer
        if (list.Count == 0) yield break;

        // 2) Executa em sequência (cursor vai pra cada um e resolve)
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];

            // pode acontecer de já ter sido desativado por alguma outra lógica
            if (t.u == null) continue;

            // foco/câmera
            if (cursor != null)
                cursor.TeleportToCell(t.cell, playSfx: true, adjustCamera: true);

            // BLINK (sem depender de posição depois)
            var renderers = t.u.GetComponentsInChildren<SpriteRenderer>(true);

            float interval = 0.12f;
            float minInterval = 0.03f;
            int blinks = 10;

            bool visible = true;
            for (int b = 0; b < blinks; b++)
            {
                visible = !visible;
                for (int r = 0; r < renderers.Length; r++)
                    if (renderers[r] != null) renderers[r].enabled = visible;

                yield return new WaitForSecondsRealtime(interval);
                interval = Mathf.Max(minInterval, interval * 0.80f);
            }

            // termina invisível
            for (int r = 0; r < renderers.Length; r++)
                if (renderers[r] != null) renderers[r].enabled = false;

            // VFX + SFX juntos (usa posição cacheada!)
            float vfxDur = 0.2f;
            if (cursor != null && cursor.explosionPrefab != null)
            {
                var fxGO = UnityEngine.Object.Instantiate(cursor.explosionPrefab, t.worldPos, Quaternion.identity);
                var fx = fxGO.GetComponent<ExplosionFX>();
                if (fx != null) vfxDur = Mathf.Max(0.01f, fx.TotalDuration);
            }

            float sfxDur = 0f;
            if (cursor != null && cursor.sfxExplosion != null)
            {
                cursor.PlaySFX(cursor.sfxExplosion);
                sfxDur = cursor.sfxExplosion.length;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(vfxDur, sfxDur));

            // agora sim desativa a unidade
            if (t.u != null)
                t.u.gameObject.SetActive(false);

            // micro pausa pra não “engolir” a próxima
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }


    // Dispara projeteis de ambos os lados (se houver)
    public static IEnumerator CoFireProjectiles(
        UnitMovement attacker,
        UnitMovement defender,
        WeaponProfile attackerWeapon,
        WeaponProfile defenderWeapon,
        bool attackerPulledTrigger,
        bool defenderPulledTrigger,
        CursorController cursor)
    {
        // delay intencional (fica bom mesmo)
        yield return new WaitForSeconds(0.25f);

        Projectile projAtk = null;
        Projectile projDef = null;

        Projectile SpawnProjectile(UnitMovement from, UnitMovement to, WeaponProfile w, bool parabolic)
        {
            if (cursor == null || cursor.projectilePrefab == null) return null;
            if (from == null || to == null || w == null) return null;

            var go = Object.Instantiate(cursor.projectilePrefab, from.transform.position, Quaternion.identity);
            var p = go.GetComponent<Projectile>();
            if (p == null) return null;

            // arco proporcional a distancia (fica parecido com o preview)
            float distWorld = Vector3.Distance(from.transform.position, to.transform.position);
            float arc = parabolic ? Mathf.Clamp(distWorld * 0.35f, 0.15f, 0.75f) : 0f;

            // tempo minimo de voo (pra enxergar)
            float minFlight = 0.18f; // rifle
            float speed = w.projectileSpeed;

            // dist/speed >= minFlight  => speed <= dist/minFlight
            if (distWorld > 0.001f)
                speed = Mathf.Min(speed, distWorld / minFlight);

            p.Init(
                from.transform.position,
                to.transform.position,
                w.projectileSprite,
                parabolic ? TrajectoryType.Parabolic : TrajectoryType.Straight,
                speed,
                arc,
                null
            );

            // som de tiro no spawn
            if (w.sfxFiring != null)
                cursor.PlaySFX(w.sfxFiring);

            return p;
        }

        if (attackerPulledTrigger && attackerWeapon != null)
            projAtk = SpawnProjectile(attacker, defender, attackerWeapon, attackerWeapon.trajectory == TrajectoryType.Parabolic);

        if (defenderPulledTrigger && defenderWeapon != null)
            projDef = SpawnProjectile(defender, attacker, defenderWeapon, defenderWeapon.trajectory == TrajectoryType.Parabolic);

        // fallback quando nao tem prefab
        if (cursor == null || cursor.projectilePrefab == null)
            yield return new WaitForSeconds(0.25f);

        yield return new WaitUntil(() => projAtk == null && projDef == null);
    }

    public static IEnumerator CoHitFlash(UnitMovement u, float duration = 0.15f, float interval = 0.03f)
    {
        if (u == null) yield break;

        var renderers = u.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

        // guarda cores originais
        var original = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) original[i] = renderers[i].color;

        float t = 0f;
        bool on = false;
        while (t < duration)
        {
            on = !on;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = on ? Color.white : new Color(1f, 0.3f, 0.3f, 1f); // branco/vermelho
            }

            yield return new WaitForSeconds(interval);
            t += interval;
        }

        // restaura
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = original[i];
    }

    public static IEnumerator CoShake(Transform t, float duration = 0.12f, float magnitude = 0.03f)
    {
        if (t == null) yield break;

        Vector3 start = t.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = (Random.value * 2f - 1f) * magnitude;
            float y = (Random.value * 2f - 1f) * magnitude;
            t.position = start + new Vector3(x, y, 0f);
            yield return null;
        }

        t.position = start;
    }


}
