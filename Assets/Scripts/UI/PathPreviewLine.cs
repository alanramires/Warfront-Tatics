using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathPreviewLine : MonoBehaviour
{
    public static PathPreviewLine Instance { get; private set; }

    [Header("Visual")]
    [SerializeField] private float width = 0.04f;
    [SerializeField, Range(0f, 1f)] private float alpha = 0.35f;

    [Header("Offsets")]
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float zOffset = 0f;

    private LineRenderer lr;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.enabled = false;
        lr.positionCount = 0;

        // arredonda os cantos (Nível 1)
        lr.numCornerVertices = 6;
        lr.numCapVertices = 6;

        // largura (pode ajustar no Inspector também)
        lr.widthMultiplier = width;

        // evita "pink" (shader/material faltando)
        if (lr.material == null || lr.material.shader == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lr.material = new Material(shader);
        }

        lr.sortingOrder = Mathf.Max(lr.sortingOrder, 50);
    }

    public void Show(UnitMovement unit)
    {
        if (unit == null || unit.boardCursor == null || unit.boardCursor.mainGrid == null)
        {
            Hide();
            return;
        }

        List<Vector3Int> path = unit.lastPathTaken;
        if (path == null || path.Count < 2)
        {
            Hide();
            return;
        }

        // cor do time + alpha
        Color c = GetTeamColor(unit.teamId);
        c.a = alpha;
        lr.startColor = c;
        lr.endColor = c;

        // desenha pontos (centro do hex)
        Grid grid = unit.boardCursor.mainGrid;
        lr.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = grid.GetCellCenterWorld(path[i]);
            p.y += yOffset;
            p.z += zOffset;
            lr.SetPosition(i, p);
        }

        lr.enabled = true;
    }

    public void Hide()
    {
        if (lr == null) return;
        lr.enabled = false;
        lr.positionCount = 0;
    }

    private Color GetTeamColor(int teamId)
    {
        switch (teamId)
        {
            case 0: return GameColors.TeamGreen;
            case 1: return GameColors.TeamRed;
            case 2: return GameColors.TeamBlue;
            case 3: return GameColors.TeamYellow;
            default: return Color.white;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
