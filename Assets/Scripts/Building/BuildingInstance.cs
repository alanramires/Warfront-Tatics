using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    [Header("Runtime")]
    public BuildingProfile profile;

    // Same convention as the rest of the project
    public int teamId = 0;

    public int capturePoints;
    public Vector3Int currentCell;

    [Header("Grid")]
    public Grid mainGrid;
    public float visualOffset = 0f;

    [Header("Refs")]
    public CursorController boardCursor;

    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // Initial SpriteRenderer setup
        sr.sortingLayerName = "Buildings";
        sr.sortingOrder = 0; // or another number if you use priority ordering
    }

    private void Start()
    {
        ApplyVisual();
        UpdateWorldPosition(true);
    }

    private void OnValidate()
    {
        ApplyVisual();
        UpdateWorldPosition(false);
    }

    public void Init(BuildingProfile p, int team, Vector3Int cell)
    {
        profile = p;
        teamId = team;
        currentCell = cell;

        capturePoints = (profile != null) ? profile.capturePointsMax : 0;

        ApplyVisual();
        UpdateWorldPosition(true);
        gameObject.name = profile != null ? $"Construcao_{profile.buildingName}_T{teamId}" : $"Construcao_T{teamId}";
    }

    private void UpdateWorldPosition(bool allowFindGrid)
    {
        if (allowFindGrid)
        {
            if (boardCursor == null)
                boardCursor = FindFirstObjectByType<CursorController>();

            if (mainGrid == null && boardCursor != null)
                mainGrid = boardCursor.mainGrid;
        }

        if (mainGrid == null) return;
        transform.position = GridUtils.GetCellCenterWorld(mainGrid, currentCell, visualOffset);
    }

    public void ApplyVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null || profile == null) return;

        sr.sprite = TeamUtils.GetTeamSprite(profile, teamId);
        sr.color = TeamUtils.GetColor(teamId);
        sr.flipX = TeamUtils.ShouldFlipX(teamId);
    }
}