using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathPreviewLine : MonoBehaviour
{
    public static PathPreviewLine Instance { get; private set; }

    [Header("Visual")]
    [SerializeField] private float width = 0.04f;
    [SerializeField, Range(0f, 1f)] private float alpha = 0.7f;

    [Header("Offsets")]
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private float zOffset = 0f;

    private LineRenderer lr;

    private void Awake()
    {
        Debug.Log($"[PathPreviewLine] Awake: {name} scene={gameObject.scene.name} instance={(Instance? Instance.gameObject.scene.name : "null")}");

         if (Instance != null && Instance != this)
        {
            bool thisIsDDOL  = gameObject.scene.name == "DontDestroyOnLoad";
            bool otherIsDDOL = Instance.gameObject.scene.name == "DontDestroyOnLoad";

            // Preferimos a cópia da cena (a que você controla)
           bool thisIsScene = gameObject.scene.name != "DontDestroyOnLoad";
           bool otherIsScene = Instance.gameObject.scene.name != "DontDestroyOnLoad";

            // ✅ Se eu sou da cena e a instância atual não é (DDOL), eu substituo.
            if (thisIsScene && !otherIsScene)
            {
                Debug.LogWarning("[PathPreviewLine] Scene instance replacing non-scene instance.");
                Destroy(Instance.gameObject);
                Instance = this;
            }
            // ✅ Se as duas são de cena, mantém a primeira e desliga a outra (evita duplicar)
            else
            {
                Debug.LogWarning($"[PathPreviewLine] Duplicate detected -> disabling this copy: {name} ({gameObject.scene.name})");
                gameObject.SetActive(false);
                enabled = false;
                return;
            }

        }
        else
        {
            Instance = this;
        }

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
        var shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            if (lr.material == null || lr.material.shader == null || lr.material.shader.name.Contains("Error"))
                lr.material = new Material(shader);

           // lr.material.color = Color.white;
        }


        // Depois de criar/garantir o material
        if (lr.material != null)
        {
          //  lr.material.color = Color.white;   // <- evita “tinta roxa” do material
            lr.material.renderQueue = 4000; // Render on top to avoid occlusion
        }


        lr.sortingOrder = Mathf.Max(lr.sortingOrder, 100);
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
            Debug.Log($"PathPreviewLine: Hiding because path is null or count < 2. Path: {path}");
            Hide();
            return;
        }

        Debug.Log($"PathPreviewLine: Showing path with {path.Count} points: {string.Join(", ", path)}");

        // cor do time + alpha
        Color c = GetTeamColor(unit.teamId);
        c.a = alpha;

        lr.startColor = c;
        lr.endColor = c;
        lr.startWidth = 0.1f;
        lr.endWidth   = 0.1f;


        if (lr.material != null)
            lr.material.color = c;


        // desenha pontos (centro do hex)
        Grid grid = unit.boardCursor.mainGrid;
        lr.positionCount = path.Count;

        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = grid.GetCellCenterWorld(path[i]);
            p.y += yOffset;
            p.z += zOffset;
            lr.SetPosition(i, p);
            positions.Add(p);
        }

        Debug.Log($"PathPreviewLine: Positions: {string.Join(", ", positions)}");

        lr.enabled = true;
        Debug.Log($"PathPreviewLine: Line enabled with {lr.positionCount} positions");
        lr.gameObject.SetActive(true);
        lr.forceRenderingOff = false; // garante que não ficou “mutado”
        lr.Simplify(0f);              // força recalcular internamente (hack bom)
        Vector3 p0 = lr.GetPosition(0);
        Vector3 pN = lr.GetPosition(lr.positionCount - 1);
        Debug.Log($"PathPreviewLine world from {p0} to {pN} | bounds={lr.bounds}");


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
