using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TargetUiUtils
{
    public static string GetUnitDisplayName(UnitMovement unit, string fallback = "Alvo")
    {
        if (unit == null) return fallback;
        if (unit.data != null && !string.IsNullOrEmpty(unit.data.unitName))
            return unit.data.unitName;
        return !string.IsNullOrEmpty(unit.name) ? unit.name : fallback;
    }

    public static string GetCellLabel(UnitMovement unit)
    {
        if (unit == null) return "?,?";
        return $"{unit.currentCell.y},{unit.currentCell.x}";
    }

    public static bool TryGetSprite(UnitMovement unit, out Sprite sprite, out Color color, bool useTeamColor)
    {
        sprite = null;
        color = Color.white;
        if (unit == null) return false;
        if (!unit.TryGetComponent<SpriteRenderer>(out var sr)) return false;
        if (sr == null || sr.sprite == null) return false;

        sprite = sr.sprite;
        color = useTeamColor ? TeamUtils.GetColor(unit.teamId) : sr.color;
        return true;
    }

    public static void SetAttackHeader(TextMeshProUGUI header, UnitMovement target)
    {
        if (header == null) return;
        string unitName = GetUnitDisplayName(target, "Alvo");

        if (target == null)
        {
            header.text = $"Atacar {unitName}?";
            return;
        }

        Color c = TeamUtils.GetColor(target.teamId);
        string hex = ColorUtility.ToHtmlStringRGB(c);
        header.text = $"Atacar <color=#{hex}>{unitName}</color>?";
    }
}