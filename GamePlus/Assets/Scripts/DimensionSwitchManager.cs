using UnityEngine;

public class DimensionSwitchManager : MonoBehaviour
{
    public enum DimensionMode { Mode3D_FP, Mode2D_SideScroll }
    public DimensionMode currentMode = DimensionMode.Mode3D_FP;

    [Header("Cameras")]
    public Camera cam3D;
    public Camera cam2D;

    [Header("World / Level Roots")]
    [Tooltip("Tarik objek Level_2D dari Hierarchy ke sini")]
    public GameObject level2DRoot;
    [Tooltip("Opsional: Tarik Proxy_2D jika ingin bayangan collider 2D juga hilang saat di Mode 3D")]
    public Renderer proxy2DRenderer;

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
    public float plane2DZ = 0f;
    public bool snap3DToPlaneZOnSwitch = false;

    // Menyimpan semua renderer yang ada di dalam Level_2D (Ground_2D, platform, dll)
    private Renderer[] level2DRenderers;

    void Start()
    {
        if ((player3DRenderers == null || player3DRenderers.Length == 0) && player3DController != null)
            player3DRenderers = player3DController.GetComponentsInChildren<Renderer>(true);

        if ((player2DRenderers == null || player2DRenderers.Length == 0) && player2DController != null)
            player2DRenderers = player2DController.GetComponentsInChildren<Renderer>(true);

        if (player3DCharController == null && player3DController != null)
            player3DCharController = player3DController.GetComponent<CharacterController>();

        // Ambil semua Renderer di dalam Level_2D (termasuk Ground_2D, platform, dekorasi 2D)
        if (level2DRoot != null)
        {
            level2DRenderers = level2DRoot.GetComponentsInChildren<Renderer>(true);
        }

        if (autoSyncPlayerSize)
        {
            SyncPlayerSizes();
        }

        SyncPosition(currentMode == DimensionMode.Mode3D_FP);
        ApplyMode(currentMode);
    }

    void Update()
    {
        if (currentMode == DimensionMode.Mode3D_FP)
        {
            Sync3DTo2D();
        }
        else
        {
            Sync2DTo3D();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentMode = (currentMode == DimensionMode.Mode3D_FP) 
                ? DimensionMode.Mode2D_SideScroll 
                : DimensionMode.Mode3D_FP;
                
            ApplyMode(currentMode);
        }
    }

    void Sync3DTo2D()
    {
        if (player3DController == null || player2DController == null) return;

        Vector3 pos3D = player3DController.transform.position;
        player2DController.transform.position = new Vector3(pos3D.x, pos3D.y, plane2DZ);
    }

    void Sync2DTo3D()
    {
        if (player3DController == null || player2DController == null) return;

        Vector3 pos2D = player2DController.transform.position;
        Vector3 pos3D = player3DController.transform.position;

        float targetZ = snap3DToPlaneZOnSwitch ? plane2DZ : pos3D.z;

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

        if (is3D)
        {
            Sync2DTo3D();
        }
        else
        {
            Sync3DTo2D();
            if (player2DRb != null) player2DRb.linearVelocity = Vector2.zero;
        }

        // 1. Toggle Kamera
        cam3D.enabled = is3D;
        cam2D.enabled = !is3D;

        // 2. Toggle Controller
        if (player3DController != null) player3DController.enabled = is3D;
        if (player3DCharController != null) player3DCharController.enabled = is3D;
        if (player2DController != null) player2DController.enabled = !is3D;

        // 3. Sembunyikan / Tampilkan seluruh isi Level_2D (Ground_2D, Platform 2D, Player_2D)
        SetRenderersVisible(level2DRenderers, !is3D);
        SetRenderersVisible(player3DRenderers, is3D);
        SetRenderersVisible(player2DRenderers, !is3D);

        // Opsional: Sembunyikan juga sprite bayangan Proxy_2D saat di Mode 3D jika diisi
        if (proxy2DRenderer != null)
        {
            proxy2DRenderer.enabled = !is3D;
        }

        // 4. Toggle Simulasi Fisika 2D
        if (player2DRb != null)
        {
            player2DRb.simulated = !is3D;
        }

        // 5. Pengaturan Kursor
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