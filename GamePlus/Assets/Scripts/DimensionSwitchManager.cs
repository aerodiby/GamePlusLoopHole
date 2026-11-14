using UnityEngine;

public class DimensionSwitchManager : MonoBehaviour
{
    public enum DimensionMode { Mode3D_FP, Mode2D_SideScroll }
    public DimensionMode currentMode = DimensionMode.Mode3D_FP;

    [Header("Cameras")]
    public Camera cam3D;
    public Camera cam2D;

    [Header("Player 3D References")]
    public MonoBehaviour player3DController;
    public CharacterController player3DCharController;
    public Renderer[] player3DRenderers;

    [Header("Player 2D References")]
    public MonoBehaviour player2DController;
    public Rigidbody2D player2DRb;
    public Renderer[] player2DRenderers;

    [Header("Auto-Size & Position Sync Settings")]
    public bool autoSyncPlayerSize = true;
    public bool matchWidthAndHeight = false;
    
    [Tooltip("Koordinat Z untuk bidang permainan 2D (default: 0)")]
    public float plane2DZ = 0f;

    [Tooltip("Jika dicentang, saat pindah dari 2D ke 3D, posisi Z Player 3D juga ditarik ke bidang 2D (Z = 0). Jika tidak, Z Player 3D tetap di tempat terakhirnya.")]
    public bool snap3DToPlaneZOnSwitch = false;

    void Start()
    {
        if ((player3DRenderers == null || player3DRenderers.Length == 0) && player3DController != null)
            player3DRenderers = player3DController.GetComponentsInChildren<Renderer>();

        if ((player2DRenderers == null || player2DRenderers.Length == 0) && player2DController != null)
            player2DRenderers = player2DController.GetComponentsInChildren<Renderer>();

        if (player3DCharController == null && player3DController != null)
            player3DCharController = player3DController.GetComponent<CharacterController>();

        if (autoSyncPlayerSize)
        {
            SyncPlayerSizes();
        }

        // Samakan posisi awal saat mulai game
        SyncPosition(currentMode == DimensionMode.Mode3D_FP);

        ApplyMode(currentMode);
    }

    void Update()
    {
        // Sinkronisasi posisi X dan Y secara real-time mengikuti karakter yang sedang aktif
        if (currentMode == DimensionMode.Mode3D_FP)
        {
            Sync3DTo2D();
        }
        else
        {
            Sync2DTo3D();
        }

        // Tekan Tab untuk ganti mode
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentMode = (currentMode == DimensionMode.Mode3D_FP) 
                ? DimensionMode.Mode2D_SideScroll 
                : DimensionMode.Mode3D_FP;
                
            ApplyMode(currentMode);
        }
    }

    // Menyalin X dan Y dari Player 3D ke Player 2D
    void Sync3DTo2D()
    {
        if (player3DController == null || player2DController == null) return;

        Vector3 pos3D = player3DController.transform.position;
        player2DController.transform.position = new Vector3(pos3D.x, pos3D.y, plane2DZ);
    }

    // Menyalin X dan Y dari Player 2D ke Player 3D
    void Sync2DTo3D()
    {
        if (player3DController == null || player2DController == null) return;

        Vector3 pos2D = player2DController.transform.position;
        Vector3 pos3D = player3DController.transform.position;

        float targetZ = snap3DToPlaneZOnSwitch ? plane2DZ : pos3D.z;

        // Matikan CharacterController sementara agar transform.position tidak di-override
        bool wasCCEnabled = false;
        if (player3DCharController != null && player3DCharController.enabled)
        {
            wasCCEnabled = true;
            player3DCharController.enabled = false;
        }

        player3DController.transform.position = new Vector3(pos2D.x, pos2D.y, targetZ);

        if (wasCCEnabled)
        {
            player3DCharController.enabled = true;
        }
    }

    void SyncPosition(bool from3DTo2D)
    {
        if (from3DTo2D) Sync3DTo2D();
        else Sync2DTo3D();
    }

    void SyncPlayerSizes()
    {
        if (player3DController == null || player2DController == null) return;

        Transform t3D = player3DController.transform;
        Transform t2D = player2DController.transform;

        float height3D = 2f * t3D.localScale.y;
        float width3D = 1f * t3D.localScale.x;

        if (player3DCharController != null)
        {
            height3D = player3DCharController.height * t3D.localScale.y;
            width3D = (player3DCharController.radius * 2f) * t3D.localScale.x;
        }

        SpriteRenderer sr2D = t2D.GetComponentInChildren<SpriteRenderer>();
        if (sr2D != null && sr2D.sprite != null)
        {
            Vector2 spriteBaseSize = sr2D.sprite.bounds.size;
            float newScaleY = height3D / spriteBaseSize.y;
            float newScaleX = matchWidthAndHeight ? (width3D / spriteBaseSize.x) : newScaleY;

            t2D.localScale = new Vector3(newScaleX, newScaleY, 1f);
        }
        else
        {
            t2D.localScale = new Vector3(matchWidthAndHeight ? width3D : height3D, height3D, 1f);
        }
    }

    void ApplyMode(DimensionMode mode)
    {
        bool is3D = (mode == DimensionMode.Mode3D_FP);

        // Pastikan posisi tersinkronisasi tepat sebelum berganti kontrol
        if (is3D)
        {
            Sync2DTo3D();
        }
        else
        {
            Sync3DTo2D();
            if (player2DRb != null) player2DRb.linearVelocity = Vector2.zero;
        }

        // Toggle Kamera
        cam3D.enabled = is3D;
        cam2D.enabled = !is3D;

        // Toggle Controller & CharacterController 3D
        if (player3DController != null) player3DController.enabled = is3D;
        if (player3DCharController != null) player3DCharController.enabled = is3D;
        if (player2DController != null) player2DController.enabled = !is3D;

        // Toggle Visibilitas Renderer
        SetRenderersVisible(player3DRenderers, is3D);
        SetRenderersVisible(player2DRenderers, !is3D);

        // Toggle Simulasi Rigidbody2D
        if (player2DRb != null)
        {
            player2DRb.simulated = !is3D;
        }

        // Pengaturan Kursor
        Cursor.lockState = is3D ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !is3D;
    }

    void SetRenderersVisible(Renderer[] renderers, bool isVisible)
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = isVisible;
        }
    }
}