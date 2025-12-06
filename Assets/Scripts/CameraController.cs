using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configurações de Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 15f;

    [Header("Limites do Mapa (Opcional)")]
    // Se quiser travar a câmera, defina estes valores no Inspector
    public bool useLimits = false;
    public float minX, maxX, minY, maxY;

    private Camera cam;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        PanCamera();
        ZoomCamera();
    }

    void PanCamera()
    {
        // 1. Ao clicar (Botão Direito ou Meio/Scroll), salva a origem
        // (Pode mudar para 0 se quiser usar o botão esquerdo)
        if (Input.GetMouseButtonDown(1)) 
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // 2. Enquanto segura, calcula a diferença e move a câmera
        if (Input.GetMouseButton(1))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            
            // Move a câmera pela diferença calculada
            transform.position += difference;

            // (Opcional) Trava a câmera dentro dos limites
            if (useLimits)
            {
                float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
                float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
                transform.position = new Vector3(clampedX, clampedY, transform.position.z);
            }
        }
    }

    void ZoomCamera()
    {
        // Lê a rodinha do mouse (scroll)
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            // O zoom ortográfico funciona 'ao contrário':
            // Menor Size = Mais Perto (Zoom In)
            // Maior Size = Mais Longe (Zoom Out)
            float newSize = cam.orthographicSize - (scroll * zoomSpeed);

            // Garante que o zoom não passe dos limites
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}