using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configurações de Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 15f;
    
    [Header("Foco em Alvo")]
    public float focusSpeed = 5f;

    [Header("Limites do Mapa (Opcional)")]
    // Se quiser travar a câmera, defina estes valores no Inspector
    public bool useLimits = false;
    public float minX, maxX, minY, maxY;

    private Camera cam;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = GetComponent<Camera>();

        // 1. Defina o tamanho inicial da câmera (Se não estiver definido no Inspector)
        float tamanhoPadrao = cam.orthographicSize;

        // 2. Aplica o Zoom Fixo de 1.5x (Magnífico)
        float novoTamanho = tamanhoPadrao / 1.5f;

        // 3. Aplica o novo tamanho
        cam.orthographicSize = novoTamanho;
        
        // Remova a chamada para PanCamera() e ZoomCamera() do Update se não quiser controle
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

    public void FocusOn(Vector3 worldPosition, bool instant = false)
    {
        // Mantém o Z atual da câmera
        Vector3 target = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);

        // Respeita limites se estiverem ativos
        if (useLimits)
        {
            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.y = Mathf.Clamp(target.y, minY, maxY);
        }

        if (instant)
        {
            transform.position = target;
        }
        else
        {
            StopAllCoroutines();            // evita ter dois focos rodando ao mesmo tempo
            StartCoroutine(SmoothFocus(target));
        }
    }

    System.Collections.IEnumerator SmoothFocus(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * focusSpeed;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
    }

    // Foca na posição com offset dinâmico baseado na posição do cursor
    public void FocusOnWithOffset(Vector3 worldPos)
    {
        Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);

        // Determina a posição alvo na tela baseada na posição atual do cursor
        // Se cursor à esquerda, alvo à direita (75%), se à direita, alvo à esquerda (25%), etc. para dar mais espaço
        float targetX = screenPoint.x < Screen.width / 2 ? 0.75f : 0.25f;
        float targetY = screenPoint.y < Screen.height / 2 ? 0.75f : 0.25f;

        Vector3 desiredScreenPoint = new Vector3(
            Screen.width * targetX,
            Screen.height * targetY,
            cam.nearClipPlane
        );

        Vector3 desiredWorldPoint = cam.ScreenToWorldPoint(desiredScreenPoint);
        Vector3 delta = worldPos - desiredWorldPoint;
        Vector3 targetCamPos = transform.position + delta;
        targetCamPos.z = transform.position.z;

        // Aplica limites
        if (useLimits)
        {
            targetCamPos.x = Mathf.Clamp(targetCamPos.x, minX, maxX);
            targetCamPos.y = Mathf.Clamp(targetCamPos.y, minY, maxY);
        }

        StartCoroutine(SmoothFocus(targetCamPos));
    }

    // Ajusta a câmera se o cursor estiver próximo da borda ou fora da tela
    public void AdjustCameraForCursor(Vector3 cursorWorldPos)
{
    Vector3 sp = cam.WorldToScreenPoint(cursorWorldPos);

    float marginX = Screen.width * 0.30f;
    float marginY = Screen.height * 0.40f;

    // Se já está numa “safe area”, não faz nada
    if (sp.x >= marginX && sp.x <= Screen.width - marginX &&
        sp.y >= marginY && sp.y <= Screen.height - marginY)
        return;

    // Clamp do ponto do cursor para dentro da safe area
    float clampedX = Mathf.Clamp(sp.x, marginX, Screen.width - marginX);
    float clampedY = Mathf.Clamp(sp.y, marginY, Screen.height - marginY);

    // Converte o ponto clampado para mundo (mesmo "depth" do cursor)
    Vector3 desiredSp = new Vector3(clampedX, clampedY, sp.z);
    Vector3 desiredWp = cam.ScreenToWorldPoint(desiredSp);

    // Delta que a câmera precisa andar para o cursor ir para o ponto clampado
    Vector3 delta = cursorWorldPos - desiredWp;

    Vector3 targetCamPos = transform.position + delta;
    targetCamPos.z = transform.position.z;

    if (useLimits)
    {
        targetCamPos.x = Mathf.Clamp(targetCamPos.x, minX, maxX);
        targetCamPos.y = Mathf.Clamp(targetCamPos.y, minY, maxY);
    }

    StopAllCoroutines();
    StartCoroutine(SmoothFocus(targetCamPos));
}


}