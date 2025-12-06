using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class GridDebug : MonoBehaviour
{
    [Header("Configurações")]
    public GameObject textPrefab; // O prefab 'CoordLabel'
    public Tilemap tilemap;       // O Tilemap do chão
    
    [Header("Ajuste de Posição")]
    // Use isso no Inspector para centralizar se ficar torto
    public Vector3 offset = new Vector3(0f, 0f, 0f); 

    void Start()
    {
        // Pega os limites do mapa pintado
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            // Só coloca texto se tiver chão nesse hexágono
            if (tilemap.HasTile(pos))
            {
                CreateLabel(pos);
            }
        }
    }

    void CreateLabel(Vector3Int pos)
    {
        // Converte grid (0,0) para mundo (0.5, 1.2, etc)
        Vector3 worldPos = tilemap.CellToWorld(pos);
        
        // Aplica o ajuste fino (para centralizar visualmente)
        worldPos += offset;

        // Cria o texto
        GameObject label = Instantiate(textPrefab, worldPos, Quaternion.identity, transform);
        
        // Pega o componente de texto e escreve a coordenada
        TMP_Text textComp = label.GetComponentInChildren<TMP_Text>();
        if (textComp != null)
        {
            // Exibe coordenada "Axial" (X, Y)
            textComp.text = $"{pos.x},{pos.y}";
        }
    }
}